using GAL01.Dialog.Data;
using NekoGraph;
using Newtonsoft.Json;
using UnityEngine;

namespace GAL01.Dialog.Runtime
{
    /// <summary>
    /// Dialog VFS 处理器 - 处理 .dialog 后缀的 VFS 文件节点
    /// </summary>
    public static class DialogVFSHandler
    {
        [EXEHandler(".dialog", typeof(DialogSequenceSO))]
        public static void Handle(
            VFSResolvedContent content,
            SignalContext context,
            BasePackData pack,
            GraphRunner runner,
            string packInstanceID)
        {
            // 1. 获取 DialogSequenceSO
            var sequence = content.GetUnityObject<DialogSequenceSO>();
            
            // 2. SO 有效性校验
            if (sequence == null)
            {
                Debug.LogError("[DialogVFSHandler] 校验失败：DialogSequenceSO 为 null");
                return;
            }
            
            if (string.IsNullOrEmpty(sequence.SequenceId))
            {
                Debug.LogWarning($"[DialogVFSHandler] 校验警告：DialogSequenceSO '{sequence.name}' 的 SequenceId 为空");
            }
            
            if (sequence.Entries == null)
            {
                Debug.LogError($"[DialogVFSHandler] 校验失败：DialogSequenceSO '{sequence.name}' 的 Entries 为 null");
                return;
            }
            
            if (sequence.Entries.Count == 0)
            {
                Debug.LogWarning($"[DialogVFSHandler] 校验警告：DialogSequenceSO '{sequence.name}' 的 Entries 为空列表");
            }
            
            // 3. 输出 SO 的所有 entry
            Debug.Log($"[DialogVFSHandler] ===== DialogSequenceSO: {sequence.name} (ID: {sequence.SequenceId}) =====");
            Debug.Log($"[DialogVFSHandler] Entries 数量: {sequence.Entries.Count}");
            
            for (int i = 0; i < sequence.Entries.Count; i++)
            {
                var entry = sequence.Entries[i];
                if (entry == null)
                {
                    Debug.LogWarning($"[DialogVFSHandler] Entry[{i}] 为 null");
                    continue;
                }
                
                switch (entry)
                {
                    case DialogEntry dialog:
                        Debug.Log($"[DialogVFSHandler] Entry[{i}] [Dialog] ID={dialog.Id}, Speaker={dialog.Speaker}, Content={dialog.Content}");
                        break;
                    case AvatarEffectEntry avatar:
                        Debug.Log($"[DialogVFSHandler] Entry[{i}] [AvatarEffect] CharacterId={avatar.CharacterId}, FadeIn={avatar.FadeIn}");
                        break;
                    case VoiceEffectEntry voice:
                        Debug.Log($"[DialogVFSHandler] Entry[{i}] [VoiceEffect] TargetDialogId={voice.TargetDialogId}, VoiceClip={voice.VoiceClip?.name ?? "null"}");
                        break;
                    case CameraEffectEntry camera:
                        Debug.Log($"[DialogVFSHandler] Entry[{i}] [CameraEffect] Profile={camera.Profile?.name ?? "null"}");
                        break;
                    case ScreenFlashEntry flash:
                        Debug.Log($"[DialogVFSHandler] Entry[{i}] [ScreenFlash] Type={flash.FlashType}, Duration={flash.Duration}");
                        break;
                    default:
                        Debug.Log($"[DialogVFSHandler] Entry[{i}] [Unknown] Type={entry.GetType().Name}");
                        break;
                }
            }
            
            Debug.Log($"[DialogVFSHandler] ===== End of {sequence.name} =====");
            
            // 4. 将 SO 传递给下游
            context.Args = sequence;
        }
    }
}
