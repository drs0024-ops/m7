using System.Linq;
using Game.Core.Messages;
using MessagePipe;
using UnityEngine;

namespace Game.Gameplay
{
	// Game/Gameplay/.../ClimaxTrigger.cs
	public class ClimaxTrigger : MonoBehaviour
	{
		private void OnTriggerEnter(Collider other)
		{
			if (other.CompareTag("Player"))
				GlobalMessagePipe.GetPublisher<ClimaxTriggered>().Publish(default);
		}
	}  

}

	 
