namespace Game.Core.Interfaces
{
	public interface IUpgradeManager
	{
		bool IsUpgradeActive(string upgradeId);
		void RequestUpgrade(string upgradeId);
		float GetRemainingTime(string upgradeId);
		// ✅ Removed: event Action<UpgradeActivatedSignal> OnUpgradeActivated;
		// (Migrated to IPublisher<UpgradeActivated> in PlayerUpgradesManager)
	}  
}