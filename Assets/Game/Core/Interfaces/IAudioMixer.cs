namespace Game.Core.Interfaces
{
    public interface IAudioMixer
    {
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SFXVolume { get; set; }
    }
}