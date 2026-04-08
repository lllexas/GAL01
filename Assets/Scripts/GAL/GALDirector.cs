using UnityEngine;
using SpaceTUI;

namespace GAL
{
    /// <summary>
    /// GAL 导演 - 通过 UIID 发送事件
    /// </summary>
    public class GALDirector : MonoBehaviour
    {
        public static GALDirector Instance { get; private set; }
        
        void Awake() => Instance = this;
        
        // ========== 转场 ==========
        
        public void FadeToBlack(float? duration = null)
        {
            PostSystem.Instance.Send("期望显示面板", "TransitionManager");
        }
            
        public void FadeFromBlack(float? duration = null)
        {
            PostSystem.Instance.Send("期望隐藏面板", "TransitionManager");
        }
        
        // ========== 立绘 ==========
        
        public void ShowCharacter(int slot, string characterID, Sprite sprite, bool fromLeft = true)
        {
            // 1. 准备数据
            var slotObj = GetSlot(slot);
            slotObj?.PrepareShow(characterID, sprite, fromLeft);
            
            // 2. 发送事件，通过 UIID 匹配
            string uiid = $"CharSlot{slot}";
            PostSystem.Instance.Send("期望显示面板", uiid);
        }
            
        public void HideCharacter(int slot)
        {
            string uiid = $"CharSlot{slot}";
            PostSystem.Instance.Send("期望隐藏面板", uiid);
        }
        
        public void ChangeExpression(int slot, Sprite sprite)
        {
            GetSlot(slot)?.ChangeExpression(sprite);
        }
        
        public void SetSpeaker(int? slot)
        {
            var stage = FindObjectOfType<CharacterStage>();
            stage?.SetSpeaker(slot);
        }
        
        CharacterAnimator GetSlot(int slot)
        {
            var stage = FindObjectOfType<CharacterStage>();
            return stage?.GetSlot(slot);
        }
        
        // ========== 对话 ==========
        
        public void ShowDialogue(DialogueData data, System.Action onComplete = null)
        {
            PostSystem.Instance.Send("期望显示面板", "DialoguePanel");
            
            var panel = FindObjectOfType<DialoguePanel>();
            panel?.ShowDialogue(data, onComplete);
        }
            
        public void HideDialogue()
        {
            PostSystem.Instance.Send("期望隐藏面板", "DialoguePanel");
        }
            
        public void ShowChoices(string[] choices, System.Action<int> onSelect)
        {
            var panel = FindObjectOfType<DialoguePanel>();
            panel?.ShowChoices(choices, onSelect);
        }
    }
}
