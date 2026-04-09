using GAL01.Dialog.Data;
using NekoGraph;
using UnityEngine;

namespace GAL01.Dialog.Runtime
{
    /// <summary>
    /// Dialog VFS 处理器 - 处理 .dialog 后缀的 VFS 文件节点
    /// 
    /// 继电器模式：
    /// 1. 校验并解析 DialogSequenceSO
    /// 2. 将序列交给 DialogPlayer（MonoBehaviour 协程）播放
    /// 3. 返回 Nope，由 DialogPlayer 播放完成后自行 InjectSignal 恢复信号流
    /// </summary>
    public static class DialogVFSHandler
    {
        [EXEHandler(".dialog", typeof(DialogSequenceSO))]
        public static HandleResult Handle(
            VFSResolvedContent content,
            SignalContext context,
            BasePackData pack,
            GraphRunner runner,
            string packInstanceID)
        {
            // 1. 获取并校验 DialogSequenceSO
            var sequence = content.GetUnityObject<DialogSequenceSO>();
            if (sequence == null)
            {
                Debug.LogError("[DialogVFSHandler] 校验失败：DialogSequenceSO 为 null");
                return HandleResult.Error;
            }
            
            if (sequence.Entries == null)
            {
                Debug.LogError($"[DialogVFSHandler] 校验失败：'{sequence.name}' 的 Entries 为 null");
                return HandleResult.Error;
            }
            
            // 2. 输出所有 Entry（调试用）
            Debug.Log($"[DialogVFSHandler] 播放序列: {sequence.name} (ID: {sequence.SequenceId}, Entries: {sequence.Entries.Count})");
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
                        Debug.Log($"[{i}] Dialog: {dialog.Speaker} - {dialog.Content}");
                        break;
                    case AvatarEffectEntry avatar:
                        Debug.Log($"[{i}] Avatar: {avatar.CharacterId}");
                        break;
                    case VoiceEffectEntry voice:
                        Debug.Log($"[{i}] Voice: {voice.VoiceClip?.name ?? "null"}");
                        break;
                    case CameraEffectEntry camera:
                        Debug.Log($"[{i}] Camera: {camera.Profile?.name ?? "null"}");
                        break;
                    case ScreenFlashEntry flash:
                        Debug.Log($"[{i}] Flash: {flash.FlashType} ({flash.Duration}s)");
                        break;
                }
            }
            
            // 3. 继电器模式：交给 DialogPlayer，返回 Nope
            // DialogPlayer 完成后会调用 runner.InjectSignal 恢复信号流
            DialogPlayer.Play(sequence, context, pack, runner, packInstanceID);
            
            return HandleResult.Nope;
        }
    }
}
