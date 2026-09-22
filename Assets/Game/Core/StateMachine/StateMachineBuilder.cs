

/// <summary>
/// Simple builder that assumes the State tree is fully wired via constructors.
/// No reflection, no magic.
/// </summary>
namespace Game.Core.StateMachine
{
    public class StateMachineBuilder
    {
        private readonly State _root;

        public StateMachineBuilder(State root)
        {
            _root = root;
        }

        public StateMachine Build()
        {
            if (_root == null)
                throw new System.NullReferenceException("Root state cannot be null.");

            return new StateMachine(_root);
        }
    }
}