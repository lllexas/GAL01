# VFS 协议详解

## 定义一个后缀

**定义一个后缀，就是定义一个类型。**

这不是比喻，是字面意思。VFS 协议是一套**数据驱动的面向对象系统**，把传统 OOP 编译期固定的要素，全部延迟到运行时配置。

| OOP 要素 | 编译期（传统） | 运行时（VFS） |
|---------|--------------|--------------|
| **类型** | `class MyClass` | 后缀名 `.xxx` |
| **数据** | 成员变量声明 | 后缀绑定的数据结构（`typeof(T)`） |
| **行为** | 方法实现 | `Handle(...)` 方法体 |

**定义一个后缀 = 在运行时注册一个新类型。**

---

## 为什么这样设计

### 传统 OOP 的痛点

```csharp
// 编译期写死，运行时改不了
public class DialogueSystem 
{
    public void ShowDialog(DialogData data) { ... }
}

// 要加新类型？改代码、重编译、发版本
public class ChoiceSystem { }  // 新增
public class ShopSystem { }   // 新增
```

### 数据驱动 OOP 的优势

| 能力 | 说明 |
|------|------|
| **运行时扩展** | DLC 新增 `.quest`、`.shop` 后缀，主工程零改动 |
| **配置即代码** | 改 SO 数据 = 改对象状态，不改代码逻辑 |
| **复用与组合** | 同一个 SO 被多个 VFS 节点引用，一改全生效 |
| **版本控制** | 图结构（流程）和数据（内容）分离提交，Merge 不冲突 |

---

## 如何定义一个后缀

**定义 = 三行代码：**

```csharp
[EXEHandler(".dialog", typeof(DialogSequenceSO))]
public static void Handle(...) { }
```

| 行 | 作用 | OOP 对应 |
|---|------|---------|
| `[EXEHandler(...)]` | 声明类型 + 绑定数据 | `class MyClass : BaseClass` |
| `.dialog` | 类型标识符 | 类名 |
| `typeof(DialogSequenceSO)` | 数据结构 | 成员变量类型 |
| `Handle(...)` | 行为入口 | 方法声明 |

**Handle 方法体放什么？不重要。**

那是**实现**，不是**定义**。就像接口定义和具体实现的区别：

```csharp
// 定义（接口）
interface IDialogHandler { void Handle(); }

// 实现（随便写）
class DialogHandler : IDialogHandler 
{ 
    void Handle() { /* 你的业务逻辑 */ } 
}
```

VFS 协议的 `[EXEHandler]` 就是**运行时的接口注册**，三行搞定类型声明。方法体里的代码随你写，不影响"定义"本身。

---

### 数据结构（独立于定义）

```csharp
public class DialogSequenceSO : ScriptableObject
{
    public string SequenceId;
    public List<ISequenceEntry> Entries;
}
```

SO 是数据容器，被 VFS 节点引用。定义时绑定它，运行时实例化它。

---

## 运行时发生了什么

```
VFS 图（类型声明）        SO 资源（数据实例）        Handler（方法实现）
┌──────────────┐        ┌──────────────┐        ┌──────────────┐
│ intro.dialog │───────▶│ Chapter1_SO  │───────▶│ Handle(...)  │
│   (.dialog)  │ 引用    │  (数据内容)   │ 驱动    │  (执行逻辑)   │
└──────────────┘        └──────────────┘        └──────────────┘
       │                                               │
       │                    信号流经节点时               │
       └──────────────────── 自动触发 ─────────────────▶

GraphRunner (Update 驱动)
    ↓
VFSNodeStrategy.OnSignalEnter(intro.dialog)
    ↓
ExeRegistry.TryGetHandler(".dialog") → 找到 Handle
    ↓
Handle(content, context, ...) 被调用
    ↓
执行业务逻辑，结果写入 context.Args
```

**关键点**：
- 你不是去 `new` 一个对象，而是**信号流经过节点时，框架自动帮你实例化并调用方法**
- 实例化来源是 VFS 节点的引用（SO 路径）
- 方法执行是同步的，但结果可以通过 `context.Args` 传递给下游节点

---

## 定义 .dialog

### 类型声明

```csharp
[EXEHandler(".dialog", typeof(DialogSequenceSO))]
public static class DialogVFSHandler
{
    public static void Handle(
        VFSResolvedContent content,
        SignalContext context,
        BasePackData pack,
        GraphRunner runner,
        string packInstanceID)
    {
        // 实例化：从引用加载 SO
        var sequence = content.GetUnityObject<DialogSequenceSO>();
        if (sequence == null) return;
        
        // 执行：遍历 Entries，发送事件
        ExecuteSequence(sequence);
        
        // 回写：传递执行结果给下游
        context.Args = new DialogResult { Sequence = sequence };
    }
}
```

### 数据结构

```csharp
// 主数据容器
public class DialogSequenceSO : ScriptableObject
{
    public string SequenceId;
    public List<ISequenceEntry> Entries;  // 混合列表
}

// 条目接口（多态基础）
public interface ISequenceEntry { }

// 具体条目类型
public class DialogEntry : ISequenceEntry
{
    public string Id, Speaker, Content;
}

public class AvatarEffectEntry : ISequenceEntry
{
    public string CharacterId;
    public Sprite Avatar;
    public FadeType FadeIn;
}
// ... 其他 EffectEntry
```

### 行为实现

```csharp
private static void ExecuteSequence(DialogSequenceSO sequence)
{
    foreach (var entry in sequence.Entries)
    {
        switch (entry)
        {
            case DialogEntry dialog:
                PostSystem.Instance.Send("Dialog.Show", dialog);
                break;
            case AvatarEffectEntry avatar:
                PostSystem.Instance.Send("Avatar.Show", avatar);
                break;
            // ... 其他条目类型
        }
    }
}
```

---

## VFS 载荷的本质

无论源格式是什么，最终都转化为后缀协议定义的 C# 数据对象。

| 源格式 | 中间形态 | 最终目标 |
|--------|----------|----------|
| SO 引用（Reference） | `VFSResolvedContent.UnityObject` | `content.GetUnityObject<DialogSequenceSO>()` |
| JSON 内嵌（Inline） | `VFSResolvedContent.RawText` | `content.ParseJson<DialogEntry>()` |
| CSV 文件 | 解析后转对象 | 同样转化为 C# 对象 |
| YAML | 解析后转对象 | 同样转化为 C# 对象 |

**核心原则**：Handler 不关心载荷从哪来，只关心最终拿到的是协议定义的那个 C# 对象。

```
VFS 节点（.dialog）
    ↓ 路径/内容
VFSContentResolver（解析层）
    ↓ 统一封装
VFSResolvedContent（中间封装）
    ↓ GetUnityObject<T>() / ParseJson<T>()
DialogSequenceSO（目标 C# 对象）
    ↓ 传给
Handle(...)（协议定义的行为）
```

**一句话**：载荷是手段，C# 对象是目的。协议定义时绑定的 `typeof(T)`，就是最终要拿到的对象类型。

---

## 纯数据后缀：Handle 可以为空

并非所有 VFS 协议都需要 Handle 方法体。这与大量仅有数据的类型是一致的。

| 类型 | Handle 作用 | 示例 |
|------|------------|------|
| **行为型** | 执行业务逻辑 | `.dialog` 遍历 Entries 发事件 |
| **数据型** | 仅传递数据（甚至为空） | `.config`、`.flag`、`.data` |

**纯数据后缀的 Handle：**

```csharp
[EXEHandler(".config", typeof(GameConfig))]
public static void Handle(VFSResolvedContent content, SignalContext context, ...)
{
    // 什么都不做，只是把数据塞给下游
    var config = content.GetUnityObject<GameConfig>();
    context.Args = config;  // 下游自己读
}
```

**或者更极端：**

```csharp
[EXEHandler(".flag", typeof(FlagData))]
public static void Handle(VFSResolvedContent content, SignalContext context, ...)
{
    // 连对象都不用解，直接透传引用路径
    context.Args = content.ReferencePath;
}
```

**关键认知**：
- Handle 是**入口**，不是**必须做事**
- 对于纯数据类型，定义三行依然成立，只是方法体为空或透传
- 数据通过 `context.Args` 流向后续节点，由它们决定怎么用

这与 C# 的纯数据类（DTO）、标记接口、配置对象完全一致——类型定义存在，行为可以没有。

---

## 设计约束

### 1. 线性执行

**Sequence 内部必须是纯线性的**，分支在 VFS 图层面做（节点连接），不在 Sequence 内部做。

```
VFS 图结构（分支在连接）：
    ┌──────────────┐
    │ start.dialog │
    └──────┬───────┘
           │
     ┌─────┴─────┐
     ▼           ▼
┌─────────┐ ┌─────────┐
│ pathA.dialog │ │ pathB.dialog │
└─────────┘ └─────────┘

❌ 错误：在 pathA.dialog 的 Entries 里做选择分支
✅ 正确：用 ConditionNode 决定走 A 还是 B
```

### 2. 同步执行

Handle 方法在当前帧同步完成，不阻塞：

```csharp
// ❌ 不要这样
await Task.Delay(1000);  // 会阻塞 GraphRunner

// ✅ 这样
PostSystem.Instance.Send("Dialog.Start", data);  // 发事件立即返回
```

### 3. 解耦通信

Handler 不直接调用 UI，通过 PostSystem 发事件：

```csharp
// ❌ 不要这样
FindObjectOfType<DialoguePanel>().Show(data);

// ✅ 这样
PostSystem.Instance.Send("Dialog.Show", data);
```

---

## 一句话总结

> **定义一个后缀 = 在运行时注册一个新类型（类型名 + 数据结构 + 方法实现）。**
>
> `.dialog` 就是这样一个运行时类型，把对话数据（SO）和行为（Handle）绑定在一起，信号流经过节点时自动触发。
