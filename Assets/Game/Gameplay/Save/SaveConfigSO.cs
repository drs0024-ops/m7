using UnityEngine;
namespace Game.Gameplay.Save
{
   [CreateAssetMenu(fileName = "SaveConfig", menuName = "Game/Configuration/Save Config")]
    public class SaveConfigSO : ScriptableObject
    {
        public string saveFileName = "save.save";
    }    
    
}
