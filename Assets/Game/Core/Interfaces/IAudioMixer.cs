namespace Game.Core.Interfaces
{
    public interface IAudioMixer
    {
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SFXVolume { get; set; }
        float GameVolume { get; set; }

        void DuckGameAudio(float targetVolume, float fadeDuration);
        void RestoreGameAudio(float fadeDuration);
    }
}   