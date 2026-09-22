using Game.Core.Enums;
using Game.UI;
using UnityEngine;
namespace Game.Core.Messages
{
	public class CRTMessages : MonoBehaviour {}
	 public readonly struct CRTWorldZoomChanged
    {
        public readonly float Zoom;
        public CRTWorldZoomChanged(float zoom) => Zoom = zoom;
    }

    public readonly struct CRTWorldOpacityChanged
    {
        public readonly float Opacity;
        public CRTWorldOpacityChanged(float opacity) => Opacity = opacity;
    }

    public readonly struct CRTRollBarIntensityChanged
    {
        public readonly float Intensity;
        public CRTRollBarIntensityChanged(float intensity) => Intensity = intensity;
    }





	public readonly struct CRTSettingsChanged
    {
        public readonly CRTSettings Settings;
        public CRTSettingsChanged(CRTSettings settings) => Settings = settings;
    }

    /// <summary>
    /// Request to change the CRT overlay state.
    /// Hidden disables the overlay. Other states enable it and apply the matching preset.
    /// Transition also runs the wipe animation, then reverts to MenuIdle.
    /// Where to publish CRTStateRequested from gameplay
    /// Moment	Where	Publish
    /// Player takes damage	Your health/injury system	new CRTStateRequested(CRTState.Injured)
    /// Player dies	GameFlowSystem.OnPlayerDied	new CRTStateRequested(CRTState.Dead)
    /// Respawn / return to menu	GameFlowSystem	new CRTStateRequested(CRTState.Hidden) or MenuIdle
    /// </summary>
    public readonly struct CRTStateRequested
    {
        public CRTState State { get; }
        public CRTStateRequested(CRTState state) => State = state;
    }


  
}

