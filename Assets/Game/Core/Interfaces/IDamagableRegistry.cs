namespace Game.Core.Interfaces
{
    public interface IDamagableRegistry
    {
        void Register(int entityId, IDamagable target);
        bool Unregister(int entityId);
        bool TryGet(int entityId, out IDamagable target);
    }
}