using Game.Core.Data;

public class UIMessages {}

namespace Game.Core.Messages
{
    #region Overlay

    

    #endregion

    #region Notifications

   // public readonly struct NotificationRequested
    //{
   //    public NotificationData Data { get; }
   //     public NotificationRequested(NotificationData data) => Data = data;
   // }

    public readonly struct AchievementUnlocked
    {
        public readonly Achievement Data;
        public AchievementUnlocked(Achievement data) => Data = data;
    }


    public readonly struct UpgradePurchased
    {
        //public PlayerUpgrade UpgradeName { get; }
        //public UpgradePurchased(string upgradeName) => UpgradeName = upgradeName;
    }

    public readonly struct ObjectiveCompleted
    {
        public string ObjectiveName { get; }
        public ObjectiveCompleted(string objectiveName) => ObjectiveName = objectiveName;
    }



    public readonly struct NextAchievementRevealed
    {
        public readonly Achievement Achievement;
        public NextAchievementRevealed(Achievement achievement) => Achievement = achievement;
    }

    #endregion

    #region Video

    public readonly struct VideoFinished
    {
        public static VideoFinished Default => new();
    }

    #endregion

    #region Options
    
    /// <summary>
    /// Request to open the Options panel. Published by MainMenu and PauseMenu.
    /// </summary>
    public readonly struct OptionsOpenRequested { }

    /// <summary>
    /// Request to close the Options panel. Published by Escape (at category level).
    /// </summary>
    public readonly struct OptionsCloseRequested { }

    /// <summary>
    /// Notification that the Options panel visibility changed.
    /// Published by OptionsMenuController after show/hide completes.
    /// </summary>
    public readonly struct OptionsPanelStateChanged
    {
        public bool IsOpen { get; }
        public OptionsPanelStateChanged(bool isOpen) => IsOpen = isOpen;
    }
    
/* Depricated

public readonly struct CloseOptionsMenuRequested
    {
        public static CloseOptionsMenuRequested Default => new();
    }

    public readonly struct OptionsClosed
    {
        public string PreviousSceneName { get; }
        public OptionsClosed(string previousSceneName) => PreviousSceneName = previousSceneName;
    }
    
    public readonly struct OptionsOpened
    {
        public OptionsSource Source { get; }
        public OptionsOpened(OptionsSource source) => Source = source;
    }

    public readonly struct OptionsBackRequested
    {
        public static OptionsBackRequested Default => new();
    }
*/
    #endregion

    public readonly struct OpenPauseMenuRequested
    {
        public static OpenPauseMenuRequested Default => new();
    }

    public readonly struct ClosePauseMenuRequested
    {
        public static ClosePauseMenuRequested Default => new();
    }


    public readonly struct ShowMainMenuRequested { }

    /// <summary>
    /// Request to navigate to the main menu scene.
    /// The target is always SceneRegistry.MainMenuScene (single source of truth).
    /// SourceSceneName is the scene to unload after navigation, or null if none.
    /// </summary>
    public readonly struct NavigateToMenu
    {
        public string SourceSceneName { get; }

        public NavigateToMenu(string sourceSceneName = null) => SourceSceneName = sourceSceneName;
    }

    /// <summary>
    /// Published each frame during scene load to drive the loading progress bar.
    /// </summary>
    public readonly struct SceneLoadProgress
    {
        public float Progress { get; }
        public SceneLoadProgress(float progress) => Progress = progress;
    }
   
}