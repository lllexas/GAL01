#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GAL01.Dialog.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GAL01.Dialog.Editor
{
    [CustomEditor(typeof(DialogSequenceSO))]
    public class DialogSequenceSOEditor : UnityEditor.Editor
    {
        private DialogSequenceSO _target;
        private ReorderableList _entryList;
        private SerializedProperty _entriesProp;
        
        // 显示控制
        private int _displayCount = 20;
        private bool _mergeDialogEntries = false;
        private bool _compactEffectEntries = false;
        
        // 合并后的显示项缓存
        private List<DisplayItem> _displayItems = new();
        private bool _needsRebuildDisplay = true;

        private void OnEnable()
        {
            _target = (DialogSequenceSO)target;
            _entriesProp = serializedObject.FindProperty("Entries");
            
            _entryList = new ReorderableList(serializedObject, _entriesProp, 
                draggable: true, 
                displayHeader: true, 
                displayAddButton: true, 
                displayRemoveButton: true);

            _entryList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, $"条目序列 ({_target.Entries.Count})");
            };

            _entryList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (_mergeDialogEntries || _compactEffectEntries)
                    DrawVirtualElement(rect, index);
                else
                    DrawEntryElement(rect, index);
            };

            _entryList.elementHeightCallback = index => 
            {
                if (!_needsRebuildDisplay)
                    return GetElementHeight(index);
                return 30f;
            };

            _entryList.onAddDropdownCallback = (rect, list) =>
            {
                int insertIndex = list.index >= 0 ? list.index + 1 : list.count;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("💬 对话条目"), false, () => AddEntryAt<DialogEntry>(insertIndex));
                menu.AddItem(new GUIContent("👤 头像效果"), false, () => AddEntryAt<AvatarEffectEntry>(insertIndex));
                menu.AddItem(new GUIContent("🔊 语音效果"), false, () => AddEntryAt<VoiceEffectEntry>(insertIndex));
                menu.AddItem(new GUIContent("📷 镜头效果"), false, () => AddEntryAt<CameraEffectEntry>(insertIndex));
                menu.AddItem(new GUIContent("⚡ 屏幕闪烁"), false, () => AddEntryAt<ScreenFlashEntry>(insertIndex));
                menu.ShowAsContext();
            };
            
            _entryList.onChangedCallback = list =>
            {
                _needsRebuildDisplay = true;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // 检查是否需要重建显示项
            if (_needsRebuildDisplay)
            {
                RebuildDisplayItems();
                _needsRebuildDisplay = false;
            }

            EditorGUILayout.Space(10);

            // CSV 文件引用
            EditorGUILayout.LabelField("CSV 源", EditorStyles.boldLabel);
            DrawCsvField();

            EditorGUILayout.Space(5);

            // 基础配置
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SequenceId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));

            EditorGUILayout.Space(10);

            // 构建按钮
            GUI.enabled = _target.CsvSource != null;
            if (GUILayout.Button("🔃 从 CSV 构建序列", GUILayout.Height(30)))
            {
                BuildFromCsv();
                _needsRebuildDisplay = true;
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            // 统计
            int dialogCount = _target.Entries.OfType<DialogEntry>().Count();
            int effectCount = _target.Entries.OfType<EffectEntry>().Count();
            EditorGUILayout.LabelField($"统计: {dialogCount} 对话 / {effectCount} 演出", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // 显示控制
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("显示:", GUILayout.Width(40));
            _displayCount = EditorGUILayout.IntSlider(_displayCount, 5, 100, GUILayout.Width(200));
            EditorGUILayout.LabelField($"条目 (共{_target.Entries.Count})", GUILayout.Width(80));
            
            _mergeDialogEntries = GUILayout.Toggle(_mergeDialogEntries, "合并对话", GUILayout.Width(70));
            _compactEffectEntries = GUILayout.Toggle(_compactEffectEntries, "紧凑特效", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                _needsRebuildDisplay = true;
            }

            EditorGUILayout.Space(5);

            // 可拖拽排序列表（限制显示数量）
            int originalCount = _entryList.count;
            if ((_mergeDialogEntries || _compactEffectEntries) && _displayItems.Count > 0)
            {
                // 使用虚拟列表显示
                DrawVirtualList();
            }
            else
            {
                // 限制显示数量
                EditorGUILayout.LabelField($"显示前 {Mathf.Min(_displayCount, _target.Entries.Count)} 条 (共 {_target.Entries.Count} 条)", EditorStyles.miniLabel);
                _entryList.DoLayoutList();
            }

            EditorGUILayout.Space(10);
            
            // 选中条目详情编辑
            DrawSelectedEntryDetails();

            serializedObject.ApplyModifiedProperties();
        }
        
        private void RebuildDisplayItems()
        {
            _displayItems.Clear();
            int i = 0;
            
            while (i < _target.Entries.Count && _displayItems.Count < _displayCount)
            {
                var entry = _target.Entries[i];
                
                // 检查是否是连续对话的开始
                if (_mergeDialogEntries && entry is DialogEntry)
                {
                    int groupStart = i;
                    int groupEnd = i;
                    
                    // 找到连续对话的结尾
                    while (groupEnd + 1 < _target.Entries.Count && _target.Entries[groupEnd + 1] is DialogEntry)
                    {
                        groupEnd++;
                    }
                    
                    int groupSize = groupEnd - groupStart + 1;
                    
                    if (groupSize >= 3)
                    {
                        // 3条以上：显示头、省略、尾
                        _displayItems.Add(new DisplayItem 
                        { 
                            Type = DisplayItemType.DialogHead,
                            Entry = _target.Entries[groupStart],
                            RealIndex = groupStart,
                            Height = 28f
                        });
                        
                        _displayItems.Add(new DisplayItem 
                        { 
                            Type = DisplayItemType.DialogCollapsed, 
                            Entry = null,
                            CollapsedCount = groupSize - 2,
                            RealIndex = -1,
                            Height = 20f
                        });
                        
                        _displayItems.Add(new DisplayItem 
                        { 
                            Type = DisplayItemType.DialogTail,
                            Entry = _target.Entries[groupEnd],
                            RealIndex = groupEnd,
                            Height = 28f
                        });
                    }
                    else
                    {
                        // 2条或更少：正常显示
                        for (int j = groupStart; j <= groupEnd && _displayItems.Count < _displayCount; j++)
                        {
                            _displayItems.Add(new DisplayItem 
                            { 
                                Type = DisplayItemType.Normal, 
                                Entry = _target.Entries[j],
                                RealIndex = j,
                                Height = 28f
                            });
                        }
                    }
                    
                    i = groupEnd + 1;
                }
                else if (_compactEffectEntries && entry is EffectEntry)
                {
                    _displayItems.Add(new DisplayItem 
                    { 
                        Type = DisplayItemType.CompactEffect, 
                        Entry = entry,
                        RealIndex = i,
                        Height = 16f
                    });
                    i++;
                }
                else
                {
                    _displayItems.Add(new DisplayItem 
                    { 
                        Type = DisplayItemType.Normal, 
                        Entry = entry,
                        RealIndex = i,
                        Height = 28f
                    });
                    i++;
                }
            }
        }
        
        private void DrawVirtualList()
        {
            EditorGUILayout.LabelField($"显示 {_displayItems.Count} 条 (共 {_target.Entries.Count} 条) | 拖拽排序已启用", EditorStyles.miniLabel);
            
            Rect listRect = EditorGUILayout.GetControlRect(false, Mathf.Min(_displayItems.Count * 30f + 20f, 400f));
            _entryList.DoList(listRect);
        }
        
        private float GetElementHeight(int index)
        {
            if (index < 0 || index >= _displayItems.Count) return 30f;
            return _displayItems[index].Height;
        }
        
        private void DrawVirtualElement(Rect rect, int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= _displayItems.Count) return;
            
            var item = _displayItems[displayIndex];
            
            switch (item.Type)
            {
                case DisplayItemType.DialogHead:
                    DrawDialogHead(rect, item.Entry as DialogEntry, item.RealIndex);
                    break;
                case DisplayItemType.DialogTail:
                    DrawDialogTail(rect, item.Entry as DialogEntry, item.RealIndex);
                    break;
                case DisplayItemType.DialogCollapsed:
                    DrawDialogCollapsed(rect, item.CollapsedCount);
                    break;
                case DisplayItemType.CompactEffect:
                    DrawCompactEffect(rect, item.Entry);
                    break;
                default:
                    DrawEntryElement(rect, item.RealIndex);
                    break;
            }
        }
        
        private void DrawDialogHead(Rect rect, DialogEntry entry, int realIndex)
        {
            float iconWidth = 25f;
            Rect iconRect = new Rect(rect.x + 5f, rect.y + 3f, iconWidth, 20f);
            Rect textRect = new Rect(rect.x + iconWidth + 10f, rect.y + 3f, rect.width - iconWidth - 30f, 20f);
            Rect expandRect = new Rect(rect.x + rect.width - 25f, rect.y + 3f, 20f, 20f);
            
            GUI.Label(iconRect, "💬", EditorStyles.largeLabel);
            string preview = $"[{realIndex}] {entry.Speaker}: {(entry.Content?.Length > 20 ? entry.Content.Substring(0, 20) + "..." : entry.Content)}";
            GUI.Label(textRect, preview, EditorStyles.boldLabel);
            
            // 展开提示
            GUI.Label(expandRect, "▼", EditorStyles.miniLabel);
        }
        
        private void DrawDialogTail(Rect rect, DialogEntry entry, int realIndex)
        {
            float iconWidth = 25f;
            Rect iconRect = new Rect(rect.x + 5f, rect.y + 3f, iconWidth, 20f);
            Rect textRect = new Rect(rect.x + iconWidth + 10f, rect.y + 3f, rect.width - iconWidth - 15f, 20f);
            
            GUI.Label(iconRect, "💬", EditorStyles.largeLabel);
            string preview = $"[{realIndex}] {entry.Speaker}: {(entry.Content?.Length > 20 ? entry.Content.Substring(0, 20) + "..." : entry.Content)}";
            GUI.Label(textRect, preview, EditorStyles.boldLabel);
        }
        
        private void DrawDialogCollapsed(Rect rect, int count)
        {
            Rect textRect = new Rect(rect.x + 40f, rect.y + 2f, rect.width - 50f, 16f);
            GUI.Label(textRect, $"    ... {count} 条对话已折叠 ...", EditorStyles.miniLabel);
        }
        
        private void DrawCompactEffect(Rect rect, ISequenceEntry entry)
        {
            Color color = entry switch
            {
                CameraEffectEntry => new Color(0.8f, 0.4f, 0.4f),
                AvatarEffectEntry => new Color(0.4f, 0.6f, 0.8f),
                VoiceEffectEntry => new Color(0.4f, 0.8f, 0.4f),
                ScreenFlashEntry => new Color(0.9f, 0.9f, 0.4f),
                _ => Color.gray
            };
            
            string icon = entry switch
            {
                CameraEffectEntry => "📷",
                AvatarEffectEntry => "👤",
                VoiceEffectEntry => "🔊",
                ScreenFlashEntry => "⚡",
                _ => "●"
            };
            
            Rect iconRect = new Rect(rect.x + 5f, rect.y + 1f, 20f, 14f);
            Rect barRect = new Rect(rect.x + 30f, rect.y + 5f, rect.width - 40f, 6f);
            
            GUI.Label(iconRect, icon, EditorStyles.miniLabel);
            EditorGUI.DrawRect(barRect, color);
        }

        private void DrawEntryElement(Rect rect, int index)
        {
            if (index < 0 || index >= _target.Entries.Count) return;
            
            var entry = _target.Entries[index];
            if (entry == null) return;

            float iconWidth = 25f;
            float typeWidth = 60f;

            Rect iconRect = new Rect(rect.x, rect.y + 5, iconWidth, 20);
            Rect typeRect = new Rect(rect.x + iconWidth, rect.y + 5, typeWidth, 20);

            string icon = entry switch
            {
                DialogEntry => "💬",
                AvatarEffectEntry => "👤",
                VoiceEffectEntry => "🔊",
                CameraEffectEntry => "📷",
                ScreenFlashEntry => "⚡",
                _ => "📄"
            };

            GUI.Label(iconRect, icon, EditorStyles.largeLabel);
            string typeName = entry.GetType().Name.Replace("Entry", "");
            GUI.Label(typeRect, typeName, EditorStyles.boldLabel);

            if (entry is CameraEffectEntry cameraEntry)
            {
                Rect fieldRect = new Rect(rect.x + iconWidth + typeWidth + 5, rect.y + 5, 
                    rect.width - iconWidth - typeWidth - 20f, 20);
                
                EditorGUI.BeginChangeCheck();
                var newProfile = (CameraProfileSO)EditorGUI.ObjectField(
                    fieldRect, 
                    cameraEntry.Profile, 
                    typeof(CameraProfileSO), 
                    false
                );
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_target, "Change Camera Profile");
                    cameraEntry.Profile = newProfile;
                    EditorUtility.SetDirty(_target);
                }
            }
            else
            {
                Rect contentRect = new Rect(rect.x + iconWidth + typeWidth + 5, rect.y + 5, 
                    rect.width - iconWidth - typeWidth - 10, 20);
                    
                string content = entry switch
                {
                    DialogEntry d => $"{d.Speaker}: {(d.Content?.Length > 15 ? d.Content.Substring(0, 15) + "..." : d.Content)}",
                    AvatarEffectEntry a => a.CharacterId,
                    VoiceEffectEntry v => $"→ {v.TargetDialogId}",
                    ScreenFlashEntry s => s.FlashType.ToString(),
                    _ => ""
                };
                GUI.Label(contentRect, content, EditorStyles.miniLabel);
            }
        }

        private void DrawCsvField()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CsvSource"));
            
            if (EditorGUI.EndChangeCheck() && _target.CsvSource != null)
            {
                string path = AssetDatabase.GetAssetPath(_target.CsvSource);
                if (!path.Contains("/Resources/"))
                {
                    EditorUtility.DisplayDialog("警告", 
                        "CSV 文件必须放在 Resources 目录下才能被打包。", "确定");
                }
            }

            if (_target.CsvSource != null)
            {
                string path = AssetDatabase.GetAssetPath(_target.CsvSource);
                if (!path.Contains("/Resources/"))
                {
                    EditorGUILayout.HelpBox("⚠ CSV 不在 Resources 目录下，Build 后将无法访问！", MessageType.Error);
                }
            }
        }

        private void BuildFromCsv()
        {
            if (_target.CsvSource == null) return;

            string csvText = _target.CsvSource.text;
            var newDialogs = ParseCsv(csvText);

            if (newDialogs.Count == 0)
            {
                EditorUtility.DisplayDialog("构建失败", "CSV 解析结果为空", "确定");
                return;
            }

            // 保留 EffectEntry，重建 DialogEntry
            var effects = _target.Entries.OfType<EffectEntry>().ToList();
            var oldDialogs = _target.Entries.OfType<DialogEntry>().ToDictionary(d => d.Id);

            var newEntries = new List<ISequenceEntry>();
            int effectIdx = 0, dialogIdx = 0;

            while (effectIdx < effects.Count || dialogIdx < newDialogs.Count)
            {
                if (dialogIdx < newDialogs.Count)
                {
                    var newDialog = newDialogs[dialogIdx];
                    if (oldDialogs.TryGetValue(newDialog.Id, out var oldDialog))
                    {
                        oldDialog.Speaker = newDialog.Speaker;
                        oldDialog.Content = newDialog.Content;
                        newEntries.Add(oldDialog);
                    }
                    else
                    {
                        newEntries.Add(newDialog);
                    }
                    dialogIdx++;
                }

                if (effectIdx < effects.Count && effectIdx < newDialogs.Count)
                {
                    newEntries.Add(effects[effectIdx]);
                    effectIdx++;
                }
            }

            while (effectIdx < effects.Count)
            {
                newEntries.Add(effects[effectIdx]);
                effectIdx++;
            }

            Undo.RecordObject(_target, "Build Dialog Sequence");
            _target.Entries = newEntries;
            EditorUtility.SetDirty(_target);

            EditorUtility.DisplayDialog("构建完成", $"生成了 {newDialogs.Count} 条对话，保留了 {effects.Count} 条演出", "确定");
        }

        private List<DialogEntry> ParseCsv(string csvText)
        {
            var entries = new List<DialogEntry>();
            var lines = csvText.Split('\n');
            if (lines.Length < 2) return entries;

            var headers = ParseCsvLine(lines[0]);
            int idIdx = headers.FindIndex(h => h.ToLower() == "id");
            int speakerIdx = headers.FindIndex(h => h.ToLower() == "speaker");
            int contentIdx = headers.FindIndex(h => h.ToLower() == "content");

            if (idIdx < 0) idIdx = 0;
            if (speakerIdx < 0) speakerIdx = 1;
            if (contentIdx < 0) contentIdx = 2;

            int lineNum = 1;
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var values = ParseCsvLine(line);
                if (values.Count == 0) continue;

                string id = idIdx < values.Count ? values[idIdx] : $"{_target.SequenceId}_{lineNum:D3}";
                if (int.TryParse(id, out int numId))
                    id = $"{_target.SequenceId}_{numId:D3}";

                entries.Add(new DialogEntry
                {
                    Id = id,
                    Speaker = speakerIdx < values.Count ? values[speakerIdx] : "",
                    Content = contentIdx < values.Count ? values[contentIdx] : ""
                });
                lineNum++;
            }

            return entries;
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString().Trim());
            return result;
        }

        private void AddEntryAt<T>(int index) where T : class, ISequenceEntry, new()
        {
            Undo.RecordObject(_target, "Add Entry");
            
            if (index < 0) index = 0;
            if (index > _target.Entries.Count) index = _target.Entries.Count;
            
            _target.Entries.Insert(index, new T());
            EditorUtility.SetDirty(_target);
            
            _entryList.index = index;
            _needsRebuildDisplay = true;
        }

        private void DrawSelectedEntryDetails()
        {
            int selectedIndex = _entryList.index;
            if (selectedIndex < 0 || selectedIndex >= _target.Entries.Count)
                return;

            var entry = _target.Entries[selectedIndex];
            if (entry == null) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"编辑: {entry.GetType().Name.Replace("Entry", "")} [{selectedIndex}]", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();

            switch (entry)
            {
                case DialogEntry dialog:
                    DrawDialogEntryEditor(dialog);
                    break;
                case CameraEffectEntry camera:
                    DrawCameraEffectEntryEditor(camera);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_target);
            }
        }

        private void DrawDialogEntryEditor(DialogEntry entry)
        {
            entry.Id = EditorGUILayout.TextField("ID", entry.Id);
            entry.Speaker = EditorGUILayout.TextField("说话者", entry.Speaker);
            EditorGUILayout.LabelField("内容");
            entry.Content = EditorGUILayout.TextArea(entry.Content, GUILayout.MinHeight(60));
        }

        private void DrawCameraEffectEntryEditor(CameraEffectEntry entry)
        {
            EditorGUILayout.LabelField("相机效果配置", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("引用的 Profile", entry.Profile, typeof(CameraProfileSO), false);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            if (entry.Profile != null)
            {
                EditorGUILayout.LabelField("配置预览:", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Profile: {entry.Profile.name}", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                var keys = entry.Profile.GetAllKeys();
                if (keys.Count > 0)
                {
                    EditorGUILayout.LabelField("包含效果:", EditorStyles.miniLabel);
                    foreach (var key in keys)
                    {
                        if (entry.Profile.TryGetEffect(key, out var config))
                        {
                            EditorGUILayout.LabelField($"  • {key}: {config.EffectType} ({config.Duration}s)", EditorStyles.miniLabel);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("此 Profile 尚未配置效果", MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("未引用 Profile，请在列表中拖拽指定", MessageType.Warning);
            }
        }
        
        private enum DisplayItemType { Normal, DialogHead, DialogTail, DialogCollapsed, CompactEffect }
        
        private class DisplayItem
        {
            public DisplayItemType Type;
            public ISequenceEntry Entry;
            public int RealIndex;
            public int CollapsedCount;
            public float Height;
        }
    }
}
#endif
