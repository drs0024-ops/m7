using UnityEngine;



public class CutSceneMessages : MonoBehaviour {}

namespace Game.Core.Messages
{
    public readonly struct CutsceneStarted
    {
        public string Context { get; }

        public CutsceneStarted(string context)
        {
            Context = context;
        }
    }

	public readonly struct CutsceneFinished
    {
        public string Context { get; }
        public bool WasSkipped { get; }

        public CutsceneFinished(string context, bool wasSkipped)
        {
            Context = context;
            WasSkipped = wasSkipped;
        }
    }

	public readonly struct CutsceneSkipRequested
    {
        public static CutsceneSkipRequested Default => new();
    }

	public readonly struct CutsceneAudioDuck
    {
        public float TargetVolume { get; }
        public float FadeDuration { get; }

        public CutsceneAudioDuck(float targetVolume, float fadeDuration)
        {
            TargetVolume = targetVolume;
            FadeDuration = fadeDuration;
        }
    }

	public readonly struct CutsceneAudioRestore
    {
        public float FadeDuration { get; }

        public CutsceneAudioRestore(float fadeDuration)
        {
            FadeDuration = fadeDuration;
        }
    }
}
