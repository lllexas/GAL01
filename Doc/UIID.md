# UIID 命名规范

## 概述

UIID（UI ID）是用于 SpaceUIAnimator 识别具体面板和组件的唯一标识符。

在 `SpaceUIAnimator.MatchUIID()` 中使用，用于将 PostSystem 事件路由到正确的 UI 组件。

## 命名规律

**格式：`{ComponentName}{Index}`**

- **ComponentName**：组件类型的英文缩写或名称
- **Index**：该类型组件的序号（从 0 开始）
- **连接方式**：直接连接，不使用括号、下划线或其他分隔符

## 已有规范

### CharSlot（角色槽位）

```csharp
// CharacterAnimator.cs
protected override string UIID => $"CharSlot{slotIndex}";
```

| UIID | 含义 |
|------|------|
| `CharSlot0` | 第 0 个角色槽位 |
| `CharSlot1` | 第 1 个角色槽位 |
| ... | ... |
| `CharSlot4` | 第 4 个角色槽位 |

**使用场景**：DialogPlayer 发送角色显示/隐藏事件时

```csharp
PostSystem.Instance.Send("期望显示角色", new RoutedRequest<CharacterShowData>
{
    uiid = "CharSlot0",  // 指定槽位 0
    data = ...
});
```

### ChoiceBar（选项条）

```csharp
// ChoiceBar.cs
protected override string UIID => $"ChoiceBar{optionIndex}";
```

| UIID | 含义 |
|------|------|
| `ChoiceBar0` | 第 0 个选项条 |
| `ChoiceBar1` | 第 1 个选项条 |
| ... | ... |
| `ChoiceBar4` | 第 4 个选项条 |

**使用场景**：不直接使用 UIID（ChoiceBar 通过 [Subscribe] 反射式订阅 "显示选项" 事件），但每个 Bar 在编辑器中配置固定的 optionIndex，对应的 UIID 在内部使用

## 添加新的组件时

若要新增 SpaceUIAnimator 子类且需要多个实例索引：

1. **选择合理的 ComponentName**
   - 使用英文，避免中文
   - 简洁但能清晰表达功能
   - 例：`CharSlot`、`ChoiceBar`、`ItemSlot` 等

2. **编辑器中配置 Index**
   - 每个实例在 Inspector 中设置固定的序号
   - 从 0 开始递增

3. **在类顶部 XML 文档注释中说明**
   ```csharp
   /// <summary>
   /// ...
   /// UIID 格式：ComponentName{index}  (例: ComponentName0, ComponentName1, ...)
   /// </summary>
   ```

4. **在本文档中补充新规范**
