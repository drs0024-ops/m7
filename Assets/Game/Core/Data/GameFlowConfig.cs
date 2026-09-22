using Game.Core.Enums;
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "GameFlowConfig", menuName = "Game/Game Flow Config")]
    public class GameFlowConfig : ScriptableObject
    {
        [Header("Initial")]
        public GameState initialState = GameState.MainMenu;
    }
}   