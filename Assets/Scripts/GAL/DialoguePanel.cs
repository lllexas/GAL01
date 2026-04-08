using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using SpaceTUI;

namespace GAL
{
    /// <summary>
    /// 对话数据
    /// </summary>
    public class DialogueData
    {
        public string characterName;
        public string text;
        public Color nameColor = Color.white;
        public float typingSpeed = 0.03f;
        public string characterID; // 关联立绘
        public int? slotIndex;     // 指定槽位，null 自动查找
    }
    
    /// <summary>
    /// 对话面板 - GAL 核心交互
    /// </summary>
    public class DialoguePanel : SpaceUIAnimator, IPointerClickHandler
    {
        [Header("显示组件")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image dialogueBg;
        
        [Header("选项组件")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        
        [Header("设置")]
        [SerializeField] private float defaultTypingSpeed = 0.03f;
        
        protected override string UIID => "DialoguePanel";
        
        // 状态
        private Tween _typewriterTween;
        private bool _isTyping = false;
        private bool _waitingForChoice = false;
        private string _currentFullText;
        
        // 回调
        private Action _onAdvance;
        private Action<int> _onChoiceSelected;
        
        // 依赖
        private CharacterStage _characterStage;
        
        void Start()
        {
            _characterStage = FindObjectOfType<CharacterStage>();
            
            期望显示面板 += OnShowPanel;
            鼠标点击 += OnPointerClick;
            
            Hide();
        }
        
        void OnShowPanel(object data)
        {
            if (!IsVisible) FadeIn();
        }
        
        /// <summary>
        /// 显示对话
        /// </summary>
        public void ShowDialogue(DialogueData data, Action onComplete = null)
        {
            if (!IsVisible) Show();
            
            _waitingForChoice = false;
            _onAdvance = onComplete;
            
            // 角色名
            if (!string.IsNullOrEmpty(data.characterName))
            {
                string hex = ColorUtility.ToHtmlStringRGB(data.nameColor);
                nameText.text = $"<color=#{hex}>{data.characterName}</color>";
                nameText.gameObject.SetActive(true);
            }
            else
            {
                nameText.gameObject.SetActive(false);
            }
            
            // 高亮当前说话者
            if (_characterStage != null && !string.IsNullOrEmpty(data.characterID))
            {
                var slot = data.slotIndex ?? _characterStage.FindCharacterSlot(data.characterID);
                _characterStage.SetSpeaker(slot);
            }
            else
            {
                _characterStage?.SetSpeaker(null);
            }
            
            // 打字机效果
            _currentFullText = data.text;
            StartTypewriter(data.text, data.typingSpeed > 0 ? data.typingSpeed : defaultTypingSpeed);
        }
        
        /// <summary>
        /// 显示选项
        /// </summary>
        public void ShowChoices(string[] choices, Action<int> onSelect)
        {
            _waitingForChoice = true;
            _onChoiceSelected = onSelect;
            
            // 清空旧选项
            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 生成选项按钮
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                var btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                var btn = btnObj.GetComponent<Button>();
                
                btnText.text = choices[i];
                btn.onClick.AddListener(() => SelectChoice(index));
            }
            
            choiceContainer.gameObject.SetActive(true);
        }
        
        void SelectChoice(int index)
        {
            choiceContainer.gameObject.SetActive(false);
            _waitingForChoice = false;
            _onChoiceSelected?.Invoke(index);
            _onChoiceSelected = null;
        }
        
        void StartTypewriter(string text, float speed)
        {
            _typewriterTween?.Kill();
            _isTyping = true;
            dialogueText.text = "";
            
            // 保护：速度不能太快
            float charDuration = Mathf.Max(0.01f, speed);
            
            int currentIndex = 0;
            _typewriterTween = DOTween.To(
                () => currentIndex,
                x => {
                    currentIndex = x;
                    dialogueText.text = text.Substring(0, x);
                },
                text.Length,
                text.Length * charDuration
            )
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                _isTyping = false;
                dialogueText.text = text; // 确保完整
            });
        }
        
        void SkipTypewriter()
        {
            _typewriterTween?.Complete();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_waitingForChoice) return;
            
            if (_isTyping)
            {
                // 打字中点击 = 跳过
                SkipTypewriter();
            }
            else
            {
                // 打完了点击 = 继续
                _onAdvance?.Invoke();
                _onAdvance = null;
            }
        }
        
        /// <summary>
        /// 清空对话
        /// </summary>
        public void Clear()
        {
            dialogueText.text = "";
            nameText.text = "";
        }
        
        protected override void CloseAction()
        {
            FadeOut();
        }
    }
}
