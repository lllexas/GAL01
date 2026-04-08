using UnityEngine;

namespace GAL
{
    /// <summary>
    /// GAL 导演 - 协调各系统的入口
    /// </summary>
    public class GALDirector : MonoBehaviour
    {
        [SerializeField] private TransitionManager transition;
        [SerializeField] private CharacterStage stage;
        [SerializeField] private DialoguePanel dialoguePanel;
        
        public static GALDirector Instance { get; private set; }
        
        void Awake()
        {
            Instance = this;
        }
        
        // ========== 转场快捷方法 ==========
        
        public void FadeToBlack(float? duration = null, System.Action onComplete = null)
            => transition?.FadeToBlack(duration, onComplete);
            
        public void FadeFromBlack(float? duration = null, System.Action onComplete = null)
            => transition?.FadeFromBlack(duration, onComplete);
            
        public void FadeThroughBlack(System.Action middleAction, float? duration = null, System.Action onComplete = null)
            => transition?.FadeThroughBlack(middleAction, duration, onComplete);
            
        public void FlashWhite(float? duration = null, System.Action onComplete = null)
            => transition?.FlashWhite(duration, onComplete);
        
        // ========== 立绘快捷方法 ==========
        
        public void ShowCharacter(int slot, string id, Sprite sprite, bool fromLeft = true)
            => stage?.ShowCharacter(slot, id, sprite, fromLeft);
            
        public void HideCharacter(int slot, bool toRight = true)
            => stage?.HideCharacter(slot, toRight);
            
        public void ChangeExpression(int slot, Sprite sprite)
            => stage?.ChangeExpression(slot, sprite);
            
        public void SetSpeaker(int? slot)
            => stage?.SetSpeaker(slot);
            
        public void ClearStage()
            => stage?.ClearStage();
        
        // ========== 对话快捷方法 ==========
        
        public void ShowDialogue(DialogueData data, System.Action onComplete = null)
            => dialoguePanel?.ShowDialogue(data, onComplete);
            
        public void ShowChoices(string[] choices, System.Action<int> onSelect)
            => dialoguePanel?.ShowChoices(choices, onSelect);
            
        public void ClearDialogue()
            => dialoguePanel?.Clear();
        
        // ========== 组合操作 ==========
        
        /// <summary>
        /// 场景切换：黑场 + 清理 + 新场景
        /// </summary>
        public void ChangeScene(System.Action setupAction, float? duration = null, System.Action onComplete = null)
        {
            FadeThroughBlack(() => {
                ClearStage();
                ClearDialogue();
                setupAction?.Invoke();
            }, duration, onComplete);
        }
    }
}
