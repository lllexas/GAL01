using UnityEngine;
using SpaceTUI;

namespace GAL
{
    /// <summary>
    /// 立绘舞台管理器 - 5槽位系统
    /// </summary>
    public class CharacterStage : SpaceUIAnimator
    {
        [Header("5个槽位")]
        [SerializeField] private CharacterAnimator[] slots = new CharacterAnimator[5];
        
        protected override string UIID => "CharacterStage";
        
        void Start()
        {
            期望显示面板 += OnShowStage;
            
            // 初始化槽位
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SlotIndex = i;
                }
            }
        }
        
        void OnShowStage(object data)
        {
            Show();
        }
        
        /// <summary>
        /// 显示角色到指定槽位
        /// </summary>
        public void ShowCharacter(int slotIndex, string characterID, Sprite sprite, bool fromLeft = true)
        {
            if (slotIndex < 0 || slotIndex >= 5) return;
            
            var slot = slots[slotIndex];
            if (slot == null) return;
            
            // 如果槽位有人，先让它退场
            if (slot.IsOccupied && slot.CurrentCharacterID != characterID)
            {
                slot.HideCharacter(fromLeft);
            }
            
            slot.ShowCharacter(characterID, sprite, fromLeft);
        }
        
        /// <summary>
        /// 隐藏指定槽位角色
        /// </summary>
        public void HideCharacter(int slotIndex, bool toRight = true)
        {
            if (slotIndex < 0 || slotIndex >= 5) return;
            slots[slotIndex]?.HideCharacter(toRight);
        }
        
        /// <summary>
        /// 切换角色表情
        /// </summary>
        public void ChangeExpression(int slotIndex, Sprite newSprite)
        {
            if (slotIndex < 0 || slotIndex >= 5) return;
            slots[slotIndex]?.ChangeExpression(newSprite);
        }
        
        /// <summary>
        /// 设置当前说话者（其他变暗）
        /// </summary>
        public void SetSpeaker(int? speakingSlotIndex)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || !slots[i].IsOccupied) continue;
                
                bool isSpeaking = speakingSlotIndex.HasValue && speakingSlotIndex.Value == i;
                slots[i].Highlight(isSpeaking);
            }
        }
        
        /// <summary>
        /// 角色强调动作
        /// </summary>
        public void Emphasize(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 5) return;
            slots[slotIndex]?.DoEmphasis();
        }
        
        /// <summary>
        /// 清空舞台
        /// </summary>
        public void ClearStage()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].IsOccupied)
                {
                    slots[i].HideCharacter(i % 2 == 0); // 奇偶不同方向退场
                }
            }
        }
        
        /// <summary>
        /// 获取角色的槽位索引
        /// </summary>
        public int? FindCharacterSlot(string characterID)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].CurrentCharacterID == characterID)
                    return i;
            }
            return null;
        }
        
        protected override void CloseAction() { }
    }
}
