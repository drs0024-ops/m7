
using UnityEngine;

namespace Game.Core.Interfaces
{
	public interface ICheckpoint
    {
        string GetId();
        Vector3 GetSpawnPosition();
        void ResetState();
        void Activate();
    }

}
    
