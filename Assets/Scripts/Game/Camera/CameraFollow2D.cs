using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.CameraSystem
{
    /// <summary>
    /// 简易 2D 横板摄像机跟随，使用 SmoothDamp 平滑移动，可选锁定 Y 轴。
    /// 偏移/平滑时间/是否锁 Y 来自 Content/data/camera.json。
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private Vector3 _velocity;

        public Transform Target
        {
            get { return target; }
            set { target = value; }
        }

        /// <summary>相机可调数值（来自 Content/data/camera.json）。</summary>
        private CameraTuning Tuning
        {
            get { return ContentLoader.Current.camera; }
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindObjectOfType<Player.PlayerController>();
                if (player != null) target = player.transform;
            }

            // 开局直接对齐，避免从原点滑过去
            if (target != null)
                transform.position = GetGoalPosition();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                GetGoalPosition(),
                ref _velocity,
                Tuning.smoothTime);
        }

        private Vector3 GetGoalPosition()
        {
            var goal = target.position + Tuning.offset.ToVector3();
            if (Tuning.lockY) goal.y = transform.position.y;
            return goal;
        }
    }
}
