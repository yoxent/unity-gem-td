using LitMotion;
using UnityEngine;

namespace GemTD.Gameplay.Enemies
{
    /// <summary>Pooled sphere view bound to an <see cref="EnemyRuntime"/>.</summary>
    public sealed class EnemyView : MonoBehaviour
    {
        const float GroundLift = 0.4f;

        MotionHandle _hopHandle;
        float _hopY;

        public EnemyRuntime Runtime { get; private set; }

        public void Bind(EnemyRuntime runtime, IMotionScheduler hopScheduler)
        {
            StopHop();
            Runtime = runtime;
            _hopY = 0f;
            TryStartHop(hopScheduler);
            SyncTransform();
        }

        public void ApplyHopPlaybackSpeed()
        {
            if (!_hopHandle.IsActive())
                return;

            var m = Runtime != null ? Runtime.MoveSpeedMultiplier : 0f;
            _hopHandle.PlaybackSpeed = m < 0f ? 0f : m;
        }

        public void SyncTransform()
        {
            if (Runtime == null)
                return;
            transform.position = Runtime.WorldPosition + Vector3.up * (GroundLift + _hopY);
        }

        public void Clear()
        {
            StopHop();
            Runtime = null;
        }

        void OnDestroy()
        {
            StopHop();
        }

        void TryStartHop(IMotionScheduler hopScheduler)
        {
            if (hopScheduler == null || Runtime == null)
                return;

            if (Runtime.LocomotionStyle != LocomotionStyle.Hop || Runtime.HopHeight <= 0f || Runtime.HopPeriod <= 0f)
                return;

            _hopHandle = LMotion.Create(0f, Runtime.HopHeight, Runtime.HopPeriod * 0.5f)
                .WithEase(Ease.OutQuad)
                .WithLoops(-1, LoopType.Yoyo)
                .WithScheduler(hopScheduler)
                .Bind(this, static (x, view) => view._hopY = x);
        }

        void StopHop()
        {
            if (_hopHandle.IsActive())
                _hopHandle.Cancel();
            _hopY = 0f;
        }
    }
}
