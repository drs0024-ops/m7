Notes on the refactor: 8/31/2026. From single use by player to multi use by player and enimies

Static buffers are now instance fields. Player and N enemies each get their own TransitionSequencer with independent buffers.
_reverseBuffer (Stack) is a reusable instance field instead of allocating per transition.
StateMachine.ChangeState is now internal — only the sequencer can trigger hierarchy swaps.
LCA lives in HsmUtility, breaking the StateMachine → TransitionSequencer coupling.
StateMachineBuilder no longer has the redundant ??= line.
The closure allocations in BeginTransition remain (one per activity per phase) because IActivity is an interface and you can't avoid the delegate wrap without a different pattern. If profiling shows this matters, replace IActivity with a base class that exposes Activate/Deactivate as virtuals and iterate directly — but for typical activity counts (2-5 per state) it's negligible.
All Debug.Log calls removed from the hot path. Add them back behind #if UNITY_EDITOR if you want edit-time visibility.