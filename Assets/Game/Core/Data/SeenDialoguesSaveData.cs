using System;
using System.Collections.Generic;
using Game.Core.Interfaces;

namespace Game.Core.Data
{
	[Serializable]
	public struct SeenDialoguesSaveData : ISaveData
	{
		public string SaveId => "SeenDialogues";
		public List<string> Ids;
	}
}