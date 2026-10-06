using System;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Core.Enums;
using Game.Core.Interfaces;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Coordinates cutscene playback for intro, new-game, and load sequences.
    /// Owns cutscene selection and playback so GameFlowSystem does not depend on cutscene content.
    /// </summary>
    public class IntroSequenceController : IIntroSequenceController
    {
        #region Dependencies

        private readonly CutsceneSelector _cutsceneSelector;
        private readonly CutscenePlayer _cutscenePlayer;
        private readonly GamePhaseController _phaseController;
        private readonly SceneLoaderService _loader;
        private readonly SceneRegistry _sceneRegistry;

        #endregion

        #region State

        private bool _initialLoadStarted;

        #endregion

        #region Construction

        public IntroSequenceController(
            CutsceneSelector cutsceneSelector,
            CutscenePlayer cutscenePlayer,
            GamePhaseController phaseController,
            SceneLoaderService loader,
            SceneRegistry sceneRegistry)
        {
            _cutsceneSelector = cutsceneSelector;
            _cutscenePlayer = cutscenePlayer;
            _phaseController = phaseController;
            _loader = loader;
            _sceneRegistry = sceneRegistry;
        }

        #endregion

        #region IIntroSequenceController

        public async UniTask PlayIntroAsync()
        {
            var clip = _cutsceneSelector.GetForIntro();
            Debug.Log($"[IntroTrace] IntroSequenceController selected intro clip: {(clip == null ? "<null>" : clip.name)}.");
            if (clip != null)
            {
                _phaseController.SetPhase(GamePhase.Cinematic);
                try
                {
                    await _cutscenePlayer.PlayAsync(clip);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[IntroSequence] Intro cutscene failed: {e}");
                }
            }

            _phaseController.SetPhase(GamePhase.Menu);
            await TryInitialLoadAsync();
        }

        public void OnVideoFinished()
        {
            if (!_phaseController.HasPhase(GamePhase.Cinematic)) return;
            _phaseController.SetPhase(GamePhase.Menu);
            TryInitialLoadAsync().Forget();
        }

        public async UniTask PlayNewGameCutsceneAsync()
        {
            var cutscene = _cutsceneSelector.GetForNewGame();
            if (cutscene == null) return;

            _phaseController.SetPhase(GamePhase.Cinematic);
            await _cutscenePlayer.PlayAsync(cutscene);
        }

        public async UniTask PlayLoadCutsceneAsync(string levelName)
        {
            var cutscene = _cutsceneSelector.GetForLoad(levelName);
            if (cutscene == null) return;

            _phaseController.SetPhase(GamePhase.Cinematic);
            await _cutscenePlayer.PlayAsync(cutscene);
        }

        #endregion

        #region Internal

        private async UniTask TryInitialLoadAsync()
        {
            if (_initialLoadStarted) return;
            _initialLoadStarted = true;

            await UniTask.DelayFrame(1);

            if (_sceneRegistry != null && _sceneRegistry.MainMenuScene != null)
                await _loader.EnsureLoadedAndActivate(_sceneRegistry.MainMenuScene);
        }

        #endregion
    }
}