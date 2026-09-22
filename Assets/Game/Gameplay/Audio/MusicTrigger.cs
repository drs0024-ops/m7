using VContainer;
using MessagePipe;
using Game.Core.Messages;
using UnityEngine;

namespace Game.Gameplay.Audio
{
    public class MusicTrigger : MonoBehaviour
    {
        [SerializeField] private string _tagFilter = "Player";
        [SerializeField] private MusicAction _action = MusicAction.Stop;
        [SerializeField] private AudioClip _clipToPlay;
        [SerializeField] private float _volumeToSet = 1f;

        private IPublisher<StopMusic> _stopPublisher;
        private IPublisher<PlayMusic> _playPublisher;
        private IPublisher<SetVolume> _volumePublisher;

        [Inject]
        public void Construct(
            IPublisher<StopMusic> stopPublisher,
            IPublisher<PlayMusic> playPublisher,
            IPublisher<SetVolume> volumePublisher)
        {
            _stopPublisher = stopPublisher;
            _playPublisher = playPublisher;
            _volumePublisher = volumePublisher;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!string.IsNullOrEmpty(_tagFilter) && !other.CompareTag(_tagFilter)) return;

            switch (_action)
            {
                case MusicAction.Stop:
                    _stopPublisher.Publish(StopMusic.Default);
                    break;
                case MusicAction.Play:
                    _playPublisher.Publish(new PlayMusic(_clipToPlay, true));
                    break;
                case MusicAction.ChangeVolume:
                    _volumePublisher.Publish(new SetVolume(_volumeToSet));
                    break;
            }
        }

        public enum MusicAction { Stop, Play, ChangeVolume }
    }
}   