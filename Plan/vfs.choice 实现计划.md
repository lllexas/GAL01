# vfs.choice 实现计划

## 目标

实现 `.choice` 后缀的 VFS 节点，支持分支选项功能。

## 数据格式

### 存储方式
- **格式**: 手动录入的 CSV 文件
- **后缀**: `.choice`
- **内容**: 每行一个选项文本

### CSV 示例
```csv
选项A
选项B
选项C
```

### 运行时数据结构
```csharp
public class ChoiceData
{
    public List<string> Options;  // 选项文本列表
}
```

## 执行流程

```
VFSNode (.choice)
       │
       ▼
┌─────────────────┐
│  ChoiceVFSHandler │
│  (Wait 模式)     │
└────────┬────────┘
         │ ① 解析 CSV → List<string>
         │ ② 返回 HandleResult.Wait
         │ ③ 唤起选项面板
         ▼
┌─────────────────┐
│   ChoicePanel    │
│   (前端自治)     │
│                 │
│   [选项A]       │
│   [选项B]       │
│   [选项C]       │
└────────┬────────┘
         │ 用户点击选项
         ▼
┌─────────────────┐
│   回调通知       │
│   selectedIndex  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  信号分发到对应   │
│  Output 端口     │
│  (0→第0个output) │
└─────────────────┘
```

## 职责划分

| 层级 | 职责 |
|------|------|
| **ChoiceVFSHandler** | 解析 CSV，返回 Wait，唤起面板，等待回调，分发信号 |
| **ChoicePanel** | 接收选项列表，渲染按钮，用户选择后回调索引 |

## 关键实现点

### 1. Handler 注册
```csharp
public static class ChoiceVFSHandler
{
    [EXEHandler(".choice", typeof(ChoiceData))]
    public static HandleResult Handle(
        VFSResolvedContent content,
        SignalContext context,
        BasePackData pack,
        GraphRunner runner,
        string packInstanceID,
        Action continueAction)
    {
        // 解析 CSV
        // 唤起 ChoicePanel
        // 返回 Wait
    }
}
```

### 2. 信号分发逻辑
```csharp
// 用户选择第 index 个选项后
// 分发到第 index 个 output 端口
EnqueueSignal(pack, vfsNode.ChildNodeIDs[index], context);
```

### 3. 与 DialogPlayer 的区别
| 特性 | DialogPlayer | ChoicePanel |
|------|-------------|-------------|
| 数据 | DialogSequenceSO (ScriptableObject) | CSV 文本列表 |
| 输出 | 单一流水线 | 多分支 output |
| 回调 | 通知完成 | 通知选择索引 |
| 职责 | 播放序列 | 分支决策 |

## 节点配置

```
[ChoiceNode]
     │
     ├── Output 0 → 选择选项A后的流程
     ├── Output 1 → 选择选项B后的流程
     └── Output 2 → 选择选项C后的流程
```

- Output 端口数量 = CSV 中的选项数量
- 运行时动态匹配，index 超出时 fallback 到默认分支

## 文件位置建议

```
Assets/
├── Scripts/
│   ├── Dialog/
│   │   ├── Runtime/
│   │   │   ├── ChoiceVFSHandler.cs    # Handler 实现
│   │   │   └── ChoiceData.cs          # 数据模型
│   │   └── ...
│   └── GAL/
│       ├── ChoicePanel.cs             # 前端面板
│       └── ...
└── Resources/
    └── Choices/
        └── example.choice               # CSV 文件
```

## 后续扩展

- [ ] 选项超时机制（默认选择）
- [ ] 选项条件显示（基于变量）
- [ ] 选项图标/颜色自定义
