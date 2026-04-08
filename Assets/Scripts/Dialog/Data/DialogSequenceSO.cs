using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAL01.Dialog.Data
{
    /// <summary>
    /// DialogSequenceSO - 对话序列资源
    /// 
    /// CSV 源文件放在 Resources 目录下，直接拖拽引用
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogSequence", menuName = "GAL01/Dialog/Dialog Sequence")]
    public class DialogSequenceSO : ScriptableObject
    {
        [Tooltip("CSV 源文件（必须放在 Resources 目录下）")]
        public TextAsset CsvSource;

        [Tooltip("序列标识，用于生成 DialogEntry 的 ID 前缀")]
        public string SequenceId;

        [Tooltip("描述信息")]
        [TextArea(2, 4)]
        public string Description;

        [SerializeReference]
        [Tooltip("混合条目序列（对话文本 + 演出效果）")]
        public List<ISequenceEntry> Entries = new();

        // ==================== 运行时查询 API ====================

        /// <summary>
        /// 获取指定索引的 DialogEntry（跳过 EffectEntry）
        /// </summary>
        public DialogEntry GetDialogEntry(int dialogIndex)
        {
            int count = 0;
            foreach (var entry in Entries)
            {
                if (entry is DialogEntry dialog)
                {
                    if (count == dialogIndex)
                        return dialog;
                    count++;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有纯对话条目的数量
        /// </summary>
        public int DialogCount
        {
            get
            {
                int count = 0;
                foreach (var entry in Entries)
                    if (entry is DialogEntry)
                        count++;
                return count;
            }
        }

        /// <summary>
        /// 根据 ID 查找 DialogEntry
        /// </summary>
        public DialogEntry FindDialogById(string id)
        {
            foreach (var entry in Entries)
            {
                if (entry is DialogEntry dialog && dialog.Id == id)
                    return dialog;
            }
            return null;
        }
    }
}
