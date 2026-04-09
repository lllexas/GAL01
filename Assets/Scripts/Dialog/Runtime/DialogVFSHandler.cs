
using NekoGraph;
using UnityEngine;

    /// <summary>
    /// Dialog VFS 处理器 - 处理 .dialog 后缀的 VFS 文件节点
    ///
    /// 当前策略：
    /// 1. 校验并解析 DialogSequenceSO
    /// 2. 输出条目摘要，便于排查数据问题
    /// 3. 若存在可用的 DialogPlayer，则交由其异步接管并返回 Nope
    /// 4. 若当前还没有真正的运行时播放器，则安全透传后续节点
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
                    default:
                        Debug.LogWarning($"[DialogVFSHandler] Entry[{i}] 类型未识别: {entry.GetType().FullName}");
                        break;
                }
            }

            // 3. 运行时播放器尚未真正接入时，不能返回 Nope 把流程挂死。
            bool handledAsync = DialogPlayer.TryPlay(sequence, context, pack, runner, packInstanceID);
            if (handledAsync)
                return HandleResult.Nope;

            Debug.LogWarning($"[DialogVFSHandler] '{sequence.name}' 当前走透传模式，未阻塞图流程");
            return HandleResult.Push;
        }
    }
