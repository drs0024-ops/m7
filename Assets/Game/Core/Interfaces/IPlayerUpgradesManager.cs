/// <summary>
/// Contract for the Player Upgrades System.
/// Handles unlocking, tracking, applying upgrades, and persistence.
/// </summary>

namespace Game.Core.Interfaces
{
    public interface IPlayerUpgradesManager : ISaveable
    {
        /// <summary>
        /// Read-only list of currently unlocked upgrades.
        /// </summary>
        //IReadOnlyList<PlayerUpgrade> UnlockedUpgrades { get; }

        /// <summary>
        /// Reference to the master list of all possible upgrades.
        /// </summary>
    //UpgradeListObject UpgradeList { get; }

        /// <summary>
        /// Finds the active player and applies all currently unlocked upgrades.
        /// Call this immediately after the player is instantiated.
        /// </summary>
        void InitializePlayerUpgrades();

        /// <summary>
        /// Unlocks a specific upgrade by ID, applies it, raises UI events, and requests a save.
        /// </summary>
        /// <param name="upgradeId">The unique ID of the upgrade.</param>
        void UnlockUpgrade(string upgradeId);

        /// <summary>
        /// Retrieves a specific upgrade definition by ID from the master list.
        /// </summary>
        //PlayerUpgrade GetUpgrade(string id);

        /// <summary>
        /// Checks if a specific upgrade has been unlocked.
        /// </summary>
        bool HasUpgrade(string upgradeId);
    } 
}  