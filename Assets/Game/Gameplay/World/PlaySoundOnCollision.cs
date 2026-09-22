using UnityEngine;

namespace Game.Gameplay.World
{
    public class PlaySoundOnCollision : MonoBehaviour
    {
        [SerializeField] private AudioClip _clipOnEnter;
        [SerializeField] private AudioClip _clipOnExit;
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private float _volume = 1f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_targetLayer.value & (1 << other.gameObject.layer)) == 0) return;
            if (_clipOnEnter != null) _source.PlayOneShot(_clipOnEnter, _volume);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if ((_targetLayer.value & (1 << other.gameObject.layer)) == 0) return;
            if (_clipOnExit != null) _source.PlayOneShot(_clipOnExit, _volume);
        }
    }   
}