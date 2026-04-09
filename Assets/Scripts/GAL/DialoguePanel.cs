using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using System;
using SpaceTUI;

namespace GAL
{
    /// <summary>
    /// 对话数据 - Model 层
    /// </summary>
    public class DialogueData
    {
        public string characterName;
        public string text;
        public Color nameColor = Color.white;
        public float typingSpeed = 0.03f;
        public string characterID;
        public int? slotIndex;
    }
    
    /// <summary>
    /// 说话者高亮数据 - 纯 MVVM 模式
    /// </summary>
    public class SpeakerData
    {
        public int? slotIndex;  // null = 无人说话
    }
    
    /// <summary>
    /// 对话数据包 - 包含回调（后端推送用）
    /// </summary>
    public class DialoguePackage
    {
        public DialogueData data;
        public System.Action onComplete;
    }
    
    /// <summary>
    /// 选项数据包
    /// </summary>
    public class ChoicePackage
    {
        public string[] choices;
        public System.Action<int> onSelect;
    }
    
    /// <summary>
    /// 对话面板 - 独立响应事件
    /// </summary>
    public class DialoguePanel : SpaceUIAnimator
    {
        [Header("显示组件")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        
        [Header("选项组件")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        
        [Header("设置")]
        [SerializeField] private float defaultTypingSpeed = 0.03f;
        
        protected override string UIID => "DialoguePanel";
        
        private Tween _typewriterTween;
        private bool _isTyping = false;
        private bool _waitingForChoice = false;
        private Action _onAdvance;
        private Action<int> _onChoiceSelected;
        
        void Start()
        {
            期望显示面板 += OnShowPanel;
            期望隐藏面板 += OnHidePanel;
            鼠标点击 += OnClickAdvance;
            
            // 订阅纯事件驱动的数据包
            PostSystem.Instance.On("对话数据", OnDialogueDataReceived);
            PostSystem.Instance.On("期望显示选项", OnChoicesReceived);
            
            Hide();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 安全注销：场景卸载时 Instance 可能已为 null
            PostSystem.Instance?.Off("对话数据", OnDialogueDataReceived);
            PostSystem.Instance?.Off("期望显示选项", OnChoicesReceived);
        }
        
        void OnDialogueDataReceived(object data)
        {
            if (data is GALDirector.DialoguePackage package)
            {
                ShowDialogue(package.data, package.onComplete);
            }
        }
        
        void OnChoicesReceived(object data)
        {
            if (data is GALDirector.ChoicePackage package)
            {
                ShowChoices(package.choices, package.onSelect);
            }
        }
        
        void OnShowPanel(object data)
        {
            if (data is DialogueData dialogueData)
            {
                if (!IsVisible) Show();
                ShowDialogueInternal(dialogueData);
            }
            else if (!IsVisible)
            {
                FadeIn();
            }
        }
        
        void OnHidePanel(object data)
        {
            FadeOut();
        }
        
        void OnClickAdvance(PointerEventData eventData)
        {
            if (_waitingForChoice) return;
            
            if (_isTyping)
            {
                _typewriterTween?.Complete();
            }
            else
            {
                _onAdvance?.Invoke();
                _onAdvance = null;
            }
        }
        
        void ShowDialogueInternal(DialogueData data)
        {
            _waitingForChoice = false;
            
            // 角色名
            if (nameText != null)
            {
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
            }
            
            // 高亮说话者 - 纯事件驱动
            var stage = FindObjectOfType<CharacterStage>();
            int? speakerSlot = null;
            if (stage != null && !string.IsNullOrEmpty(data.characterID))
            {
                speakerSlot = data.slotIndex ?? stage.FindCharacterSlot(data.characterID);
            }
            
            // 发送事件而非直接调用 - MVVM 模式
            PostSystem.Instance.Send("期望高亮角色", new SpeakerData { slotIndex = speakerSlot });
            
            // 打字机
            float speed = data.typingSpeed > 0 ? data.typingSpeed : defaultTypingSpeed;
            StartTypewriter(data.text, speed);
        }
        
        void StartTypewriter(string text, float speed)
        {
            _typewriterTween?.Kill();
            _isTyping = true;
            if (dialogueText != null) dialogueText.text = "";
            
            int currentIndex = 0;
            _typewriterTween = DOTween.To(
                () => currentIndex,
                x => {
                    currentIndex = x;
                    if (dialogueText != null) dialogueText.text = text.Substring(0, x);
                },
                text.Length,
                text.Length * Mathf.Max(0.01f, speed)
            )
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                _isTyping = false;
                if (dialogueText != null) dialogueText.text = text;
            });
        }
        
        // ========== 公共 API（供 GALDirector 调用）==========
        
        public void ShowDialogue(DialogueData data, Action onComplete = null)
        {
            _onAdvance = onComplete;
            OnShowPanel(data);
        }
        
        public void ShowChoices(string[] choices, Action<int> onSelect)
        {
            _waitingForChoice = true;
            _onChoiceSelected = onSelect;
            
            if (choiceContainer == null) return;
            
            foreach (Transform child in choiceContainer)
                Destroy(child.gameObject);
            
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                var btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                var btn = btnObj.GetComponent<Button>();
                
                if (btnText != null) btnText.text = choices[i];
                btn.onClick.AddListener(() => {
                    choiceContainer.gameObject.SetActive(false);
                    _waitingForChoice = false;
                    _onChoiceSelected?.Invoke(index);
                    _onChoiceSelected = null;
                });
            }
            
            choiceContainer.gameObject.SetActive(true);
        }
        
        public void Clear()
        {
            if (dialogueText != null) dialogueText.text = "";
            if (nameText != null) nameText.text = "";
        }
        
        protected override void CloseAction() => FadeOut();
    }
}
