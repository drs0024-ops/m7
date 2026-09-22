using Game.Core.Data;
using UnityEngine;

namespace Game.Core.Messages
{

	public class AchievementMessages : MonoBehaviour {

		public readonly struct NextAchievementRevealed
		{
			public readonly Achievement Achievement;
			public NextAchievementRevealed(Achievement achievement) => Achievement = achievement;
		}   
	}
}