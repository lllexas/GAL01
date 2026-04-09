using NekoGraph;
using UnityEngine;
using System.Collections;
using GAL;

    /// <summary>
    /// 对话播放器 - 后端推送包 → 前端自治渲染 → 回调通知完成
    /// 
    /// 架构模式：文档流（Document Stream）
    /// - 后端推送完整 DialogSequenceSO 包（剧本）
    /// - 前端自主控制渲染节奏（打字机、动画、等待点击）
    /// - 播放完成回调 runner.InjectSignal 恢复 NekoGraph 流
    /// </summary>
    public class DialogPlayer : MonoBehaviour
    {
        private static DialogPlayer _instance;
        private Coroutine _playingCoroutine;
        
        // 播放状态
        private bool _isWaitingForClick = false;
        private System.Action _onClickCallback;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        
        /// <summary>
        /// 后端推送入口：接收完整包，开始自治渲染
        /// </summary>
        public static bool TryPlay(
            DialogSequenceSO sequence,
            SignalContext context,
            BasePackData pack,
            GraphRunner runner,
            string packInstanceID)
        {
            if (sequence == null)
            {
                Debug.LogError("[DialogPlayer] sequence 为 null");
                return false;
            }

            if (_instance == null)
            {
                Debug.LogWarning($"[DialogPlayer] 场景中不存在 DialogPlayer，序列 '{sequence.name}' 透传");
                return false;
            }

            // 中断之前的播放
            if (_instance._playingCoroutine != null)
            {
                _instance.StopCoroutine(_instance._playingCoroutine);
            }
            
            // 启动新播放协程，完成后回调
            _instance._playingCoroutine = _instance.StartCoroutine(
                _instance.PlaySequence(sequence, () => {
                    runner.InjectSignal(context);
                })
            );
            
            return true; // true = 已接管，后端挂起等待回调
        }
        
        /// <summary>
        /// 前端自治渲染：解包 → 逐条播放 → 等待用户输入 → 完成回调
        /// </summary>
        private IEnumerator PlaySequence(DialogSequenceSO sequence, System.Action onComplete)
        {
            Debug.Log($"[DialogPlayer] 开始播放序列: {sequence.name} ({sequence.Entries.Count} 条)");
            
            foreach (var entry in sequence.Entries)
            {
                if (entry == null) continue;
                
                switch (entry)
                {
                    case DialogEntry dialog:
                        yield return PlayDialogEntry(dialog);
                        break;
                        
                    case AvatarEffectEntry avatar:
                        yield return PlayAvatarEntry(avatar);
                        break;
                        
                    case ScreenFlashEntry flash:
                        yield return PlayFlashEntry(flash);
                        break;
                        
                    case VoiceEffectEntry voice:
                        yield return PlayVoiceEntry(voice);
                        break;
                        
                    case CameraEffectEntry camera:
                        yield return PlayCameraEntry(camera);
                        break;
                }
            }
            
            Debug.Log($"[DialogPlayer] 序列播放完成: {sequence.name}");
            _playingCoroutine = null;
            onComplete?.Invoke();
        }
        
        // ========== 各类型条目渲染 ==========
        
        private IEnumerator PlayDialogEntry(DialogEntry dialog)
        {
            // 发事件给 DialoguePanel，等待点击继续
            _isWaitingForClick = true;
            bool clicked = false;
            
            PostSystem.Instance.Send("期望显示面板", "DialoguePanel");
            PostSystem.Instance.Send("对话数据", new DialoguePackage {
                data = new DialogueData {
                    characterName = dialog.Speaker,
                    text = dialog.Content
                },
                onComplete = () => clicked = true
            });
            
            // 等待点击
            yield return new WaitUntil(() => clicked);
            _isWaitingForClick = false;
        }
        
        private IEnumerator PlayAvatarEntry(AvatarEffectEntry avatar)
        {
            // 发送角色显示事件
            PostSystem.Instance.Send("期望显示面板", new CharacterShowData {
                slotIndex = 0, // TODO: 从 Avatar 配置获取槽位
                id = avatar.CharacterId,
                sprite = avatar.Avatar
            });
            
            yield return new WaitForSeconds(0.1f); // 最小帧延迟
        }
        
        private IEnumerator PlayFlashEntry(ScreenFlashEntry flash)
        {
            var transType = flash.FlashType == ScreenFlashType.White 
                ? TransitionType.FlashWhite 
                : TransitionType.FadeToBlack;
                
            PostSystem.Instance.Send("期望显示面板", "TransitionManager");
            PostSystem.Instance.Send("转场数据", new TransitionData {
                type = transType,
                duration = flash.Duration
            });
            
            yield return new WaitForSeconds(flash.Duration);
        }
        
        private IEnumerator PlayVoiceEntry(VoiceEffectEntry voice)
        {
            // TODO: 接入音频系统
            yield return null;
        }
        
        private IEnumerator PlayCameraEntry(CameraEffectEntry camera)
        {
            // TODO: 接入镜头系统
            yield return null;
        }
    }
