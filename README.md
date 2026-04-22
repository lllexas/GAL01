# GAL01

Unity 视觉小说/AVG 框架。由 **NekoGraph** 声明式剧本图执行系统驱动剧情流程，**SpaceTUI**
去中心化事件框架渲染 UI，**GAL** 表现层完成对话、立绘、转场等演出。

## 架构全景

```
┌─────────────────────────────────────────────────────────────┐
│                     NekoGraph 后端                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │ GraphRunner  │  │ VFS 协议      │  │ 剧本编辑器        │   │
│  │ 信号驱动执行  │  │ 后缀即类型    │  │ .nekograph 资产  │   │
│  └──────┬───────┘  └──────┬───────┘  └──────────────────┘   │
│         │                 │                                   │
│         └─────────────────┘                                   │
│              ↓ 信号流                                         │
│         VFSNode(.dialog/.choice/...)                          │
│              ↓                                                │
│         Handler 三态返回值                                    │
│         Continue / Wait / Error                               │
└─────────────────────────────────────────────────────────────┘
                              ↓ 异步回调 (continueAction)
┌─────────────────────────────────────────────────────────────┐
│                     GAL 前端表现层                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │ DialogPlayer │  │ CameraDirector│  │ TransitionManager│   │
│  │ 序列仲裁队列  │  │ 镜头特效      │  │ 屏幕闪白/黑      │   │
│  └──────┬───────┘  └──────────────┘  └──────────────────┘   │
│         │                                                    │
│         ↓ PostSystem 事件总线                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │DialoguePanel │  │CharacterSlot │  │ BackgroundAnimator│   │
│  │ 对话文本      │  │ 角色立绘      │  │ 背景切换         │   │
│  └──────────────┘  └──────────────┘  └──────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## NekoGraph 后端：声明式剧本图执行系统

NekoGraph 不是传统意义上的"脚本解析器"，而是一个**完整的声明式执行引擎**——剧情结构以有向图的形式在编辑器中配置，运行时由
`GraphRunner` 驱动信号流遍历节点。

### 图执行引擎

- **信号驱动**：`SignalContext` 携带状态在节点间流动，每个信号有唯一 `SignalId`，支持存档/读档精确恢复
- **多 Pack 并行**：多个剧情包（Pack）可同时运行，Pack 间可通过信号通信
- **状态持久化**：Pack 数据可完整序列化为 JSON，游戏重启后无缝继续

### VFS 协议：数据驱动的运行时类型系统

VFS 协议把传统 OOP 的编译期类型声明延迟到运行时配置：

| OOP 要素 | 编译期          | 运行时（VFS）                       |
| -------- | --------------- | ----------------------------------- |
| 类型     | `class MyClass` | 后缀名 `.dialog`                    |
| 数据     | 成员变量        | 后缀绑定的 `typeof(T)`              |
| 行为     | 方法实现        | `[EXEHandler]` 标记的 `Handle(...)` |

**后缀即类型**：定义 `.dialog` 只需要三行代码——`[EXEHandler(".dialog", typeof(DialogSequenceSO))]`
注册类型，`Handle(...)` 编写行为。运行时信号流经过节点时自动触发，无需 `new`。

**载荷统一化**：无论源格式是 SO 引用、JSON 内联还是 CSV 文件，最终都转化为后缀协议定义的 C# 对象。Handler 不关心载荷从哪来。

### 三态返回值

```csharp
public enum HandleResult
{
    Continue,  // 同步继续传播子信号（默认）
    Wait,      // 挂起子信号，异步恢复
    Error      // 传播终止
}
```

### Wait 挂起恢复机制

当 Handler 需要等待异步操作（用户选择、网络请求、动画播放）时，返回 `Wait`。此时：

1. 子信号被克隆并挂起到 `SuspendedSignals` 字典（Key 为 `SignalId`）
2. `continueAction` 闭包通过 **ID 列表**（非直接引用）精确恢复子信号——GC 友好、可序列化
3. 异步完成后调用 `continueAction()`，挂起信号从字典移入 `ActiveSignals`，继续传播
4. 存档时 `SuspendedSignals` 被完整保存，读档后 `GraphRunner.Init()` 自动恢复

这是 NekoGraph 支持**复杂分支选择、存档读档**的核心机制。

### 剧本编辑器工作流

- **ShaderGraph 风格自动保存**：内存优先编辑，特定时机（Play Mode 前、构建时、Ctrl+S）落盘
- **`.nekograph` 专有扩展名**：`ScriptedImporter` 注册，Project 窗口原生支持，双击打开编辑器
- **脏标记与防丢失**：未保存时窗口标题显示 `*`，退出/Play Mode 前弹窗提示

## SpaceTUI：去中心化事件驱动 UI 框架

SpaceTUI 是 GAL 前端的 UI 基础设施，核心设计原则是**"没有管理器"**。

### 为什么不用管理器

传统 `UIManager` 上帝类需要 Inspector 拖拽 30+ 引用，改名字要改 10 处，Scene 文件 Merge 必冲突。SpaceTUI 的解决方案：

- **去中心化**：每个组件独立响应事件，删掉任何组件其他组件无感知
- **字符串标识**：通过 `UIID` 精确匹配，Scene 中零引用关系
- **原子化行为**：转场、显示面板、显示立绘都是独立事件，自由组合

### PostSystem 事件总线

```csharp
// 发事件（发布者不知道订阅者存在）
PostSystem.Instance.Send("期望显示面板", "DialoguePanel");

// 订阅事件（订阅者不知道发布者存在）
public class DialoguePanel : SpaceUIAnimator
{
    void Start() => 期望显示面板 += OnShow;
}
```

### SpaceUIAnimator：行为驱动动效基类

四层独立动画轨道，互不 Kill：

| 轨道               | 用途           | 方法                                        |
| ------------------ | -------------- | ------------------------------------------- |
| A `_stateTween`    | 淡入/淡出/位移 | `FadeIn()`, `FadeOut()`, `Show()`, `Hide()` |
| B `_rotationTween` | 旋转           | `RotateTo()`, `ResetRotation()`             |
| C `_scaleTween`    | 缩放/脉冲      | `PlayScaleAnimation()`, `ResetScale()`      |
| D `_breathTween`   | 呼吸循环       | `StartBreathing()`, `StopBreathing()`       |

轨道 C 和 D 通过**虚拟倍率**在 `Update` 中乘法合成到 `transform.localScale`：

```csharp
transform.localScale = _initialScale * _scaleMultiplier * _breathMultiplier;
```

## GAL 前端：自相似三层架构

前后端通信采用**自相似三层架构**，每层都是"发包-等待-回调"：

```
VFSHandler ──[调用传递委托]──► DialogPlayer ──[事件传递委托]──► DialoguePanel
```

### 完全解耦

`DialogPlayer` **不引用任何 NekoGraph 类型**，只接收 `DialogSequenceSO` 和 `Action onComplete` 回调。这意味着：

- GAL 前端可以在没有 NekoGraph 的情况下独立测试
- 后端可以替换为其他剧本系统，只要提供同样的 SO + 回调契约

### 仲裁队列

- FIFO 队列，同一时间只执行一个 Sequence
- 帧隔离：`yield return null` 避免 Hide/Show 时序冲突
- 自动回收：序列完成后 `GALFrontend.Instance.HideAllGalPanels()` 清理所有面板

### 前后端通信契约（完整数据流）

```
NekoGraph 剧本图
    ↓ 信号流到达 .dialog 节点
VFSNodeStrategy 识别后缀 .dialog
    ↓ 调用 Handler
DialogVFSHandler (Wait 模式)
    ① 解析 DialogSequenceSO
    ② 挂起 Runner 进程
    ③ 调用 DialogPlayer.TryPlay(sequence, onComplete)
    ④ 返回 HandleResult.Wait
        ↓
DialogPlayer (前端自治)
    ① 仲裁队列排队
    ② PostSystem.Send("期望显示面板", "DialoguePanel")
    ③ 逐条播放 Entries：
       - DialogEntry → PostSystem.Send("播放行", data)
       - AvatarEffectEntry → PostSystem.Send("期望显示角色", routedRequest)
       - BackgroundEffectEntry → PostSystem.Send("期望切换背景", routedRequest)
       - ScreenFlashEntry → PostSystem.Send("期望显示面板", "TransitionManager")
       - CameraEffectEntry → CameraDirector.Instance.PlayEffect(...)
    ④ 序列完成 → onComplete.Invoke()
        ↓
DialogVFSHandler 回调
    ① 恢复 Runner 进程 (continueAction)
    ② 子信号继续传播
```

## 目录结构

```
Assets/
├── Scripts/GAL/                  # GAL 前端程序集
│   ├── Runtime/                  # NekoGraph.GAL
│   │   ├── Frontend/             # UI 面板（DialoguePanel、ChoiceBar、CharacterAnimator、BackgroundAnimator）
│   │   ├── Director/             # CameraDirector、TransitionManager
│   │   ├── Player/               # DialogPlayer、ChoicePlayer
│   │   ├── Handler/              # VFS Handler（.dialog、.choice）
│   │   └── Data/                 # SO 数据定义（DialogSequenceSO、EffectEntry 等）
│   └── Editor/                   # NekoGraph.GAL.Editor
├── NekoGraph/                    # NekoGraph 后端（子模块/集成）
│   ├── Runtime/                  # 图执行引擎（GraphRunner、SignalContext、BasePackData）
│   ├── Editor/                   # 剧本编辑器（PackWindow、自动保存、.nekograph Importer）
│   └── SpaceTUI/                 # UI 动画框架（SpaceUIAnimator、PostSystem）
├── Resources/Dialog/             # 对话序列资产（DialogSequenceSO）
├── Resources/*.nekograph         # 剧本图资产
└── Scenes/SampleScene.unity      # 示例场景
```

## 程序集划分

| 程序集                 | 说明              | 依赖                                                                    |
| ---------------------- | ----------------- | ----------------------------------------------------------------------- |
| `NekoGraph.Runtime`    | 后端图执行引擎    | —                                                                       |
| `SpaceTUI`             | UI 动画与事件框架 | `NekoGraph.Runtime`, `DOTween.Modules`, `Unity.TextMeshPro`             |
| `NekoGraph.GAL`        | 前端运行时        | `NekoGraph.Runtime`, `SpaceTUI`, `DOTween.Modules`, `Unity.TextMeshPro` |
| `NekoGraph.GAL.Editor` | 前端编辑器扩展    | `NekoGraph.GAL`, `NekoGraph.Editor`, `NekoGraph.Runtime`                |

## 技术栈

- Unity 2022 LTS + URP
- [DOTween](http://dotween.demigiant.com/) — 动画引擎
- TextMeshPro — 文本渲染
- Newtonsoft.Json — 序列化
- NekoGraph — 声明式剧本图执行系统

## 快速开始

1. 打开 `Assets/Scenes/SampleScene.unity`
2. 创建对话序列：`Assets → Create → GAL → Dialog Sequence`
3. 在 NekoGraph Pack Editor 中创建 `.dialog` 节点，引用该序列
4. 运行场景，信号流自动驱动对话播放

## 设计文档

见 `Doc/` 目录：

| 文档                                  | 内容                                        |
| ------------------------------------- | ------------------------------------------- |
| `VFS协议详解.md`                      | 后缀即类型、Handle 定义、载荷统一化         |
| `NekoGraph-Wait信号挂起与恢复机制.md` | Wait 三态返回值、信号挂起字典、存档恢复     |
| `NekoGraph-自动保存与专有扩展名.md`   | ShaderGraph 风格自动保存、.nekograph 扩展名 |
| `SpaceUI设计原理.md`                  | 为什么不用管理器、去中心化事件驱动          |
| `SpaceUIAnimator架构详解.md`          | 四层轨道、虚拟倍率、UIID 路由               |
| `SpaceUI的计算机科学原理.md`          | 观察者模式、MVVM、SOLID 原则验证            |
| `Sequence播放器职责.md`               | 自相似三层架构、仲裁队列、职责边界          |
| `UIID.md`                             | UIID 命名规范                               |

## 许可证

MIT
