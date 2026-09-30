Dependency Graph
Tests.EditMode ──┐
Editor ──────────┤
                 ▼
            Bootstrap
           /    |    \
          ▼     ▼     ▼
        Core  Gameplay  UI
               |        |
               ▼        ▼
             Core     Core
               |
               ▼
     MessagePipe / VContainer / UniTask / LeanTween

Circular references: NONE ✅

Layering (bottom-up): Core → Gameplay → UI → Bootstrap → Editor/Tests ✅

HandleMainMenuAsync
  ├── Publish SaveRequest("SceneRouter", true)
  │     └── OnSaveRequested fires (synchronous)
  │           ├── _isSaveComplete = false
  │           └── _saveTimeoutCts = new CTS(10s)
  ├── await WaitUntil(() => _isSaveComplete)
  │     .AttachExternalCancellation(_saveTimeoutCts.Token)
  │     ├── If SaveCompleted arrives → _isSaveComplete = true → WaitUntil returns
  │     └── If 10s elapses → OperationCanceledException → log warning → proceed
  ├── token.ThrowIfCancellationRequested()
  └── TransitionAsync / EnsureLoadedAndActivate   