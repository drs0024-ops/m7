using UnityEngine;

public class CurrencyMessages
{
	
}
namespace Game.Core.Messages
{

	
		public readonly struct CurrencyCollected
		{
			public readonly int Amount;
			public readonly UnityEngine.Transform Collector;
			public readonly AudioClip Clip;

			public CurrencyCollected(int amount, UnityEngine.Transform collector, AudioClip clip)
			{
				Amount = amount;
				Collector = collector;
				Clip = clip;
			}
		}   
	
	public readonly struct CurrencyChanged
    {
        public readonly int Amount;
        public CurrencyChanged(int amount) => Amount = amount;
    }
}
