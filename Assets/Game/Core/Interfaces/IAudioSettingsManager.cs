namespace Game.Core.Interfaces
{
    public interface IAudioSettingsManager : ISaveable
    {
        /// <summary>
        /// Current Master Volume value (0.0 to 1.0).
        /// </summary>
        float MasterVolume { get; }

        /// <summary>
        /// Current Music Volume value (0.0 to 1.0).
        /// </summary>
        float MusicVolume { get; }

        /// <summary>
        /// Current SFX Volume value (0.0 to 1.0).
        /// </summary>
        float SfxVolume { get; }

        /// <summary>
        /// Sets the Master Volume and applies it to the AudioMixer.
        /// </summary>
        void SetMasterVolume(float value);

        /// <summary>
        /// Sets the Music Volume and applies it to the AudioMixer.
        /// </summary>
        void SetMusicVolume(float value);

        /// <summary>
        /// Sets the SFX Volume and applies it to the AudioMixer.
        /// </summary>
        void SetSfxVolume(float value);
    }   

}