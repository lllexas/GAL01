using UnityEngine;

namespace GAL
{
    /// <summary>
    /// 立绘舞台 - 纯容器，不管理 Slot
    /// Slot 各自独立响应事件
    /// </summary>
    public class CharacterStage : MonoBehaviour
    {
        [Header("5个槽位")]
        [SerializeField] private CharacterAnimator[] slots = new CharacterAnimator[5];
        
        void Start() { }
        
        // ========== 只读查询接口（供 DialoguePanel 等使用）==========
        
        public CharacterAnimator GetSlot(int index)
        {
            if (index < 0 || index >= 5) return null;
            return slots[index];
        }
        
        public int? FindCharacterSlot(string characterID)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].CurrentCharacterID == characterID)
                    return i;
            }
            return null;
        }
        
        public void SetSpeaker(int? slotIndex)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || !slots[i].IsOccupied) continue;
                slots[i].Highlight(slotIndex.HasValue && slotIndex.Value == i);
            }
        }
    }
}
