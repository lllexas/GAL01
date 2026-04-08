using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using SpaceTUI;

namespace GAL
{
    /// <summary>
    /// 转场管理器 - 全局画面控制
    /// </summary>
    public class TransitionManager : SpaceUIAnimator
    {
        [Header("转场层")]
        [SerializeField] private Image blackScreen;
        [SerializeField] private Image whiteScreen;
        
        [Header("默认时长")]
        [SerializeField] private float defaultFadeDuration = 0.5f;
        
        protected override string UIID => "TransitionManager";
        
        private Tween _currentTween;
        
        void Start()
        {
            // 初始状态：透明且隐藏
            if (blackScreen != null)
            {
                blackScreen.color = new Color(0, 0, 0, 0);
            }
            if (whiteScreen != null)
            {
                whiteScreen.color = new Color(1, 1, 1, 0);
            }
            
            Hide();
        }
        
        /// <summary>
        /// 黑场淡入（用于场景切换开头）
        /// </summary>
        public void FadeToBlack(float? duration = null, Action onComplete = null)
        {
            KillCurrent();
            Show();
            
            float dur = duration ?? defaultFadeDuration;
            
            _currentTween = blackScreen
                .DOFade(1f, dur)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => onComplete?.Invoke());
        }
        
        /// <summary>
        /// 黑场淡出（用于场景切换结尾）
        /// </summary>
        public void FadeFromBlack(float? duration = null, Action onComplete = null)
        {
            KillCurrent();
            Show();
            
            float dur = duration ?? defaultFadeDuration;
            
            _currentTween = blackScreen
                .DOFade(0f, dur)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => {
                    Hide();
                    onComplete?.Invoke();
                });
        }
        
        /// <summary>
        /// 黑场切换（淡入+淡出）
        /// </summary>
        public void FadeThroughBlack(Action middleAction, float? duration = null, Action onComplete = null)
        {
            float halfDur = (duration ?? defaultFadeDuration) / 2f;
            
            FadeToBlack(halfDur, () => {
                middleAction?.Invoke();
                FadeFromBlack(halfDur, onComplete);
            });
        }
        
        /// <summary>
        /// 白闪效果
        /// </summary>
        public void FlashWhite(float? duration = null, Action onComplete = null)
        {
            KillCurrent();
            Show();
            
            float dur = duration ?? 0.3f;
            
            whiteScreen.color = new Color(1, 1, 1, 0);
            
            // 快速白闪序列
            Sequence seq = DOTween.Sequence();
            seq.Append(whiteScreen.DOFade(1f, dur * 0.2f).SetEase(Ease.OutQuad));
            seq.Append(whiteScreen.DOFade(0f, dur * 0.8f).SetEase(Ease.InQuad));
            seq.OnComplete(() => {
                Hide();
                onComplete?.Invoke();
            });
            
            _currentTween = seq;
        }
        
        /// <summary>
        /// 强行中断当前转场
        /// </summary>
        public void Cut()
        {
            KillCurrent();
            if (blackScreen != null) blackScreen.color = new Color(0, 0, 0, 0);
            if (whiteScreen != null) whiteScreen.color = new Color(1, 1, 1, 0);
            Hide();
        }
        
        private void KillCurrent()
        {
            _currentTween?.Kill();
            _currentTween = null;
        }
        
        protected override void CloseAction() { }
    }
}
