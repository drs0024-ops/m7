CameraFlowTests.md
# Camera System — Test Documentation

## Test Files

| File | Class | Tests | Covers |
|------|-------|:-----:|--------|
| `CameraConfigSOTests.cs` | `CameraConfigSOTests` | 7 | Data-driven rig lookup, filtering, ActiveComposer |
| `CameraSwitcherTests.cs` | `CameraSwitcherTests` | 4 | Re-entrancy, dispose guard, unknown mode, init |
| `CameraTargetManagerTests.cs` | `CameraTargetManagerTests` | 6 | Subscriptions, null-safety, dispose guard |

**Total: 17 camera tests** (38 project-wide including flow/save tests)

---

## CameraConfigSOTests

| Test | Verifies |
|------|----------|
| `GetRig_ReturnsCorrectEntry_ByMode` | `GetRig(CameraMode.NoYFollow)` returns the matching entry with correct `mode` and `isFollowCam` |
| `GetRig_ReturnsDefault_ForUnknownMode` | `GetRig((CameraMode)999)` returns a default struct (all refs null, `isFollowCam` false) |
| `FindRig_ReturnsNull_ForUnknownMode` | `FindRig((CameraMode)999)` returns `null` (nullable struct) |
| `FindRig_ReturnsEntry_ForKnownMode` | `FindRig(CameraMode.LockedRoom)` returns non-null with correct `mode` and `isFollowCam == false` |
| `GetFollowRigs_ReturnsOnlyFollowCams` | Filters correctly: returns 2 entries, both `isFollowCam == true` |
| `GetNonFollowRigs_ReturnsOnlyNonFollowCams` | Filters correctly: returns 1 entry, `isFollowCam == false`, mode is `LockedRoom` |
| `SetActiveComposer_UpdatesProperty` | Getter/setter round-trip (null in, null out) |

### Setup Pattern
- Creates `CameraConfigSO` via `ScriptableObject.CreateInstance`
- Populates 3 rig entries (CenterFollow, NoYFollow, LockedRoom)
- Tears down with `DestroyImmediate`

---

## CameraSwitcherTests

| Test | Verifies |
|------|----------|
| `SwitchTo_UnknownMode_DoesNotThrow` | Calling `SwitchTo(default)` with no matching rig is a safe no-op |
| `SwitchTo_AfterDispose_DoesNotThrow` | `_isDisposed` guard prevents post-dispose crashes |
| `Dispose_CalledTwice_DoesNotThrow` | Double-dispose is idempotent |
| `Initialize_SubscribesAndSwitches` | `Initialize()` doesn't throw (subscribes + calls `SwitchTo(CenterFollow)`) |

### Setup Pattern
- Creates `CameraConfigSO` with 1 rig (CenterFollow, all refs null)
- Injects no-op `ISubscriber<CameraSwitchRequest>` + `IPublisher<CameraModeActive>` via constructor
- No `BuiltinContainerBuilder` (project convention)

---

## CameraTargetManagerTests

| Test | Verifies |
|------|----------|
| `Initialize_SubscribesWithoutError` | Subscription setup doesn't throw |
| `UpdateTargets_NullFollow_DoesNotThrow` | Null follow + null boundary is a safe no-op |
| `UpdateTargets_WithLockedRoomTarget_DoesNotThrow` | Null-safe even with lockedRoomTarget param |
| `ResetBounds_DoesNotThrow` | Reset with all-null confiners is safe |
| `Dispose_CalledTwice_DoesNotThrow` | Double-dispose is idempotent |

### Setup Pattern
- Creates `CameraConfigSO` with 2 rigs (CenterFollow follow, LockedRoom non-follow)
- Injects no-op `ISubscriber<CameraTargetUpdate>` + `ISubscriber<CameraBoundsResetRequest>`

---

## What's NOT Tested (EditMode limitation)

| Behavior | Why | How to test |
|----------|-----|-------------|
| Actual vCam enable/disable | Requires live `CinemachineCamera` (MonoBehaviour) | PlayMode test or manual |
| `StandbyUpdate` values set correctly | Requires live vCam | PlayMode test |
| Blend application | Requires Brain + BlenderSettings in scene | Manual (Debug Window) |
| `CinemachineImpulseSource` firing | Requires live impulse + Brain | Manual |
| `CameraFollowObject` position mirror | Requires live player transform | PlayMode test |
| `CameraZoneTrigger` direction logic | Requires `Collider2D` + Rigidbody | PlayMode test |

### Recommended PlayMode Tests (future)

```csharp
// In a Tests.PlayMode assembly:
[Test]
public void SwitchTo_LockedRoom_DisablesFollowCams()
{
    // Load scene, resolve CameraSwitcher from scope
    // Call SwitchTo(LockedRoom)
    // Assert: CenterFollowCAM.enabled == false
    // Assert: NoYFollowCAM.enabled == false
    // Assert: LockedRoomCAM.enabled == true
}

[Test]
public void UpdateTargets_DoesNotOverwrite_LockedRoom_Follow()
{
    // Publish CameraTargetUpdate with player transform as follow
    // Assert: LockedRoomCAM.Follow != playerTransform
    // Assert: LockedRoomCAM.Follow == lockedRoomAnchor
}

No-Op Stub Pattern (project convention)
All camera tests use reflection-free constructor injection with no-op stubs:

private sealed class NoOpSubscriber<T> : ISubscriber<T> where T : struct
{
    public IDisposable Subscribe(Action<T> action) => new NoOpDisposable();
    public IDisposable Subscribe(Action<T, IObserver<T>> action) => new NoOpDisposable();
    public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters) => new NoOpDisposable();

    private sealed class NoOpDisposable : IDisposable { public void Dispose() { } }
}

private sealed class NoOpPublisher<T> : IPublisher<T> where T : struct
{
    public void Publish(T message) { }
    public void Publish(T message, Action onError) { }
    public void Publish(T message, Action<T> onError, Action onCompleted) { }
}

Why not BuiltinContainerBuilder: Project convention — keeps tests fast, isolated, and free of VContainer's bootstrap overhead.


---

Both files are ready to drop into your repo. Suggested locations:

| File | Path |
|------|------|
| `CameraFlow.md` | `Assets/Docs/CameraFlow.md` (or `Docs/` at project root) |
| `CameraFlowTests.md` | `Assets/Docs/CameraFlowTests.md` |