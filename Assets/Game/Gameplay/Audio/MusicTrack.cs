using System.Linq;
using UnityEngine;

namespace Game.Core.Audio 
{

	[System.Serializable]
	public class MusicTrack
	{
		public AudioClip clip;
		public bool loop = true;
		[Range(0f, 1f)]
    	public float volume = 1f;
		[Tooltip("Seconds to wait before this track starts (0 = immediate)")]
		public float delay = 0f;
	}
}