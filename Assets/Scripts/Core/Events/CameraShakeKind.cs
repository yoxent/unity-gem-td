namespace GemTD.Core
{
    /// <summary>
    /// Burst is an instant spike that decays. Duration holds then eases out.
    /// Continuous holds until <see cref="GameEvents.RaiseCameraShakeStop"/> /
    /// <see cref="GameEvents.RaiseCameraShakeStopContinuous"/>.
    /// </summary>
    public enum CameraShakeKind
    {
        Burst = 0,
        Duration = 1,
        Continuous = 2
    }
}
