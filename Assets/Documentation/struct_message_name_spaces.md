# Current `Assets/Game` Structure

This tree reflects the current source layout. Assembly comments describe the dependency direction configured for the project.

```text
Assets/Game/
|
|-- Bootstrap/ (can see Core, Gameplay, and UI)
|   |-- BootStrap.asmdef
|   |-- Bootstrap.cs
|   |-- EventSystemManager.cs
|   |-- GameFlowSystem.cs
|   |-- GameManagerService.cs
|   |-- GameStateInputHandler.cs
|   |-- GameStateMachine.cs
|   |-- GameStateSceneRouter.cs
|   |-- LevelLoader.cs
|   |-- LevelProgressionManager.cs
|   |-- OverlayManager.cs
|   |-- SceneCleanupService.cs
|   |-- SceneFadeManager.cs
|   |-- SceneLoaderService.cs
|   |-- SceneRegistry.cs
|   |-- SceneTransitionDirector.cs
|   |-- SceneTransitionOrchestrator.cs
|   `-- Scopes/
|       |-- FacilitySceneLifetimeScope.cs
|       |-- IntroVideoSceneLifetimeScope.cs
|       |-- LevelLifetimeScope.cs
|       |-- OptionsMenuSceneLifeScope.cs
|       |-- ProjectLifetimeScope.cs
|       `-- SceneLifetimeScopeIntroVideo.cs
|
|-- Core/ (shared contracts and infrastructure; no game feature references)
|   |-- Core.asmdef
|   |-- Data/
|   |   |-- Achievement.cs
|   |   |-- AchievementSaveData.cs
|   |   |-- AudioSaveData.cs
|   |   |-- CheckPointSaveData.cs
|   |   |-- DialogueText.cs
|   |   |-- LevelProgressionSaveData.cs
|   |   |-- NotificationData.cs
|   |   |-- Objective.cs
|   |   |-- PlayerUpgradesSaveData.cs
|   |   `-- VideoSaveData.cs
|   |-- Enums/
|   |   |-- ActivityMode.cs
|   |   |-- CameraMode.cs
|   |   |-- DoorToSpawnAt.cs
|   |   |-- GameState.cs
|   |   |-- NotificationType.cs
|   |   |-- OptionsSource.cs
|   |   |-- PlayerState.cs
|   |   |-- SoundType.cs
|   |   `-- SpawnKeys.cs
|   |-- Interfaces/
|   |   |-- IAchievementManager.cs
|   |   |-- IAudioManager.cs
|   |   |-- IAudioMixer.cs
|   |   |-- IAudioSettingsManager.cs
|   |   |-- ICheckpoint.cs
|   |   |-- ICheckpointManager.cs
|   |   |-- IDamagable.cs
|   |   |-- IDialogueController.cs
|   |   |-- IDirector.cs
|   |   |-- IEnemyMoveable.cs
|   |   |-- IGameEventListener.cs
|   |   |-- IGameManager.cs
|   |   |-- IGameStateInputHandler.cs
|   |   |-- IGameStateMachine.cs
|   |   |-- IGameStateSceneRouter.cs
|   |   |-- IInteractable.cs
|   |   |-- ILevelProgressionManager.cs
|   |   |-- IOptionsMenuContextManager.cs
|   |   |-- IPlayerSpawnerManager.cs
|   |   |-- IPlayerStateDriver.cs
|   |   |-- IPlayerUpgradesManager.cs
|   |   |-- ISaveData.cs
|   |   |-- ISaveManager.cs
|   |   |-- ISaveable.cs
|   |   |-- ITalkable.cs
|   |   |-- ITriggerCheckable.cs
|   |   |-- IUpgradeManager.cs
|   |   |-- IUpgradeStateManager.cs
|   |   `-- Killable.cs
|   |-- Messages/
|   |   |-- AchievementMessages.cs
|   |   |-- AudioSignals.cs
|   |   |-- CameraSignals.cs
|   |   |-- CombatMessages.cs
|   |   |-- CoreMessages.cs
|   |   |-- CurrencyMessages.cs
|   |   |-- EnemyDied.cs
|   |   |-- GameStateChanged.cs
|   |   |-- HUDMessages.cs
|   |   |-- PlayerSignals.cs
|   |   |-- SaveSignals.cs
|   |   |-- SceneSignals.cs
|   |   |-- TimeScalePause.cs
|   |   |-- TimeScaleResume.cs
|   |   |-- UISignals.cs
|   |   `-- UpgradeSignals.cs
|   |-- StateMachine/
|   |   |-- HsmUtility.cs
|   |   |-- IActivity.cs
|   |   |-- ISequence.cs
|   |   |-- State.cs
|   |   |-- StateMachine.cs
|   |   |-- StateMachineBuilder.cs
|   |   `-- TransitionSequencer.cs
|   |-- SceneField.cs
|   `-- UserSystem.cs
|
|-- Gameplay/ (can see Core)
|   |-- Gameplay.asmdef
|   |-- Achievements/
|   |   |-- AchievementConfigSO.cs
|   |   |-- AchievementList.cs
|   |   |-- AchievementManager.cs
|   |   `-- ObjectiveTracker.cs
|   |-- Aduio/
|   |   |-- AudioConfigSO.cs
|   |   |-- AudioListenerManager.cs
|   |   |-- AudioManager.cs
|   |   |-- AudioMixerMaster.cs
|   |   |-- AudioView.cs
|   |   `-- MusicTrigger.cs
|   |-- Camera/
|   |   |-- CameraBoundsManager.cs
|   |   |-- CameraConfigSO.cs
|   |   |-- CameraControlTrigger.cs
|   |   |-- CameraEffectController.cs
|   |   |-- CameraFollowObject.cs
|   |   |-- CameraPanMover.cs
|   |   |-- CameraSettingsSO.cs
|   |   |-- CameraShakeRequest.cs
|   |   |-- CameraShakeTrigger.cs
|   |   |-- CameraSwitchDataSO.cs
|   |   |-- CameraSwitcher.cs
|   |   |-- CameraSystem.cs
|   |   |-- CameraTargetManager.cs
|   |   |-- CameraZoneTrigger.cs
|   |   |-- DebugLog.cs
|   |   |-- ParallaxOrchestrator.cs
|   |   |-- SceneCameraSetup.cs
|   |   |-- SceneViewDriverDisabler.cs
|   |   `-- ScreenShakeProfile.cs
|   |-- Enemies/
|   |   |-- Attack/, Chase/, Enemy Types/, EnemySOs/, Idle/, StateMachine/, Trigger Checks/
|   |   |-- Enemy.cs
|   |   |-- EnemyCollisionManager.cs
|   |   |-- EnemyHealth.cs
|   |   |-- EnemyModel.cs
|   |   |-- EnemyPrefabBase.cs
|   |   `-- HeadDamageCheck.cs
|   |-- Level/
|   |   |-- LevelConfigSO.cs
|   |   |-- LevelLoader.cs
|   |   `-- LevelLoader_Moved.cs
|   |-- Parallax/
|   |   |-- BackgroundScroller.cs
|   |   |-- ParallaxAuthoringComponent.cs
|   |   |-- ParallaxBackground.cs
|   |   |-- ParallaxCamera.cs
|   |   `-- ParallaxLayer.cs
|   |-- Player/
|   |   |-- States/
|   |   |-- CharacterSceneConfig.cs
|   |   |-- DataManager.cs
|   |   |-- IPlayerFactory.cs
|   |   |-- InputManager.cs
|   |   |-- KillPlayer.cs
|   |   |-- KnockBack.cs
|   |   |-- KnockBackTriggerCheck.cs
|   |   |-- PlayerAttack.cs
|   |   |-- PlayerBombUpgrade.cs
|   |   |-- PlayerCloak.cs
|   |   |-- PlayerContext.cs
|   |   |-- PlayerController.cs
|   |   |-- PlayerDependencies.cs
|   |   |-- PlayerFactory.cs
|   |   |-- PlayerHSMBuilder.cs
|   |   |-- PlayerHealth.cs
|   |   |-- PlayerMovementStats.cs
|   |   |-- PlayerPrefabReference.cs
|   |   |-- PlayerRegistrationManager.cs
|   |   |-- PlayerSpawnerService.cs
|   |   |-- PlayerStateDriverShell.cs
|   |   |-- PlayerStateManager.cs
|   |   `-- SquashAndStretch.cs
|   |-- Save/
|   |   |-- SaveConfigSO.cs
|   |   |-- SaveDataContainer.cs
|   |   |-- SaveManager.cs
|   |   `-- SaveStateManager.cs
|   |-- Scene/
|   |   `-- IntroSceneLoader.cs
|   |-- Upgrades/
|   |   |-- BombUpgradeHandler.cs
|   |   |-- InvisibilityInputHandler.cs
|   |   |-- InvisibilityPickup.cs
|   |   |-- InvisibilityVisualPresenter.cs
|   |   |-- PlayerUpgrade.cs
|   |   |-- UpgradeIDs.cs
|   |   |-- UpgradeList.cs
|   |   |-- UpgradeListObject.cs
|   |   |-- UpgradeOrchestrator.cs
|   |   |-- UpgradePickup.cs
|   |   `-- UpgradeStateManager.cs
|   |-- Video/
|   |   `-- VideoPlayerMaster.cs
|   |-- World/
|   |   |-- BounceTrigger.cs
|   |   |-- Checkpoint.cs
|   |   |-- CheckpointManager.cs
|   |   |-- DamageFlash.cs
|   |   |-- DoorTriggerInteraction.cs
|   |   |-- ParallaxLayerMine.cs
|   |   |-- PlaySoundOnCollision.cs
|   |   |-- PopUpIngameText.cs
|   |   `-- TriggerInteractionBase.cs
|   |-- MainMenuSceneLifetimeScope.cs
|   |-- OptionsMenuSceneLifeScope.cs
|   `-- Persistence.cs
|
|-- UI/ (can see Core and Gameplay)
|   |-- UI.asmdef
|   |-- DialogueController/DialogueController.cs
|   |-- HUD/HealthBar.cs
|   |-- Notification/
|   |   |-- NotificationPanel.cs
|   |   |-- NotificationUI.cs
|   |   |-- NotificationView.cs
|   |   `-- PanelConfigSO.cs
|   |-- AudioSettingsUI.cs
|   |-- FadePanelManager.cs
|   |-- MainMenuFader.cs
|   |-- MainMenuSceneLifetimeScope.cs
|   |-- MainMenuView.cs
|   |-- MenuNavigationHighlighter.cs
|   `-- ObjectivesManager.cs
|
|-- Editor/ (editor-only; can see Core and Gameplay)
|   |-- Editor.asmdef
|   |-- CustomInspectors/
|   |   |-- DoorLabelHandle.cs
|   |   |-- ParallaxAuthoringEditor.cs
|   |   |-- SceneFieldPropertyDrawer.cs
|   |   `-- TriggerConfigDrawer.cs
|   `-- SceneTools/ParallaxAuthoringWindow.cs
|
`-- Helpers/
    |-- GameManagerTest.cs
    |-- TimeDiagnostic.cs
    |-- TimeScaleMonitor.cs
    `-- UIClickDebugger.cs
```

## Namespace summary

| Area | Namespace root |
| --- | --- |
| Core data, enums, interfaces, messages | `Game.Core.*` |
| Bootstrap services | `Game.Bootstrap` and `Game.Bootstrap.Scopes` |
| Gameplay features | `Game.Gameplay.*` |
| UI features | `Game.UI` |
| Editor tooling | `Game.Editor` |

The `Aduio`, `LevelLoader_Moved.cs`, and `SceneLifetimeScopeIntroVideo.cs` names are preserved exactly as they currently exist in the project.
