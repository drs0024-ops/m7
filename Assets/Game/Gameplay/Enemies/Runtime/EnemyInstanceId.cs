using System;

namespace Game.Gameplay.Enemies.Runtime
{
    public readonly struct EnemyInstanceId : IEquatable<EnemyInstanceId>
    {
        public int Value { get; }

        public EnemyInstanceId(int value) => Value = value;

        public bool Equals(EnemyInstanceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EnemyInstanceId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public static bool operator ==(EnemyInstanceId left, EnemyInstanceId right) => left.Equals(right);
        public static bool operator !=(EnemyInstanceId left, EnemyInstanceId right) => !left.Equals(right);
    }
}