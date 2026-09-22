using UnityEngine;

namespace Game.Gameplay.Player
{
    [System.Serializable]
    public class PlayerDependencies
    {
        public Rigidbody2D Rb;
        public Animator Anim;
        public KnockBack KnockBack;
        public ParticleSystem Dust;

        public Collider2D FeetColl;
        public Collider2D BodyColl;

        public PlayerMovementStats Stats;

        public int PlayerLayer;
        public int OneWayLayer;
    }
}   