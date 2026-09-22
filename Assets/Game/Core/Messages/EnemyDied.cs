using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Game.Core.Messages
{
	
	public readonly struct EnemyDied
    {
        public readonly Vector2 Position;
        public EnemyDied(Vector2 position) => Position = position;
    }

}
