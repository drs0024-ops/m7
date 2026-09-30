# Camera System — Flow Documentation

## Architecture Overview

ProjectLifetimeScope (persistent)
├── Physical Camera (Camera component)
├── CinemachineBrain (on same GO as Camera)
└── CinemachineBlenderSettings (asset, assigned to Brain)

FacilitySceneLifetimeScope (per-scene, child of Project)
├── CameraConfigSO (per-scene asset, RegisterInstance)
├── CinemachineImpulseSource (RegisterComponent)
├── SceneCameraSetup (RegisterComponent)
├── CameraFollowObject (RegisterComponent)
├── CameraTargetManager (Scoped, IInitializable, IDisposable)
├── CameraSwitcher (Scoped, IInitializable, IDisposable)
├── CameraEffectController (RegisterComponentInHierarchy)
└── CameraPanMover (RegisterComponentInHierarchy)


## Message Flow

| Message | Publisher | Subscriber | Action |
|---------|-----------|------------|--------|
| `CameraSwitchRequest` | `CameraZoneTrigger`, `SceneCameraSetup`, `CameraDebugWindow` | `CameraSwitcher` | Enable target vCam, disable others, set StandbyUpdate, publish `CameraModeActive` |
| `CameraModeActive` | `CameraSwitcher` | Any external system (analytics, cutscenes, UI) | Notify of mode change |
| `CameraTargetUpdate` | `SceneCameraSetup` | `CameraTargetManager` | Apply follow to follow-cams, boundary to all, lockedRoomTarget to LockedRoom |
| `CameraBoundsResetRequest` | `CameraDebugWindow`, gameplay code | `CameraTargetManager` | Set all confiner bounds to null |
| `CameraShakeRequest` | `CameraShakeTrigger`, gameplay code | `CameraEffectController` | Fire CinemachineImpulseSource (profile or default) |
| `CameraDampingRequest` | Gameplay code (fall/land) | `CameraEffectController` | Lerp Y-damping up/down |
| `CameraPanRequest` | Gameplay code | `CameraPanMover` | Tween composer TargetOffset |
| `CameraSystemReady` | `CameraFollowObject.Start()` | `SceneCameraSetup` | Re-fire target update + initial switch |
| `PlayerSpawned` | `PlayerSpawnerService` | `CameraFollowObject`, `SceneCameraSetup` | Set follow target, re-fire target update |
| `PlayerFacingChanged` | Player controller | `CameraFollowObject` | Y-flip the follow object |

## Data Flow (per frame)

Player moves
→ CameraFollowObject.Update() mirrors X/Y position
→ (on facing change) CameraFollowObject flips Y rotation

CinemachineBrain.LateUpdate()
→ Active vCam reads Follow (CameraFollowObject)
→ CinemachinePositionComposer applies damping + TargetOffset
→ CinemachineConfiner2D clamps to BoundingShape2D
→ Brain composites to physical Camera
→ CinemachineImpulseSource (if active) adds shake offset


## Camera Switch Sequence

CameraSwitchRequest published (mode = LockedRoom)
CameraSwitcher.HandleSwitch → SwitchTo(LockedRoom)
Re-entrancy check: _isSwitching == false? ✓
_config.FindRig(LockedRoom) → CameraRigEntry
Loop all rigs:
Active rig: enabled=true, StandbyUpdate=Always
Inactive rigs: enabled=false, StandbyUpdate=Never
_config.SetActiveComposer(rig.composer)
_modePublisher.Publish(new CameraModeActive(LockedRoom))
_isSwitching = false
BlenderSettings applies blend (Cut for LockedRoom, 0.2s ease out from LockedRoom)

## Per-Scene Setup Checklist

- [ ] 3 vCam GameObjects: `CenterFollowCAM`, `NoYFollowCAM`, `LockedRoomCAM`
- [ ] Each vCam has: `CinemachineCamera` + `CinemachinePositionComposer` + `CinemachineConfiner2D`
- [ ] `CenterFollowCAM` Follow = `CameraFollowObject`
- [ ] `NoYFollowCAM` Follow = `CameraFollowObject`
- [ ] `LockedRoomCAM` Follow = scene's room anchor Transform
- [ ] `CameraFollowObject` GO at spawn point
- [ ] Boundary `Collider2D` (trigger) enclosing the room
- [ ] `CameraZoneTrigger` GOs at switch points (with `CameraSwitchDataSO`)
- [ ] `CameraShakeTrigger` GOs where needed (with `ScreenShakeProfile`)
- [ ] Camera GO with: `CameraEffectController` + `CinemachineImpulseSource` + `CameraPanMover`
- [ ] `SceneCameraSetup` GO with all refs assigned
- [ ] `FacilitySceneLifetimeScope` GO with `_cameraConfig`, `_impulseSource`, `_sceneCameraSetup`, `_cameraFollowObject` assigned
- [ ] `CameraConfigSO` asset: 3 rows filled (mode, camera, composer, confiner, isFollowCam)
- [ ] `CinemachineBlenderSettings` asset assigned to Brain's Custom Blends

## Adding a New Camera Mode

1. Add value to `CameraMode` enum
2. Create vCam GameObject in scene (with composer + confiner)
3. Add row to scene's `CameraConfigSO`
4. Add blend entries to `CinemachineBlenderSettings`
5. Trigger: `Publish(new CameraSwitchRequest(CameraMode.NewMode))`

**Zero code changes.**

## Persistent Camera Setup (Bootstrap Scene)

| GO | Components |
|----|-----------|
| Persistent Camera | `Camera`, `CinemachineBrain`, `CameraPerfHook` (dev only) |

- Brain's **Custom Blends** → assign `CinemachineBlenderSettings` asset
- `ProjectLifetimeScope` registers both `Camera` and `CinemachineBrain`