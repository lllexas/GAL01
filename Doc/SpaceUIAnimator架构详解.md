# SpaceUIAnimator 架构详解

## 核心设计理念

**行为驱动动效基类 - 通过事件订阅组合行为**

- 去中心化：没有管理器概念，每个组件独立响应事件
- 事件驱动：所有通信通过 `PostSystem` 事件总线
- UIID 匹配：通过字符串标识符精确匹配目标组件
- 轨道独立：四个动画维度完全独立，互不干扰

---

## 类定义

```csharp
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public abstract class SpaceUIAnimator : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
```

**必须组件**：
- `CanvasGroup` - 控制 alpha 和射线拦截
- `Image` - UI 渲染（即使透明也需要）

**实现接口**：
- `IPointerEnterHandler` - 鼠标滑入
- `IPointerExitHandler` - 鼠标滑出  
- `IPointerClickHandler` - 鼠标点击

---

## 四层独立轨道

| 轨道 | 字段 | 用途 | 控制方法 |
|-----|------|------|---------|
| A | `_stateTween: Sequence` | 状态转换（淡入/淡出/位移） | `FadeIn()`, `FadeOut()`, `Show()`, `Hide()` |
| B | `_rotationTween: Tween` | 旋转动画 | `RotateTo()`, `ResetRotation()` |
| C | `_scaleTween: Tween` | 缩放/脉冲 | `PlayScaleAnimation()`, `ResetScale()` |
| D | `_breathTween: Tween` | 呼吸循环 | `StartBreathing()`, `StopBreathing()` |

**为什么独立？**
- 避免互相 Kill：呼吸动画不应该打断旋转动画
- 可组合：多个轨道动画可以同时播放

---

## 虚拟倍率合成

```csharp
// Update 中每帧执行
Vector3 combinedScale = _initialScale * _scaleMultiplier * _breathMultiplier;
transform.localScale = combinedScale;
```

| 倍率 | 默认值 | 控制轨道 | 作用 |
|-----|--------|---------|------|
| `_scaleMultiplier` | 1f | 轨道 C | `PlayScaleAnimation()` 时改变 |
| `_breathMultiplier` | 1f | 轨道 D | `StartBreathing()` 时在 1 ~ 1+amplitude 循环 |

**为什么用虚拟倍率？**
- 缩放动画和呼吸效果可以同时存在
- 乘法合成：`初始缩放 × 动画缩放 × 呼吸缩放`

---

## UIID 系统

### 定义

```csharp
// 基类私有字段（Inspector 显示但由代码控制）
[SerializeField] private string _uiID;

// 子类重写此属性
protected virtual string UIID => "";
```

### 工作流程

```csharp
// 1. 子类定义 UIID
public class MyPanel : SpaceUIAnimator
{
    protected override string UIID => "MyPanel";
}

// 2. 发送事件
PostSystem.Instance.Send("期望显示面板", "MyPanel");

// 3. 基类自动匹配
[Subscribe("期望显示面板")]
private void HandleShowPanel(object data)
{
    if (MatchUIID(data))  // "MyPanel" == "MyPanel" ✓
    {
        期望显示面板?.Invoke(data);  // 触发子类订阅
    }
}
```

### MatchUIID 逻辑

```csharp
protected bool MatchUIID(object data)
{
    if (string.IsNullOrEmpty(_uiID)) return false;
    
    string targetID = data as string;
    if (string.IsNullOrEmpty(targetID)) return false;
    
    return _uiID == targetID;  // 精确字符串匹配
}
```

---

## 事件管道

### 可用事件（protected）

```csharp
// 状态事件
protected event Action<object> 进入根界面;

// 面板显示/隐藏
protected event Action<object> 期望显示面板;
protected event Action<object> 期望隐藏面板;

// 鼠标交互
protected event Action<PointerEventData> 鼠标滑入;
protected event Action<PointerEventData> 鼠标滑出;
protected event Action<PointerEventData> 鼠标点击;
```

### 内部中转站（private）

```csharp
[Subscribe("进入根界面")]
private void HandleEnterRoot(object data) => 进入根界面?.Invoke(data);

[Subscribe("期望显示面板")]
private void HandleShowPanel(object data) 
{
    if (MatchUIID(data)) 期望显示面板?.Invoke(data);
}

[Subscribe("期望隐藏面板")]
private void HandleHidePanel(object data)
{
    if (MatchUIID(data)) 期望隐藏面板?.Invoke(data);
}

[Subscribe("期望隐藏所有面板")]
private void HandleHideAllPanels(object data) => 期望隐藏面板?.Invoke(data);
```

**注意**：`期望隐藏所有面板` **跳过 ID 检查**，广播给所有面板。

---

## 生命周期

### Awake

```csharp
protected virtual void Awake()
{
    _uiID = UIID;                                    // 从子类获取 UIID
    _canvasGroup = GetComponent<CanvasGroup>();      // 获取组件
    
    // 缓存初始状态
    _initialPosition = transform.localPosition;
    _initialRotation = transform.localRotation;
    _initialScale = transform.localScale;
    _slideInStartPosition = _initialPosition;
    _targetRotation = _initialRotation;
    
    // 初始隐藏状态
    _canvasGroup.alpha = 0f;
    _canvasGroup.blocksRaycasts = false;
    
    // 注册到 PostSystem
    PostSystem.Instance.Register(this);
}
```

### OnDestroy

```csharp
protected virtual void OnDestroy()
{
    // Kill 所有轨道
    _stateTween?.Kill();
    _rotationTween?.Kill();
    _scaleTween?.Kill();
    _breathTween?.Kill();
    
    // 注销事件
    PostSystem.Instance.Unregister(this);
}
```

### Update

```csharp
protected virtual void Update()
{
    CheckAndApplyRotation();  // 自动旋转
    
    // 缩放合成
    transform.localScale = _initialScale * _scaleMultiplier * _breathMultiplier;
}
```

---

## 原子动画方法

### 状态方法

| 方法 | 作用 | 保护机制 |
|-----|------|---------|
| `FadeIn()` | 淡入 + 向上位移 | `blocksRaycasts == true` 时禁止 |
| `FadeOut()` | 淡出 + 向下位移 | `blocksRaycasts == false` 时禁止 |
| `Show()` | 立即显示 | `blocksRaycasts == true` 时禁止 |
| `Hide()` | 立即隐藏 | `blocksRaycasts == false` 时禁止 |

**关键设计**：`blocksRaycasts` 同时作为**状态标志**和**交互控制**。

### 轨道控制方法

```csharp
// 轨道 B - 旋转
public void RotateTo(float targetRotationY)
public void RotateTo(Quaternion targetRotation)
public void ResetRotation()

// 轨道 C - 缩放
public void PlayScaleAnimation()
public void ResetScale()
public void SetTargetScale(Vector3 scale)

// 轨道 D - 呼吸
public void StartBreathing()
public void StopBreathing()
```

---

## 抽象方法

```csharp
protected abstract void CloseAction();
```

**子类必须实现** - 定义关闭按钮的行为。

**建议实现**：
```csharp
protected override void CloseAction()
{
    FadeOut();
    // 或
    Hide();
    // 或
    PostSystem.Instance.Send("期望隐藏面板", UIID);
}
```

---

## 正确 vs 错误使用

### ✅ 正确

```csharp
public class MyPanel : SpaceUIAnimator
{
    protected override string UIID => "MyPanel";
    
    void Start()
    {
        期望显示面板 += OnShow;
    }
    
    void OnShow(object data)
    {
        // 基类已匹配 UIID，能进来就是该我响应
        FadeIn();
    }
    
    protected override void CloseAction()
    {
        FadeOut();
    }
}
```

### ❌ 错误

```csharp
// 错误 1：直接调用其他组件
public void Open()
{
    otherPanel.Show();  // 违反解耦原则！
}

// 错误 2：重复做 UIID 匹配
void OnShow(object data)
{
    if (data as string == "MyPanel")  // 基类已经做了！
        Show();
}

// 错误 3：忘记实现 CloseAction
public class MyPanel : SpaceUIAnimator
{
    // 编译错误！CloseAction 是 abstract
}
```

---

## 关键字段汇总

| 字段 | 类型 | 用途 |
|-----|------|------|
| `_uiID` | string | 序列化字段（Inspector 显示） |
| `UIID` | virtual property | 子类重写提供标识 |
| `_canvasGroup` | CanvasGroup | 状态控制核心 |
| `_stateTween` | Sequence | 轨道 A |
| `_rotationTween` | Tween | 轨道 B |
| `_scaleTween` | Tween | 轨道 C |
| `_breathTween` | Tween | 轨道 D |
| `_scaleMultiplier` | float | 轨道 C 当前倍率 |
| `_breathMultiplier` | float | 轨道 D 当前倍率 |
| `_initialPosition/Rotation/Scale` | Vector3/Quaternion | 初始状态缓存 |
| `_slideInStartPosition` | Vector3 | 淡入动画起始位置 |
| `_targetRotation` | Quaternion | 旋转目标值 |
| `IsVisible` | bool | 可见状态 |

---

## 版本历史

- **当前版本**：基于代码逐行核对
- **文档作者**：AI Assistant（经用户拷打后修正）
- **修正内容**：补充了接口实现、RequireComponent、抽象方法等遗漏细节
