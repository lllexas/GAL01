using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SpaceTUI;
using UnityEngine.EventSystems;

namespace GAL
{
    /// <summary>
    /// 立绘动画器 - 通过 UIID 响应事件
    /// </summary>
    public class CharacterAnimator : SpaceUIAnimator
    {
        [SerializeField] private Image characterImage;
        [SerializeField] private Vector3 offscreenLeft = new Vector3(-1200, 0, 0);
        [SerializeField] private Vector3 offscreenRight = new Vector3(1200, 0, 0);
        
        [Header("槽位")]
        [SerializeField] private int slotIndex;  // 0-4
        
        private Vector3 _homePosition;
        private Sprite _pendingSprite;
        private bool _fromLeft = true;
        
        public string CurrentCharacterID { get; private set; }
        public bool IsOccupied => !string.IsNullOrEmpty(CurrentCharacterID);
        
        // 直接根据 slotIndex 生成 UIID（修复序列化冲突）
        protected override string UIID => $"CharSlot{slotIndex}";
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.localPosition;
        }
        
        void Start()
        {
            期望显示面板 += OnShowPanel;
            期望隐藏面板 += OnHidePanel;
        }
        
        public void PrepareShow(string characterID, Sprite sprite, bool fromLeft)
        {
            CurrentCharacterID = characterID;
            _pendingSprite = sprite;
            _fromLeft = fromLeft;
        }
        
        void OnShowPanel(object data)
        {
            if (characterImage != null && _pendingSprite != null)
                characterImage.sprite = _pendingSprite;
            
            ResetScale();
            StopBreathing();
            
            transform.localPosition = _fromLeft ? offscreenLeft : offscreenRight;
            Show();
            
            transform.DOLocalMove(_homePosition, _fadeDuration)
                .SetEase(Ease.OutBack, 0.5f);
            PlayScaleAnimation();
        }
        
        void OnHidePanel(object data)
        {
            var targetPos = offscreenRight;
            
            transform.DOLocalMove(targetPos, _fadeDuration)
                .SetEase(Ease.InBack, 0.5f)
                .OnComplete(() => {
                    CurrentCharacterID = null;
                    _pendingSprite = null;
                    Hide();
                });
        }
        
        public void Highlight(bool isSpeaking)
        {
            if (characterImage == null) return;
            
            if (isSpeaking)
            {
                characterImage.DOFade(1f, 0.2f);
                transform.DOLocalMove(_homePosition, 0.3f);
            }
            else
            {
                characterImage.DOFade(0.6f, 0.2f);
                transform.DOLocalMove(_homePosition + Vector3.back * 50, 0.3f);
            }
        }
        
        public void ChangeExpression(Sprite newSprite)
        {
            if (characterImage == null) return;
            
            characterImage.DOFade(0f, 0.08f)
                .OnComplete(() => {
                    characterImage.sprite = newSprite;
                    characterImage.DOFade(1f, 0.08f);
                });
        }
        
        protected override void CloseAction() { }
    }
}
