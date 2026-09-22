Camera System Documentation (Unity 2026 + VContainer + Cinemachine 3)
Table of Contents
System Overview
Architecture Diagram
Signal Definitions (C# Classes)
Core Components
Implementation Guide
Usage Examples
🌐 System Overview
This camera system adheres to the Single Responsibility Principle (SRP) and uses a Publish-Subscribe pattern via a typed SignalBus managed by VContainer. This ensures:

Zero Coupling: Game logic (Player, Triggers) never references Camera scripts; they only know the SignalBus.
High Performance: Uses LeanTween for interpolation and stored delegates in SignalBus to prevent GC allocations and memory leaks.
Modularity: Each feature (Shake, Bounds, Switching) is an isolated module injected into the CameraSystem.
Type Safety: Replaces fragile string-based events with strongly-typed C# request classes.
Cinemachine 3 Native: Fully compatible with Unity.Cinemachine namespace, utilizing CinemachineCamera, PositionComposer, and Confiner2D.
🏗️ Architecture Diagram
graph TD
    subgraph Game_Logic [Game Logic (Broadcasters)]
        Player[Player Controller]
        Trigger[Zone Trigger]
        Explosion[Explosion Script]
        SceneLoader[Scene Loader]
    end

    subgraph SignalBus [VContainer SignalBus (Singleton)]
        Bus[SignalBus Instance]
    end

    subgraph Camera_System [Camera System (Listeners)]
        CamSys[CameraSystem.cs<br/>Orchestrator]
        Switcher[CameraSwitcher.cs<br/>State Manager]
        Effects[CameraEffectController.cs<br/>Shake & Damping]
        Mover[CameraMover.cs<br/>Panning]
        TargetMgr[CameraTargetManager.cs<br/>Bounds & Follow]
    end

    subgraph Unity_Engine [Unity Engine]
        CM_Center[CinemachineCamera: Center]
        CM_NoY[CinemachineCamera: NoY]
        CM_Locked[CinemachineCamera: Locked]
        Impulse[CinemachineImpulseSource]
        Collider[Boundary Collider2D]
        Follow[Player Transform]
    end

    Player --Fires--> Bus
    Trigger --Fires--> Bus
    Explosion --Fires--> Bus
    SceneLoader --Fires--> Bus

    Bus --Subscribes--> CamSys
    CamSys --Delegates--> Switcher
    CamSys --Delegates--> Effects
    CamSys --Delegates--> Mover
    CamSys --Delegates--> TargetMgr

    Switcher --Activates--> CM_Center
    Switcher --Activates--> CM_NoY
    Switcher --Activates--> CM_Locked
    
    Effects --Triggers--> Impulse
    TargetMgr --Sets Follow--> Follow
    TargetMgr --Assigns Bounds--> Collider

📡 Signal Definitions (C# Classes)
Instead of ScriptableObject assets, we use lightweight C# classes. These are not MonoBehaviours and incur zero memory overhead when fired.

Signal Name	Payload	Purpose
CameraSwitchRequest	CameraMode Mode, Vector2 ExitDirection	Request to switch active camera (e.g., CenterFollow → LockedRoom).
CameraPanRequest	Vector2 TargetOffset, float Duration	Request to pan camera to a specific offset (e.g., look up/down).
CameraShakeRequest	float ForceMultiplier, ScreenShakeProfile Profile	Request to trigger screen shake with optional profile overrides.
CameraDampingRequest	bool IsFalling	Request to interpolate Y-damping (e.g., for falling states).
CameraTargetUpdateRequest	Transform FollowTarget, Collider2D Boundary	Scene Load: Updates persistent cameras with new scene targets and bounds.
CameraBoundsResetRequest	(Empty)	Force refresh of boundary colliders (rarely needed, handled by TargetUpdate).

🧩 Core Components
1. CameraSystem (Orchestrator)
Role: The single entry point for all signals. It subscribes to the SignalBus in Initialize() and delegates logic to specific modules.
Lifecycle: IInitializable, IDisposable (Scoped or Singleton).
Key Logic:
public void Initialize() {
    _signalBus.Subscribe<CameraSwitchRequest>(req => _switcher.SwitchTo(req.Mode));
    _signalBus.Subscribe<CameraTargetUpdateRequest>(req => _targetMgr.UpdateTargets(req.FollowTarget, req.Boundary));
    // ...
}

2. CameraSwitcher (State Manager)
Role: Manages the active state of the 3 persistent cameras.
Mechanism: Enables/Disables CinemachineCamera components based on CameraMode enum. Cinemachine Brain handles the blending automatically.
Safety: Checks for null cameras before switching. Uses CameraConfigSO for references.
3. CameraTargetManager (Bounds & Follow)
Role: The single source of truth for updating camera targets and confinement boundaries.
Critical Fix: Calls confiner.InvalidateBoundingShapeCache() after assigning new bounds to prevent glitches in Cinemachine 3.
Usage: Called by SceneCameraInitializer on scene load.
4. CameraEffectController (Shake & Damping)
Role: Handles screen shake via CinemachineImpulseSource and dynamic damping changes.
Optimization: Caches CinemachineImpulseSource via injection (no FindObjectOfType at runtime).
Shake Logic: Correctly re-assigns the ImpulseDefinition struct in Cinemachine 3 before firing.
5. CameraMover (Panning)
Role: Tweens the TargetOffset of the active CinemachinePositionComposer.
Mechanism: Uses LeanTween.value to interpolate offsets smoothly.
Safety: Cancels existing tweens before starting new ones to prevent conflict.
6. SceneCameraInitializer (Entry Point)
Role: Implements IStartable. Fires the initial CameraTargetUpdateRequest when a scene loads to sync the persistent camera system with the new scene's data (SceneConfigSO).
🛠️ Implementation Guide
1. Project Setup
Packages: Ensure VContainer, Unity.Cinemachine (v3+), and LeanTween are installed.
Namespaces: Use using Unity.Cinemachine; (not Cinemachine).
2. ScriptableObjects Creation
Create the following assets in your project:

CameraConfigSO: Holds references to the 3 persistent CinemachineCamera objects.
CameraSwitchDataSO: Holds CameraMode for Left/Right triggers.
ScreenShakeProfile: Defines shake curves, duration, and gains.
SceneConfigSO: Holds scene-specific Transform (Player start) and Collider2D (Boundary).
3. VContainer Registration (ProjectLifetimeScope)
Register the system as Singletons to persist across scene loads.

protected override void Configure(IContainerBuilder builder)
{
    // 1. Populate Camera Config
    var cameraConfig = ScriptableObject.CreateInstance<CameraConfigSO>();
    cameraConfig.Center = _centerFollowCamera;
    cameraConfig.NoY = _noYFollowCamera;
    cameraConfig.Locked = _lockedRoomCamera;
    builder.RegisterInstance(cameraConfig);

    // 2. Register Services
    builder.Register<CameraTargetManager>(Lifetime.Singleton);
    builder.Register<CameraSwitcher>(Lifetime.Singleton);
    builder.Register<CameraSystem>(Lifetime.Singleton);
    builder.Register<CameraEffectController>(Lifetime.Singleton);
    builder.Register<CameraMover>(Lifetime.Singleton);

    // 3. Register Entry Point
    builder.RegisterEntryPoint<SceneCameraInitializer>();
    
    // 4. Register SignalBus
    builder.Register<SignalBus>(Lifetime.Singleton).AsImplementedInterfaces();
}

4. Scene Setup
Persistent Scene: Contains the ProjectLifetimeScope, the 3 CinemachineCamera objects, CinemachineBrain, CinemachineListener, and CinemachineImpulseSource.
Loadable Scenes: Contain a SceneConfig component (or SO reference) that defines the BoundaryCollider and PlayerStart transform.
📝 Usage Examples
1. Switching Cameras (Zone Trigger)
Fire a signal when the player enters a trigger zone.

public class CameraZoneTrigger : MonoBehaviour
{
    [Inject] private ISignalBus _signalBus;
    [SerializeField] private CameraSwitchDataSO _data;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var direction = other.attachedRigidbody.velocity.x > 0 ? 1 : -1;
            var mode = direction > 0 ? _data.cameraOnRight : _data.cameraOnLeft;
            
            _signalBus.Fire(new CameraSwitchRequest { Mode = mode });
        }
    }
}

2. Triggering Screen Shake (Explosion)
Fire a signal with a profile reference.

public class Explosion : MonoBehaviour
{
    [Inject] private ISignalBus _signalBus;
    [SerializeField] private ScreenShakeProfile _shakeProfile;

    public void Detonate()
    {
        // Logic for damage/effects...
        
        _signalBus.Fire(new CameraShakeRequest 
        { 
            Profile = _shakeProfile, 
            ForceMultiplier = 1.5f 
        });
    }
}

3. Updating Bounds (Scene Load)
Handled automatically by SceneCameraInitializer, but can be called manually:

// Inside a Scene Loader or Manager
_signalBus.Fire(new CameraTargetUpdateRequest 
{ 
    FollowTarget = playerTransform, 
    Boundary = roomBoundaryCollider 
});

4. Panning Camera (Cutscene/Event)
_signalBus.Fire(new CameraPanRequest 
{ 
    TargetOffset = new Vector2(0, 5), // Look up 5 units
    Duration = 1.0f 
});

✅ Checklist for Deployment
[ ] Generic Signals: Ensure all Subscribe calls use <T> (e.g., Subscribe<CameraSwitchRequest>).
[ ] Cinemachine 3 API: Verify InvalidateBoundingShapeCache() is used, not InvalidatePathCache().
[ ] Struct Assignment: Ensure ImpulseDefinition is assigned back to the source in shake logic.
[ ] Null Safety: All managers check for null Composer or Camera before tweening/switching.
[ ] Disposal: All IDisposable classes unsubscribe from SignalBus to prevent memory leaks.