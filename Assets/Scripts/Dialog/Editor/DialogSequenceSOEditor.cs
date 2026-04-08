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
        private int _scrollOffset = 0;
        private bool _mergeDialogEntries = false;
        private bool _compactEffectEntries = false;

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
                DrawEntryElement(rect, index);
            };

            _entryList.elementHeightCallback = index => 30f;

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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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
            }
            GUI.enabled = true;

            EditorGUILayout.Space(10);

            // 统计
            int dialogCount = _target.Entries.OfType<DialogEntry>().Count();
            int effectCount = _target.Entries.OfType<EffectEntry>().Count();
            EditorGUILayout.LabelField($"统计: {dialogCount} 对话 / {effectCount} 演出", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            // 显示控制
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("显示:", GUILayout.Width(40));
            _displayCount = EditorGUILayout.IntSlider(_displayCount, 5, 100, GUILayout.Width(200));
            EditorGUILayout.LabelField($"条目 (共{_target.Entries.Count})", GUILayout.Width(80));
            
            _mergeDialogEntries = GUILayout.Toggle(_mergeDialogEntries, "合并对话", GUILayout.Width(70));
            _compactEffectEntries = GUILayout.Toggle(_compactEffectEntries, "紧凑特效", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 可拖拽排序列表
            _entryList.DoLayoutList();

            EditorGUILayout.Space(10);
            
            // 选中条目详情编辑
            DrawSelectedEntryDetails();

            serializedObject.ApplyModifiedProperties();
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
    }
}
#endif
