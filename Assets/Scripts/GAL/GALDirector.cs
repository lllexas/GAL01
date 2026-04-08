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
            // 纯事件驱动 - 不再直接查找和调用
            PostSystem.Instance.Send("期望高亮角色", new SpeakerData { slotIndex = slot });
        }
        
        CharacterAnimator GetSlot(int slot)
        {
            var stage = FindObjectOfType<CharacterStage>();
            return stage?.GetSlot(slot);
        }
        
        // ========== 对话 ==========
        
        public void ShowDialogue(DialogueData data, System.Action onComplete = null)
        {
            // 纯事件驱动 - 发送数据和回调
            PostSystem.Instance.Send("期望显示面板", "DialoguePanel");
            PostSystem.Instance.Send("对话数据", new DialoguePackage { data = data, onComplete = onComplete });
        }
        
        /// <summary>
        /// 对话数据包 - 包含回调
        /// </summary>
        public class DialoguePackage
        {
            public DialogueData data;
            public System.Action onComplete;
        }
            
        public void HideDialogue()
        {
            PostSystem.Instance.Send("期望隐藏面板", "DialoguePanel");
        }
            
        public void ShowChoices(string[] choices, System.Action<int> onSelect)
        {
            // 纯事件驱动
            PostSystem.Instance.Send("期望显示选项", new ChoicePackage { choices = choices, onSelect = onSelect });
        }
        
        /// <summary>
        /// 选项数据包
        /// </summary>
        public class ChoicePackage
        {
            public string[] choices;
            public System.Action<int> onSelect;
        }
    }
}
