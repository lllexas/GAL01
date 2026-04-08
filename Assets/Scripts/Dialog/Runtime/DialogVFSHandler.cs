using GAL01.Dialog.Data;
using NekoGraph;
using Newtonsoft.Json;
using UnityEngine;

namespace GAL01.Dialog.Runtime
{
    /// <summary>
    /// Dialog VFS 处理器 - 处理 .dialog 后缀的 VFS 文件节点
    /// 
    /// 支持两种载荷方式：
    /// 1. JSON 直接嵌入：VFSResolvedContent.ParseJson&lt;DialogEntry&gt;()
    /// 2. SO 引用：VFSResolvedContent.GetUnityObject&lt;DialogSequenceSO&gt;()
    /// </summary>
    public static class DialogVFSHandler
    {
        /// <summary>
        /// 处理 .dialog 文件节点
        /// 运行时执行：将对话内容传递给对话系统显示
        /// </summary>
        [EXEHandler(".dialog", typeof(DialogEntry))]
        public static void Handle(
            VFSResolvedContent content,
            SignalContext context,
            BasePackData pack,
            GraphRunner runner,
            string packInstanceID)
        {
            // 尝试作为 SO 引用解析
            var sequenceSO = content.GetUnityObject<DialogSequenceSO>();
            if (sequenceSO != null)
            {
                ExecuteSequence(sequenceSO, context);
                return;
            }

            // 尝试作为 JSON 解析（单条或数组）
            if (content.HasText)
            {
                var text = content.GetTextOrEmpty().Trim();
                
                // 尝试解析为 DialogEntry 数组
                if (text.StartsWith("["))
                {
                    try
                    {
                        var entries = JsonConvert.DeserializeObject<DialogEntry[]>(text);
                        if (entries != null && entries.Length > 0)
                        {
                            ExecuteEntries(entries, context);
                            return;
                        }
                    }
                    catch { /* 解析失败，继续尝试单条 */ }
                }

                // 尝试解析为单条 DialogEntry
                try
                {
                    var entry = JsonConvert.DeserializeObject<DialogEntry>(text);
                    if (entry != null)
                    {
                        ExecuteSingle(entry, context);
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DialogVFSHandler] JSON 解析失败：{e.Message}");
                }
            }

            Debug.LogWarning("[DialogVFSHandler] 无法解析 .dialog 内容");
        }

        /// <summary>
        /// 执行完整的对话序列
        /// </summary>
        private static void ExecuteSequence(DialogSequenceSO sequence, SignalContext context)
        {
            Debug.Log($"[DialogVFSHandler] 执行对话序列：{sequence.name}，共 {sequence.DialogCount} 条对话");
            
            // 将序列传递给对话系统
            // 这里只是示例，实际需要对接你的对话显示系统
            foreach (var entry in sequence.Entries)
            {
                switch (entry)
                {
                    case DialogEntry dialog:
                        Debug.Log($"[{dialog.Speaker}] {dialog.Content}");
                        break;
                    case AvatarEffectEntry avatar:
                        Debug.Log($"[演出] 切换头像：{avatar.CharacterId}");
                        break;
                    case VoiceEffectEntry voice:
                        Debug.Log($"[演出] 播放语音：{voice.VoiceClip?.name}");
                        break;
                    case CameraEffectEntry camera:
                        Debug.Log($"[演出] 镜头效果：{(camera.Profile != null ? camera.Profile.name : "(未指定)")}");
                        break;
                }
            }

            // 将序列通过 context 传递给下游节点
            context.Args = sequence;
        }

        /// <summary>
        /// 执行多条对话条目
        /// </summary>
        private static void ExecuteEntries(DialogEntry[] entries, SignalContext context)
        {
            foreach (var entry in entries)
            {
                ExecuteSingle(entry, context);
            }
        }

        /// <summary>
        /// 执行单条对话
        /// </summary>
        private static void ExecuteSingle(DialogEntry entry, SignalContext context)
        {
            Debug.Log($"[Dialog] [{entry.Id}] {entry.Speaker}: {entry.Content}");
            context.Args = entry;
        }
    }
}
