using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Parallax
{
    [ExecuteInEditMode]
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private ParallaxCamera _parallaxCamera;
        [SerializeField] private List<ParallaxLayer> _parallaxLayers = new();

        private void Start()
        {
            if (_parallaxCamera == null)
                _parallaxCamera = FindAnyObjectByType<ParallaxCamera>();

            SetLayers();

            if (_parallaxCamera != null)
                _parallaxCamera.onCameraTranslate += Move;
        }

        private void OnDisable()
        {
            if (_parallaxCamera != null)
                _parallaxCamera.onCameraTranslate -= Move;
        }

        private void SetLayers()
        {
            _parallaxLayers.Clear();
            var allLayers = GetComponentsInChildren<ParallaxLayer>(true);
            for (int i = 0; i < allLayers.Length; i++)
                _parallaxLayers.Add(allLayers[i]);
        }

        private void Move(Vector3 delta)
        {
            for (int i = 0; i < _parallaxLayers.Count; i++)
                _parallaxLayers[i].Move(delta);
        }
    }
}   