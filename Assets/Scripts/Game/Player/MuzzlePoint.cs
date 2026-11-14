using Spine.Unity;
using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;
using ZeroWalker.VFX;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 枪口定位点。
    /// 直接读取 Spine 骨骼（默认 "F_Muzzle"）的世界坐标，保证子弹总是从枪口射出，
    /// 无论角色处于哪个动作、朝向哪边。骨骼名来自 Content/data/assets.json，
    /// 兜底偏移来自 Content/data/muzzle.json。
    /// 火光由 PlayerController.Fired 事件驱动，控制器不直接调用这里的表现方法。
    /// </summary>
    [DisallowMultipleComponent]
    public class MuzzlePoint : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("枪口火光")]
        [SerializeField] private SpriteFlash flash;

        private Spine.Bone _bone;
        private PlayerController _controller;

        /// <summary>枪口可调数值（来自 Content/data/muzzle.json）。</summary>
        private MuzzleTuning Tuning
        {
            get { return ContentLoader.Current.muzzle; }
        }

        /// <summary>枪口骨骼名（来自 Content/data/assets.json）。</summary>
        private static string MuzzleBoneName
        {
            get { return ContentLoader.Current.assets.spine.muzzleBone; }
        }

        /// <summary>枪口的世界坐标。</summary>
        public Vector3 WorldPosition
        {
            get
            {
                if (skeletonAnimation == null) return transform.position;

                if (_bone == null && skeletonAnimation.Skeleton != null)
                    _bone = skeletonAnimation.Skeleton.FindBone(MuzzleBoneName);

                if (_bone != null)
                    return skeletonAnimation.transform.TransformPoint(new Vector3(_bone.WorldX, _bone.WorldY, 0f));

                return skeletonAnimation.transform.TransformPoint(Tuning.fallbackOffset.ToVector3());
            }
        }

        private void Awake()
        {
            if (skeletonAnimation == null)
                skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        }

        private void OnEnable()
        {
            _controller = GetComponentInParent<PlayerController>();
            if (_controller != null) _controller.Fired += HandleFired;
        }

        private void OnDisable()
        {
            if (_controller != null) _controller.Fired -= HandleFired;
            _controller = null;
        }

        /// <summary>有人开枪了：在枪口闪一次火光（颜色随子弹走）。</summary>
        private void HandleFired(int facing, Color tint)
        {
            PlayFlash(facing, tint);
        }

        /// <summary>在枪口播放一次火光（使用火光自身的颜色）。</summary>
        public void PlayFlash(int facing)
        {
            PlayFlash(facing, Color.white);
        }

        /// <summary>在枪口播放一次火光，并乘上指定着色（特殊子弹使用自己的颜色）。</summary>
        public void PlayFlash(int facing, Color tint)
        {
            if (flash == null) return;

            flash.transform.position = WorldPosition;
            flash.transform.localScale = Vector3.one * Mathf.Abs(flash.transform.localScale.x);
            flash.Play(tint);
        }

        private void LateUpdate()
        {
            // 火光存活期间跟随枪口，避免角色移动时火光掉队
            if (flash != null && flash.IsPlaying)
                flash.transform.position = WorldPosition;
        }
    }
}
