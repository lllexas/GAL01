# NekoGraph 自动保存与专有扩展名实现

## 提交信息
- **Commit**: 4b115a2
- **标题**: 自动保存实现，专有扩展名实现，对齐ShaderGraph
- **日期**: 2026-04-10

---

## 概述

本提交全面提升了 NekoGraph 编辑器的工作流体验，实现了**ShaderGraph 风格**的保存机制：

1. **自动保存系统** - 内存优先，适时落盘
2. **`.nekograph` 专有扩展名** - 专属文件类型，Unity 原生支持
3. **改进的编辑器窗口** - 脏标记提示、拖拽加载、未保存警告

---

## 一、自动保存系统

### 设计哲学：对齐 ShaderGraph

| 特性 | 旧行为 | 新行为 (ShaderGraph 风格) |
|------|--------|---------------------------|
| 保存时机 | 每次修改立即写文件 | 内存优先，特定时机保存 |
| 用户感知 | 频繁 IO，可能卡顿 | 流畅编辑，手动/自动保存 |
| 退出提示 | 无 | 未保存提醒 |
| 进入 Play Mode | 可能丢失修改 | 自动提示保存 |

### 核心组件

```
Editor/
├── AutoSave/
│   ├── NekoGraphAutoSave.cs      # 自动保存管理器
│   └── NekoGraphSaveShortcut.cs  # Ctrl+S 快捷键拦截
└── _Base/
    └── BaseGraphWindow.cs        # PackWindow 实现保存接口
```

### 保存触发条件

```csharp
// NekoGraphAutoSave.cs
private const bool SAVE_BEFORE_PLAY_MODE = true;  // 进入 Play Mode 前
private const bool SAVE_ON_BUILD = true;          // 构建时
// Ctrl+S 快捷键
// 场景保存时连带保存
// 退出 Unity 时提示
```

### 关键时机的处理

#### 1. 进入 Play Mode

```csharp
private void OnPlayModeChanged(PlayModeStateChange state)
{
    if (SAVE_BEFORE_PLAY_MODE && state == PlayModeStateChange.ExitingEditMode)
    {
        var dirty = GetAllPackWindows().Where(w => w.IsDirty).ToList();
        if (dirty.Count > 0)
        {
            // 三选一对话框：保存 / 不保存 / 取消
            int result = EditorUtility.DisplayDialogComplex("未保存的 Pack",
                $"有 {dirty.Count} 个 Pack 未保存，是否保存后再进入 Play Mode?",
                "保存", "不保存", "取消");
            
            // 取消则阻止进入 Play Mode
            if (result == 2) EditorApplication.isPlaying = false;
        }
    }
}
```

#### 2. Ctrl+S 快捷键

```csharp
// 方案 A：AssetModificationProcessor 拦截
public class NekoGraphSaveShortcut : AssetModificationProcessor
{
    public static string[] OnWillSaveAssets(string[] paths)
    {
        // Unity 保存任何资产前调用
        var windows = Resources.FindObjectsOfTypeAll<PackWindow>()
            .Where(w => w.IsDirty).ToList();
        
        foreach (var window in windows)
            window.SilentSave();
        
        return paths; // 不拦截原保存
    }
}

// 方案 B：定时检测按键（备用）
private static void CheckSaveShortcut()
{
    Event e = Event.current;
    if (e != null && e.type == EventType.KeyDown)
    {
        if ((e.control || e.command) && e.keyCode == KeyCode.S)
        {
            SaveAllWindows("Ctrl+S 快捷键");
        }
    }
}
```

#### 3. 退出 Unity

```csharp
private bool OnWantsToQuit()
{
    var dirtyWindows = GetAllPackWindows().Where(w => w.IsDirty).ToList();
    if (dirtyWindows.Count > 0)
    {
        string message = $"有 {dirtyWindows.Count} 个 Pack 编辑器窗口未保存:\n" +
                         string.Join("\n", dirtyWindows.Select(w => $"- {w.Title}")) +
                         "\n\n是否保存后再退出?";
        
        int result = EditorUtility.DisplayDialogComplex("未保存的更改",
            message, "保存并退出", "不保存退出", "取消");
        
        switch (result)
        {
            case 0: // 保存并退出
                foreach (var window in dirtyWindows) window.SilentSave();
                return true;
            case 1: return true;  // 不保存退出
            default: return false; // 取消退出
        }
    }
    return true;
}
```

### 静默保存机制

```csharp
// PackWindow 实现 IPackWindowSaveable 接口
public void SilentSave()
{
    if (!HasValidAssetPath || _currentPack == null) return;

    try
    {
        _graphView.FlushToPack(_currentPack);           // 同步视图到数据
        File.WriteAllText(AssetPath, _currentPack.ToJson()); // 写文件
        IsDirty = false;                                 // 清除脏标记
        UpdateTitle();                                   // 更新标题
        Debug.Log($"[PackWindow] 已自动保存: {AssetPath}");
    }
    catch (Exception e)
    {
        Debug.LogError($"[PackWindow] 自动保存失败: {e.Message}");
    }
}
```

---

## 二、`.nekograph` 专有扩展名

### 为什么要专有扩展名

| 问题 | 解决方案 |
|------|---------|
| `.json` 太通用，双击用文本编辑器打开 | `.nekograph` 专属扩展名 |
| Unity 中无法区分 Pack 文件和普通 JSON | 自定义 ScriptedImporter |
| 没有图标和预览 | AssetEditor 提供 Inspector 预览 |

### 实现架构

```
Editor/Importers/
├── NekoGraphImporter.cs      # ScriptedImporter - 导入 .nekograph
└── NekoGraphAssetEditor.cs   # CustomEditor - Inspector 和双击处理
```

### 1. ScriptedImporter

**文件**: `Editor/Importers/NekoGraphImporter.cs`

```csharp
[ScriptedImporter(1, "nekograph")]
public class NekoGraphImporter : ScriptedImporter
{
    [Tooltip("Pack 数据预览")]
    public string PreviewJson;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        // 读取文件
        string jsonContent = File.ReadAllText(ctx.assetPath);
        
        // 截取前 1000 字符作为预览
        PreviewJson = jsonContent.Length > 1000
            ? jsonContent.Substring(0, 1000) + "\n... (truncated)"
            : jsonContent;

        // 创建 TextAsset 作为主对象
        var textAsset = new TextAsset(jsonContent);
        textAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);

        ctx.AddObjectToAsset("main", textAsset);
        ctx.SetMainObject(textAsset);

        // 验证 JSON 有效性
        ValidatePackData(ctx.assetPath, jsonContent);
    }
}
```

**效果**:
- `.nekograph` 文件在 Project 窗口显示为 TextAsset 图标
- 选中时 Inspector 显示 JSON 预览
- 修改文件后自动重新导入

### 2. AssetEditor 和双击打开

**文件**: `Editor/Importers/NekoGraphAssetEditor.cs`

```csharp
[CustomEditor(typeof(NekoGraphImporter))]
public class NekoGraphAssetEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        // 显示标题和打开按钮
        EditorGUILayout.LabelField("NekoGraph Pack", EditorStyles.largeLabel);
        
        if (GUILayout.Button("在 Pack Editor 中打开", GUILayout.Height(30)))
        {
            OpenInEditor();
        }

        // 显示预览内容
        if (!string.IsNullOrEmpty(importer.PreviewJson))
        {
            EditorGUILayout.LabelField("内容预览:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(importer.PreviewJson, GUILayout.Height(200));
        }
    }

    // 双击 .nekograph 文件时打开 PackWindow
    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var asset = EditorUtility.InstanceIDToObject(instanceID) as TextAsset;
        if (asset == null) return false;

        string path = AssetDatabase.GetAssetPath(instanceID);
        if (path.EndsWith(".nekograph", System.StringComparison.OrdinalIgnoreCase))
        {
            PackWindow.OpenWithAsset(path);
            return true; // 已处理，阻止默认行为
        }
        return false;
    }
}
```

**效果**:
- 双击 `.nekograph` 文件直接打开 PackWindow
- 无需通过菜单手动选择文件

### 3. 迁移工具

**文件**: `Editor/Tools/NekoGraphMigrationTool.cs`

```csharp
public class NekoGraphMigrationWindow : EditorWindow
{
    // 批量将 .json 转换为 .nekograph
    [MenuItem("Tools/NekoGraph/批量转换扩展名 (.json → .nekograph)")]
    
    // 功能：
    // 1. 预览模式 - 扫描显示哪些文件会被转换
    // 2. 试运行 - 默认开启，不实际执行
    // 3. 验证文件内容 - 检查是否包含 "PackID", "Nodes", "NodeID" 特征
    // 4. 执行迁移 - 使用 AssetDatabase.MoveAsset 保留引用
}
```

**使用流程**:
1. 打开 Tools/NekoGraph/批量转换扩展名
2. 选择源文件夹
3. 点击预览查看影响范围
4. 关闭试运行模式
5. 执行迁移

---

## 三、PackWindow 改进

### 1. 脏标记系统

```csharp
public class PackWindow : EditorWindow, IPackWindowSaveable
{
    public bool IsDirty { get; private set; }
    
    private void MarkDirty()
    {
        if (!IsDirty)
        {
            IsDirty = true;
            UpdateTitle();
        }
    }
    
    private void UpdateTitle()
    {
        string baseTitle = _currentPack?.PackID ?? "Pack Editor";
        string dirtyMark = IsDirty ? " *" : "";
        string newMark = HasValidAssetPath ? "" : " [New]";
        titleContent = new GUIContent($"{baseTitle}{dirtyMark}{newMark}");
    }
}
```

**效果**: 窗口标题显示 `PackID *` 表示有未保存修改

### 2. 内容变更检测

```csharp
// BaseGraphView.cs
public System.Action OnContentChanged;

private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
{
    bool hasChanges = false;

    if (changes.elementsToRemove != null)
    {
        foreach (var element in changes.elementsToRemove)
        {
            if (element is BaseNode) hasChanges = true;
            else if (element is Edge) hasChanges = true;
        }
    }

    if (changes.edgesToCreate != null) hasChanges = true;

    // 延迟触发避免频繁调用
    if (hasChanges)
    {
        EditorApplication.delayCall += () => OnContentChanged?.Invoke();
    }
    return changes;
}
```

### 3. 拖拽加载

```csharp
// PackWindow.cs - 支持从 Project 窗口拖拽 .nekograph 文件
private void SetupDragAndDrop()
{
    rootVisualElement.RegisterCallback<DragEnterEvent>(OnDragEnter);
    rootVisualElement.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
    rootVisualElement.RegisterCallback<DragPerformEvent>(OnDragPerform);
    rootVisualElement.RegisterCallback<DragLeaveEvent>(OnDragLeave);
}

private void OnDragPerform(DragPerformEvent evt)
{
    if (IsNekoGraphDrag())
    {
        DragAndDrop.AcceptDrag();
        var paths = DragAndDrop.paths;
        
        if (paths != null && paths.Length > 0)
        {
            string path = paths[0];
            
            // 询问是否保存当前
            if (IsDirty)
            {
                int result = EditorUtility.DisplayDialogComplex(...);
                // ... 处理选择
            }
            
            LoadFromPath(path);
        }
    }
}
```

**使用方式**: 从 Project 窗口拖拽 `.nekograph` 文件到编辑器窗口

### 4. 操作前保存提示

```csharp
private bool PromptSaveIfDirty(string action)
{
    if (!IsDirty) return true;

    int result = EditorUtility.DisplayDialogComplex("未保存的更改",
        $"当前 Pack 有未保存的更改，是否先保存？\n\n操作：{action}",
        "保存", "不保存", "取消");

    switch (result)
    {
        case 0: // 保存并继续
            SilentSave();
            return true;
        case 1: return true;  // 不保存继续
        default: return false; // 取消操作
    }
}

// 应用于：新建文件、加载文件、关闭窗口
private void NewFile()
{
    if (!PromptSaveIfDirty("创建新文件")) return;
    // ...
}

private void LoadData()
{
    if (!PromptSaveIfDirty("加载文件")) return;
    // ...
}
```

### 5. 关闭时自动保存

```csharp
private void OnDisable()
{
    if (IsDirty && HasValidAssetPath)
    {
        SilentSave();
        Debug.Log($"[PackWindow] 关闭时自动保存: {AssetPath}");
    }
}
```

---

## 四、文件格式变更

### 旧格式
```
Assets/Resources/MainStory.json
```

### 新格式
```
Assets/Resources/MainStory.nekograph
```

### 文件内容不变
`.nekograph` 仍然是 JSON 格式，只是扩展名改变，方便 Unity 识别和导入。

---

## 五、完整工作流

### 新建 Pack
1. Window → NekoGraph → Pack Editor
2. 编辑节点和连接
3. 点击保存（首次需要选择路径）
4. 生成 `.nekograph` 文件

### 编辑现有 Pack
1. 双击 `.nekograph` 文件，或在 Pack Editor 中加载
2. 编辑（标题显示 `*` 表示未保存）
3. Ctrl+S 保存，或关闭时自动保存

### 进入 Play Mode 测试
1. 有未保存修改时弹出提示
2. 选择保存/不保存/取消
3. 自动保存后再进入 Play Mode

### 构建发布
1. 构建时自动保存所有 dirty 的 Pack
2. `.nekograph` 文件作为 TextAsset 被打包

---

## 六、技术亮点

| 特性 | 实现方式 | 价值 |
|------|---------|------|
| **ShaderGraph 风格** | 内存优先 + 特定时机保存 | 流畅编辑体验 |
| **专有扩展名** | ScriptedImporter | Unity 原生支持，双击打开 |
| **脏标记** | 监听 GraphView 变化 | 用户清晰感知修改状态 |
| **防丢失** | 退出/Play Mode/切换文件前提示 | 数据安全 |
| **静默保存** | 无弹窗，后台写文件 | 不打断工作流 |
| **拖拽加载** | UIEvents Drag 事件 | 快速切换文件 |

---

## 七、菜单项汇总

| 菜单路径 | 功能 |
|---------|------|
| Window → NekoGraph → Pack Editor | 打开编辑器 |
| NekoGraph → 保存所有 Pack 窗口 (Ctrl+Shift+S) | 手动保存全部 |
| NekoGraph → 检查未保存的 Pack | 查看哪些窗口有修改 |
| Tools → NekoGraph → 批量转换扩展名 | JSON 转 NEKOGRAPH |
