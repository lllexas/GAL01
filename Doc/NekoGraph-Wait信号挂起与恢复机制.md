# NekoGraph Wait 信号挂起与恢复机制

## 提交信息
- **Commit**: b575640
- **标题**: feat: 给 SignalContext 添加唯一 ID；实现 Wait 状态下的信号挂起与恢复机制
- **日期**: 2026-04-10

---

## 概述

本提交实现了 **Wait 信号传播模式**的完整生命周期管理，解决了异步 Handler 在暂停信号传播后如何正确恢复的问题。这是 VFS 协议三态返回值（`Continue`/`Wait`/`Error`）中 `Wait` 模式的最终完善。

---

## 核心问题

### Wait 模式的困境

```csharp
// 问题：Handler 返回 Wait 后，后续信号怎么办？
public HandleResult Handle(object content, SignalContext ctx, ...)
{
    ShowChoice("选A还是选B?", onSelect: (choice) => {
        // 用户选择后，需要继续传播信号
        // 但此时 Handle 早已返回，上下文已丢失
    });
    return HandleResult.Wait; // 告诉 Runner：别继续传播了
}
```

**核心矛盾**:
- Handler 必须立即返回 `Wait` 以暂停同步传播
- 但异步回调发生时，需要恢复原本应该传播的子信号
- 这些子信号不能在内存中丢失

---

## 解决方案：信号挂起机制

### 核心设计

1. **SignalContext 获得唯一 ID** - 用于在字典中精确定位
2. **SuspendedSignals 字典** - 专门存储被挂起的信号
3. **闭包持有 ID 列表** - continueAction 通过 ID 列表精确恢复

### 时序图

```
┌─────────┐     ┌─────────────┐     ┌─────────────┐     ┌──────────────┐
│  信号    │     │ VFSNodeStrategy│    │   Handler    │     │ 挂起信号字典  │
└────┬────┘     └──────┬──────┘     └──────┬──────┘     └───────┬──────┘
     │                 │                   │                    │
     │ ExecuteNode     │                   │                    │
     │────────────────>│                   │                    │
     │                 │                   │                    │
     │                 │  准备 continueAction                  │
     │                 │  (闭包持有 suspendedIds 列表)          │
     │                 │                   │                    │
     │                 │ Invoke Handler    │                    │
     │                 │──────────────────>│                    │
     │                 │                   │                    │
     │                 │ 返回 Wait         │                    │
     │                 │<──────────────────│                    │
     │                 │                   │                    │
     │                 │ SuspendSignals()  │                    │
     │                 │────────────────────────────────────────>│
     │                 │                   │                    │
     │                 │ 子信号写入        │                    │
     │                 │ SuspendedSignals  │                    │
     │                 │────────────────────────────────────────>│
     │                 │                   │                    │
     │                 │ Handler 返回后     │                    │
     │                 │ 不传播子信号       │                    │
     │                 │                   │                    │
     │   【异步回调发生时】                 │                    │
     │                 │                   │                    │
     │                 │ continueAction()  │                    │
     │                 │ (调用闭包)         │                    │
     │                 │                   │                    │
     │                 │ ResumeSuspendedSignals()              │
     │                 │ 根据 ID 列表精确恢复 │                   │
     │                 │────────────────────────────────────────>│
     │                 │                   │                    │
     │                 │ 子信号从字典移除    │                    │
     │                 │ 加入活跃队列        │                    │
     │                 │<────────────────────────────────────────│
     │                 │                   │                    │
```

---

## 关键代码变更

### 1. SignalContext 添加唯一 ID

**文件**: `Runtime/Runner_Analyser/SignalContext.cs`

```csharp
[Serializable]
public class SignalContext
{
    /// <summary>
    /// 信号唯一 ID - 用于挂起字典的 key 喵~
    /// 字段初始化器保证任何构造路径（含反序列化）都有值，JSON 覆盖后恢复存档 ID 喵~
    /// </summary>
    public string SignalId = Guid.NewGuid().ToString("N");
    
    // ... 其他字段
}
```

**设计要点**:
- 使用字段初始化器，确保新创建的信号自动获得 ID
- JSON 反序列化时会覆盖为存档中的 ID，保证存档/读档一致性
- `ToString("N")` 生成 32 位无连字符 GUID，节省存储空间

### 2. BasePackData 添加挂起字典

**文件**: `Runtime/Base/BasePackData.cs`

```csharp
public class BasePackData
{
    /// <summary>
    /// 挂起信号字典 - Wait 状态下被冻结的后续子信号喵~
    /// Key: 子信号自身的 SignalId
    /// Value: 子信号本体
    /// 存档时保存，Handler 完成后通过闭包持有的 key 列表精确 Remove + Enqueue 恢复喵~
    /// </summary>
    [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
    [Tooltip("挂起信号字典")]
    public Dictionary<string, SignalContext> SuspendedSignals = new Dictionary<string, SignalContext>();
}
```

**设计要点**:
- 使用 `ItemTypeNameHandling.Auto` 确保多态信号类型正确序列化
- Key 是信号自身的 SignalId，Value 是信号本体
- 字典结构支持存档/读档，游戏重启后能正确恢复挂起状态

### 3. VFSNodeStrategy 实现挂起/恢复逻辑

**文件**: `Runtime/GraphVSF/VFSNodeStrategy.cs`

#### 挂起信号

```csharp
private List<string> SuspendSignals(BasePackData pack, IEnumerable<string> targetIds, SignalContext context)
{
    // 标记当前节点为已处理（避免重复执行）
    if (pack.Nodes.TryGetValue(context.CurrentNodeId, out var currentNode))
        currentNode.IsChecked = true;

    var suspendedIds = new List<string>();
    foreach (var targetId in targetIds)
    {
        // 克隆父信号，创建子信号
        var newSignal = context.Clone(copyPath: true);
        newSignal.RecordConnection(new ConnectionData(context.CurrentNodeId, -1, targetId, -1));
        newSignal.CurrentNodeId = targetId;
        
        // 存入挂起字典
        pack.SuspendedSignals[newSignal.SignalId] = newSignal;
        suspendedIds.Add(newSignal.SignalId);
    }
    return suspendedIds;
}
```

#### 恢复信号

```csharp
private void ResumeSuspendedSignals(BasePackData pack, List<string> suspendedIds)
{
    foreach (var id in suspendedIds)
    {
        if (pack.SuspendedSignals.TryGetValue(id, out var signal))
        {
            pack.ActiveSignals.Enqueue(signal);
            pack.SuspendedSignals.Remove(id);
        }
    }
}
```

**关键设计 - 闭包持有 ID 列表**:

```csharp
// continueAction 在 handler 返回前构建，此时还不知道是否 Wait
List<string> suspendedIds = null;
System.Action continueAction = () => ResumeSuspendedSignals(pack, suspendedIds);

// Handler 执行
result = handler.Invoke(content, context, pack, runner, packInstanceID, continueAction);

// 如果是 Wait 模式，挂起信号并填充 suspendedIds
if (result == HandleResult.Wait)
{
    suspendedIds = SuspendSignals(pack, vfsNode.ChildNodeIDs, context);
}
```

**为什么用 ID 列表而不是直接引用？**

| 方案 | 问题 |
|------|------|
| 直接持有 Signal 引用 | 闭包可能长期存在，引用会导致信号无法 GC，且存档时引用失效 |
| 持有 ID 列表 | 轻量、可序列化、支持精确恢复、不影响 GC |

### 4. GraphRunner 加载时恢复挂起信号

**文件**: `Runtime/Runner_Analyser/GraphRunner.cs`

```csharp
public void Init(Dictionary<string, BasePackData> packDataDict)
{
    PersistentGuidToInstancedPackDict = packDataDict;

    foreach (var pack in packDataDict.Values)
    {
        // 1. 恢复挂起信号 - Wait 状态下被冻结的信号现在重新入队
        if (pack.SuspendedSignals != null && pack.SuspendedSignals.Count > 0)
        {
            int totalSuspendedCount = 0;

            foreach (var signal in pack.SuspendedSignals.Values)
            {
                pack.ActiveSignals.Enqueue(signal);
                totalSuspendedCount++;
            }

            if (EnableDebugLog && totalSuspendedCount > 0)
                Debug.Log($"[GraphRunner] Pack '{pack.PackID}' 恢复了 {totalSuspendedCount} 个挂起信号喵~");

            pack.SuspendedSignals.Clear();
        }

        // 2. 启动未启动的 Pack
        if (!pack.HasStarted && !string.IsNullOrEmpty(pack.RootNodeId))
        {
            pack.ActiveSignals.Enqueue(new SignalContext(pack.RootNodeId, null));
            pack.HasStarted = true;
        }
    }
}
```

---

## Wait 模式完整使用示例

### 1. 定义带选择的后缀 Handler

```csharp
[VFSHandler("choice")]
public static HandleResult HandleChoice(object content, SignalContext ctx, 
    BasePackData pack, GraphRunner runner, string packInstanceID, Action continueAction)
{
    var data = content as ChoiceData;
    
    // 显示选择 UI，传入回调
    DialogueUI.ShowChoice(data.Options, (selectedIndex) => {
        // 用户做出选择后，恢复信号传播
        ctx.SetVariable("choice_result", selectedIndex);
        continueAction(); // 关键：调用 continueAction 恢复挂起的子信号
    });
    
    return HandleResult.Wait; // 暂停同步传播
}
```

### 2. 图结构示例

```
┌─────────┐    ┌─────────────┐    ┌───────────┐    ┌───────────┐
│  Start  │───>│ choice.node │───>│ 分支 A    │    │ 分支 B    │
└─────────┘    └─────────────┘    └───────────┘    └───────────┘
                                     ▲                  ▲
                                     │                  │
                              【这些子信号被挂起，
                               等待 continueAction】
```

### 3. 执行时序

```
T1: 执行 Start → 传播到 choice.node
T2: choice Handler 执行，显示 UI，返回 Wait
T3: VFSNodeStrategy 将 "分支 A" 和 "分支 B" 信号挂起到 SuspendedSignals
T4: Runner 继续处理其他信号，choice 节点完成（不传播子信号）
...
T10: 用户点击选择
T11: 回调执行，调用 continueAction
T12: ResumeSuspendedSignals 将子信号从字典移入 ActiveSignals
T13: 子信号被正常执行，根据条件走分支 A 或 B
```

---

## 存档与读档支持

### 存档时

```json
{
  "PackID": "MainStory",
  "ActiveSignals": [...],
  "SuspendedSignals": {
    "a1b2c3d4": { "SignalId": "a1b2c3d4", "CurrentNodeId": "branch_a", ... },
    "e5f6g7h8": { "SignalId": "e5f6g7h8", "CurrentNodeId": "branch_b", ... }
  },
  // ... 其他字段
}
```

### 读档时

1. JSON 反序列化恢复 `SuspendedSignals` 字典
2. `GraphRunner.Init()` 将所有挂起信号移入 `ActiveSignals`
3. Runner 主循环继续执行，仿佛从未中断

---

## 设计亮点

| 设计 | 优势 |
|------|------|
| **SignalId + 字典** | O(1) 查找，精确恢复，支持存档 |
| **闭包持有 ID 列表** | 避免长期引用，GC 友好，可序列化 |
| **字典清空策略** | 恢复后立即 Remove，不残留无用数据 |
| **Init 统一恢复** | 读档逻辑与正常启动一致，无特殊分支 |

---

## 与三态返回值的配合

```csharp
public enum HandleResult
{
    Continue,  // 同步继续传播子信号（默认行为）
    Wait,      // 挂起子信号，通过 continueAction 异步恢复 ✨ 本提交实现
    Error      // 传播终止，记录错误
}
```

**Wait 模式的意义**:
- 让 VFS Handler 能够处理**异步操作**（网络请求、用户输入、动画播放等）
- 同时保持图的**声明式结构**，不破坏数据驱动设计
- 与 Unity 的协程/回调模式无缝集成
