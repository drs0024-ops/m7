
using System;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
	[Serializable]
    public class OrbCounterSaveData : ISaveData
    {
        public string SaveId = "OrbCounter";
        public int Total;
        public int HumanoidCount;
        public int NonHumanoidCount;

        string ISaveData.SaveId => SaveId;
    }

}


	