namespace Game.Core.Interfaces
{
	public interface IUpgradeStateManager
	{
		bool HasUpgrade(string id);
		bool IsEffectActive(string id);
		float GetRemainingTime(string id);
	}
}
