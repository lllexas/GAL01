# SpaceUI 的计算机科学原理

## 1. 观察者模式 (Observer Pattern)

**GoF 定义**：定义对象间的一对多依赖关系，当一个对象状态改变时，所有依赖者自动收到通知并更新。

**SpaceUI 实现**：
```csharp
protected event Action<object> 期望显示面板;

void Start()
{
    期望显示面板 += OnShow;  // 订阅（Attach）
}

// 触发（Notify）
PostSystem.Send("期望显示面板", data);
```

**关键点**：
- `Subject` = PostSystem 事件名（如 `"期望显示面板"`）
- `Observer` = 订阅事件的组件
- `ConcreteObserver` = `OnShow` 回调方法

---

## 2. 发布-订阅模式 (Publish-Subscribe)

**与观察者模式的区别**：引入事件总线作为中间层，发布者和订阅者完全解耦。

**SpaceUI 实现**：
```csharp
// 发布者不知道订阅者存在
PostSystem.Instance.Send("期望显示面板", "DialoguePanel");

// 订阅者不知道发布者存在
public class DialoguePanel : SpaceUIAnimator
{
    void Start() => 期望显示面板 += OnShow;
}
```

**理论价值**：
- **空间解耦**：发布者和订阅者无需知道对方存在或位置
- **时间解耦**：事件可异步处理，发布者无需等待订阅者
- **拓扑解耦**：支持 1:N、N:1、N:M 的灵活通信拓扑

---

## 3. 控制反转 (Inversion of Control, IoC)

**Fowler 定义**：反转控制的流向，框架调用应用程序代码，而非应用程序调用框架代码。

**传统模式（控制正转）**：
```csharp
public void ShowDialogue() 
{
    dialoguePanel.Show();      // 应用程序调用框架
    charStage.Highlight(2);    // 应用程序调用框架
}
```

**SpaceUI（控制反转）**：
```csharp
void Start()
{
    期望显示面板 += OnShow;    // 框架注册回调
}

// PostSystem 在适当时机调用
void OnShow(object data) { }   // 框架调用应用程序
```

**理论价值**：好莱坞原则 - "Don't call us, we'll call you"

---

## 4. 依赖倒置原则 (Dependency Inversion Principle)

**SOLID 原则之一**：高层模块不应依赖低层模块，二者都应依赖抽象。

**SpaceUI 实现**：
```csharp
// 依赖抽象（事件名）
PostSystem.Send("期望显示面板", "DialoguePanel");

// 而非依赖具体实现
// dialoguePanel.Show();  // 违反 DIP
```

**依赖关系图**：
```
传统：
DialoguePanel ───────► CharacterStage  (具体依赖具体)
         │
         ▼
   UIManager (上帝类)

SpaceUI：
DialoguePanel ───────► "期望显示面板"  (具体依赖抽象)
                           ▲
                           │
CharacterStage ────────────┘
```

---

## 5. 开闭原则 (Open/Closed Principle)

**SOLID 原则之一**：对扩展开放，对修改关闭。

**SpaceUI 验证**：

| 操作 | 传统管理器 | SpaceUI |
|-----|-----------|---------|
| 新增面板 | 修改 UIManager + Inspector | 只写新类，零修改 |
| 新增槽位 | 修改 CharacterManager | 零修改，发新 UIID 即可 |
| 新增行为 | 修改调用方 | 外挂订阅者 |

**代码证明**：
```csharp
// 新增 HistoryPanel，不需要改任何现有代码
public class HistoryPanel : SpaceUIAnimator
{
    protected override string UIID => "HistoryPanel";
    // 自动接入事件系统，无需注册
}
```

---

## 6. 迪米特法则 (Law of Demeter)

**最小知识原则**：一个对象应该对其他对象有最少的了解。

**SpaceUI 实现**：
```csharp
// 符合 LoD：只知道事件名
PostSystem.Send("期望显示面板", "CharSlot2");

// 违反 LoD：知道 CharacterStage 的存在及其方法
characterStage.slots[2].Show();
```

**理论价值**：
- 减少对象间的耦合度
- 降低因依赖变化导致的连锁修改
- 提高模块的独立性

---

## 7. 命令模式 (Command Pattern)

**GoF 定义**：将请求封装为对象，从而可用不同的请求、队列或日志来参数化其他对象。

**SpaceUI 中的命令**：
```csharp
// 命令对象 = 事件名 + 数据
public class CharacterShowData  // ConcreteCommand
{
    public int slotIndex;       // Receiver 标识
    public string id;           // 参数
    public Sprite sprite;       // 参数
}

// Invoker = PostSystem
PostSystem.Send("期望显示面板", new CharacterShowData { ... });

// Receiver = CharacterAnimator
void OnShow(object data) => Execute((CharacterShowData)data);
```

**扩展性**：
- 支持命令队列（可扩展）
- 支持命令日志（可扩展）
- 支持 Undo（可扩展）

---

## 8. 状态机 (Finite State Machine)

**定义**：有限个状态及状态间转移的数学模型。

**SpaceUI 状态**：
```csharp
// 状态定义
State {
    Visible,      // IsVisible = true
    Hidden,       // IsVisible = false
    FadingIn,     // _stateTween 播放中，alpha 0→1
    FadingOut     // _stateTween 播放中，alpha 1→0
}

// 状态标志
bool IsVisible;                          // 显式状态
bool _canvasGroup.blocksRaycasts;        // 隐式状态

// 状态转换保护
public void FadeIn() 
{
    if (_canvasGroup.blocksRaycasts) return;  // 已在显示状态，拒绝转移
    // 执行转移...
}
```

**理论价值**：防止无效状态转移，保证系统稳定性。

---

## 9. 函数组合 (Function Composition)

**函数式编程原理**：将多个简单函数组合成复杂函数。

**SpaceUI 的缩放组合**：
```csharp
// f(x) = initial * scale * breath
// 纯函数，无副作用
transform.localScale = _initialScale * _scaleMultiplier * _breathMultiplier;

// 各乘数独立控制
_scaleMultiplier = f_scale(t);      // 轨道 C 时间函数
_breathMultiplier = f_breath(t);    // 轨道 D 时间函数
```

**理论特性**：
- **可交换律**：乘法顺序可交换
- **可结合律**：(a * b) * c = a * (b * c)
- **幺元**：1.0 不影响结果

---

## 10. 单一职责原则 (Single Responsibility Principle)

**SOLID 原则之一**：一个类应该只有一个引起变化的原因。

**SpaceUI 的 SRP 分解**：

| 类 | 职责 | 变化原因 |
|---|------|---------|
| `SpaceUIAnimator` | 动画生命周期管理 | 动画系统变更 |
| `CharacterAnimator` | 立绘显示逻辑 | 立绘表现变更 |
| `DialoguePanel` | 对话文本渲染 | 对话 UI 变更 |
| `PostSystem` | 消息路由分发 | 通信机制变更 |
| `MatchUIID` | 地址匹配算法 | 寻址策略变更 |

**对比传统管理器**：
```csharp
// UIManager 违反 SRP：处理对话、立绘、转场...
public class UIManager 
{
    void ShowDialogue() { }   // 职责 1
    void ShowCharacter() { }  // 职责 2
    void DoTransition() { }   // 职责 3
    void PlaySound() { }      // 职责 4
    // ... 变化原因太多！
}
```

---

## 11. 接口隔离原则 (Interface Segregation Principle)

**SOLID 原则之一**：客户端不应被迫依赖它们不用的方法。

**SpaceUI 实现**：
```csharp
// 组件只订阅需要的事件
public class CharacterAnimator : SpaceUIAnimator
{
    void Start()
    {
        期望显示面板 += OnShow;   // 需要
        期望隐藏面板 += OnHide;   // 需要
        // 不订阅 鼠标点击（不需要就不依赖）
    }
}

// DialoguePanel 订阅不同的事件
public class DialoguePanel : SpaceUIAnimator
{
    void Start()
    {
        期望显示面板 += OnShow;
        鼠标点击 += OnClick;       // CharacterAnimator 不依赖这个
    }
}
```

---

## 12. 分层架构 (Layered Architecture)

**架构模式**：将系统划分为若干层，每层只与相邻层交互。

**SpaceUI 层次**：
```
┌─────────────────────────────────────┐
│  表现层 (Presentation)               │
│  - CharacterAnimator                 │
│  - DialoguePanel                     │
│  - TransitionManager                 │
├─────────────────────────────────────┤
│  事件层 (Event/Communication)        │
│  - PostSystem                        │
│  - MatchUIID                         │
├─────────────────────────────────────┤
│  基础设施层 (Infrastructure)         │
│  - DOTween (动画)                    │
│  - Unity EventSystem (输入)          │
└─────────────────────────────────────┘
```

**依赖规则**：上层依赖下层，下层不依赖上层。

---

## 理论验证：设计质量指标

### 耦合度 (Coupling)

| 指标 | 传统管理器 | SpaceUI |
|-----|-----------|---------|
| 类间依赖数 | O(n²) | O(n) |
| 修改影响范围 | 全局 | 局部 |
| 测试复杂度 | 高（需 Mock 全部依赖） | 低（只验证事件） |

### 内聚度 (Cohesion)

| 类型 | 传统管理器 | SpaceUI |
|-----|-----------|---------|
| 功能内聚 | 低（管理所有 UI） | 高（每个组件只做自己） |
| 通信内聚 | 中（直接方法调用） | 高（事件是统一通信方式） |

### 圈复杂度 (Cyclomatic Complexity)

```csharp
// 传统：UIManager 需要大量 if/else 判断调用谁
void Show(string name) 
{
    if (name == "dialogue") dialogue.Show();
    else if (name == "char") char.Show();  // 分支多，复杂度高
}

// SpaceUI：基类 MatchUIID 统一处理，子类无分支
void OnShow(object data) => FadeIn();  // 复杂度 1
```

---

## 13. MVVM 架构模式

**核心定义**：分离 Model（数据）、View（视图）、ViewModel（视图模型），ViewModel 通过某种机制与 View 通信。

**重要区分**：
- **MVVM** = 架构模式（分层、分离、绑定）
- **WPF** = MVVM 的一种实现（XAML + `INotifyPropertyChanged`）
- **SpaceUI** = MVVM 在 Unity 中的特定实现（事件绑定）

### 架构对比

| 层级 | 职责 | WPF 实现 | SpaceUI 实现 |
|-----|------|---------|-------------|
| **Model** | 纯数据 | C# POCO | `DialogueData`, `CharacterShowData` |
| **ViewModel** | 业务逻辑 + 视图状态 | `INotifyPropertyChanged` | `SpaceUIAnimator` 子类 |
| **View** | 渲染呈现 | XAML | Unity `GameObject` |
| **绑定机制** | View-ViewModel 通信 | 属性变更通知 | `PostSystem` 事件 |

### SpaceUI 的 MVVM 实现

**Model**（数据）
```csharp
public class DialogueData
{
    public string characterName;
    public string text;
}
```

**ViewModel**（视图模型）
```csharp
public class DialoguePanel : SpaceUIAnimator  // ViewModel
{
    protected override string UIID => "DialoguePanel";
    
    [SerializeField] private TextMeshProUGUI nameText;  // View 引用
    [SerializeField] private CanvasGroup _canvasGroup;  // View 引用
    
    // ViewModel 处理业务逻辑
    void OnShowPanel(object data)
    {
        if (data is DialogueData model)  // 获取 Model
        {
            nameText.text = model.characterName;  // 更新 View
            FadeIn();  // 控制 View 状态
        }
    }
}
```

**View**（视图）
- Unity `GameObject`
- `Image`、`TextMeshProUGUI`、`CanvasGroup` 等组件
- 被动接受 ViewModel 操作

**绑定机制**（事件替代属性绑定）
```csharp
// WPF: 属性变更自动同步
// <TextBlock Text="{Binding CharacterName}" />

// SpaceUI: 事件触发显式更新
PostSystem.Send("期望显示面板", new DialogueData { characterName = "主角" });
```

### 关于实现机制的选择

**WPF 的选择**（适合桌面应用）：
- 声明式 XAML
- 双向数据绑定
- 设计时支持（Blend）
- 反射性能可接受

**SpaceUI 的选择**（适合 Unity 游戏）：
- 命令式事件
- 单向数据流
- 运行时动态组合
- 无反射开销

**两者都是 MVVM**，只是**绑定机制不同**以适应不同平台特性。

### 常见误区澄清

**误区**：MVVM = WPF = XAML + `INotifyPropertyChanged`

**正解**：
- MVVM 是**架构模式**，关注分层和分离
- WPF 是**具体实现**，使用 XAML 和属性绑定
- SpaceUI 是**另一种实现**，使用事件和组件引用

**验证 SpaceUI 符合 MVVM**：

| MVVM 核心特征 | SpaceUI 符合？ |
|-------------|--------------|
| Model 独立？ | ✅ `DialogueData` 纯数据 |
| View 与 ViewModel 分离？ | ✅ ViewModel 操作 View 抽象（CanvasGroup） |
| ViewModel 不依赖具体 View？ | ✅ 通过事件通信，不依赖具体 GameObject |
| 可测试性？ | ✅ ViewModel 可独立单元测试 |
| 分层清晰？ | ✅ Model → ViewModel → View |

**结论**：SpaceUI 是 **MVVM 架构模式在 Unity 游戏开发中的特定实现**，用事件机制替代了 WPF 的属性绑定系统。

---

## 结论

SpaceUI 不是经验主义的产物，而是计算机科学经典理论的工程化实现：

- **GoF 设计模式**：观察者、命令、发布-订阅
- **SOLID 原则**：单一职责、开闭、依赖倒置、接口隔离
- **架构模式**：MVVM、分层、控制反转、事件驱动
- **编程范式**：函数组合、状态机

这些理论的存在保证了 SpaceUI 的**可维护性**、**可扩展性**和**可测试性**。
