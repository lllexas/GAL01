using System;
using UnityEngine;

namespace GAL01.Dialog.Data
{
    /// <summary>
    /// 演出效果条目基类
    /// </summary>
    public abstract class EffectEntry : ISequenceEntry { }

    /// <summary>
    /// 头像切换效果
    /// </summary>
    [Serializable]
    public class AvatarEffectEntry : EffectEntry
    {
        public string CharacterId;
        public Sprite Avatar;
        public FadeType FadeIn = FadeType.Instant;
    }

    /// <summary>
    /// 语音播放效果
    /// </summary>
    [Serializable]
    public class VoiceEffectEntry : EffectEntry
    {
        /// <summary>关联的 DialogEntry.Id，空表示紧跟前一条对话</summary>
        public string TargetDialogId;
        public AudioClip VoiceClip;
    }

    /// <summary>
    /// 镜头效果 - 只引用 CameraProfileSO
    /// </summary>
    [Serializable]
    public class CameraEffectEntry : EffectEntry
    {
        public CameraProfileSO Profile;
    }

    /// <summary>
    /// 屏幕闪白/闪黑效果
    /// </summary>
    [Serializable]
    public class ScreenFlashEntry : EffectEntry
    {
        public ScreenFlashType FlashType;
        public float Duration = 0.2f;
    }

    public enum FadeType { Instant, FadeIn, FadeInWithScale }
    public enum ScreenFlashType { White, Black, Red }
}
