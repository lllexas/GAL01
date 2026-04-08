using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using SpaceTUI;

namespace GAL
{
    /// <summary>
    /// 立绘动画器 - 单个角色控制
    /// </summary>
    public class CharacterAnimator : SpaceUIAnimator
    {
        [SerializeField] private Image characterImage;
        
        [Header("位置配置")]
        [SerializeField] private Vector3 offscreenLeft = new Vector3(-1200, 0, 0);
        [SerializeField] private Vector3 offscreenRight = new Vector3(1200, 0, 0);
        
        private Vector3 _homePosition;
        private string _currentCharacterID;
        
        public int SlotIndex { get; set; } = -1;
        public bool IsOccupied => !string.IsNullOrEmpty(_currentCharacterID);
        public string CurrentCharacterID => _currentCharacterID;
        
        protected override string UIID => $"Character_{SlotIndex}";
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.localPosition;
            Hide();
        }
        
        /// <summary>
        /// 显示角色
        /// </summary>
        public void ShowCharacter(string characterID, Sprite sprite, bool fromLeft = true)
        {
            _currentCharacterID = characterID;
            characterImage.sprite = sprite;
            
            // 重置状态
            ResetScale();
            StopBreathing();
            
            // 起始位置
            transform.localPosition = fromLeft ? offscreenLeft : offscreenRight;
            
            Show(); // 立即显示
            
            // 滑入 + 弹性效果
            transform.DOLocalMove(_homePosition, _fadeDuration)
                .SetEase(Ease.OutBack, 0.5f);
                
            PlayScaleAnimation(); // 轨道 C
        }
        
        /// <summary>
        /// 隐藏角色
        /// </summary>
        public void HideCharacter(bool toRight = true)
        {
            var targetPos = toRight ? offscreenRight : offscreenLeft;
            
            transform.DOLocalMove(targetPos, _fadeDuration)
                .SetEase(Ease.InBack, 0.5f)
                .OnComplete(() => {
                    _currentCharacterID = null;
                    Hide();
                });
        }
        
        /// <summary>
        /// 切换表情（不换人）
        /// </summary>
        public void ChangeExpression(Sprite newSprite)
        {
            if (characterImage.sprite == newSprite) return;
            
            // 简单淡切
            characterImage.DOFade(0f, 0.08f)
                .OnComplete(() => {
                    characterImage.sprite = newSprite;
                    characterImage.DOFade(1f, 0.08f);
                });
        }
        
        /// <summary>
        /// 高亮（当前说话者）
        /// </summary>
        public void Highlight(bool isSpeaking)
        {
            if (isSpeaking)
            {
                // 说话：不透明 + 正常位置
                characterImage.DOFade(1f, 0.2f);
                transform.DOLocalMove(_homePosition, 0.3f);
            }
            else
            {
                // 旁听：半透明 + 略微后退
                characterImage.DOFade(0.6f, 0.2f);
                transform.DOLocalMove(_homePosition + Vector3.back * 50, 0.3f);
            }
        }
        
        /// <summary>
        /// 强调动作
        /// </summary>
        public void DoEmphasis()
        {
            // 短暂放大
            transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f);
        }
        
        protected override void CloseAction() { }
    }
}
