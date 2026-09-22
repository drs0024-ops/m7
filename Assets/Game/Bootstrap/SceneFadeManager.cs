using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Bootstrap
{
    public class SceneFadeManager : MonoBehaviour
    {
        [SerializeField] private Image _fadeImage;
        [Range(0.1f, 10f)] [SerializeField] private float _fadeOutSpeed = 5f;
        [Range(0.1f, 10f)] [SerializeField] private float _fadeInSpeed = 5f;
        [SerializeField] private Color _baseColor;
        [SerializeField] private bool _startBlack;

        private int _tweenId = -1;
        private UniTaskCompletionSource _activeTcs;

        private void Awake()
        {
            _baseColor.a = _startBlack ? 1f : 0f;
            if (_fadeImage != null)
                _fadeImage.color = _baseColor;
        }

        public UniTask FadeOutAsync(CancellationToken cancellationToken = default)
        {
            if (_fadeImage == null) return UniTask.CompletedTask;

            CancelFade();

            var tcs = new UniTaskCompletionSource();
            _activeTcs = tcs;
            float duration = 1f / _fadeOutSpeed;

            _tweenId = LeanTween.value(_fadeImage.gameObject, UpdateAlpha, _fadeImage.color.a, 1f, duration)
                .setEase(LeanTweenType.linear)
                .setOnComplete(() =>
                {
                    _baseColor.a = 1f;
                    _fadeImage.color = _baseColor;
                    _activeTcs = null;
                    tcs.TrySetResult();
                }).id;

            if (cancellationToken != default)
            {
                cancellationToken.Register(() =>
                {
                    CancelFade();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }

        public UniTask FadeInAsync(CancellationToken cancellationToken = default)
        {
            if (_fadeImage == null) return UniTask.CompletedTask;

            CancelFade();

            var tcs = new UniTaskCompletionSource();
            _activeTcs = tcs;
            float duration = 1f / _fadeInSpeed;

            _tweenId = LeanTween.value(_fadeImage.gameObject, UpdateAlpha, _fadeImage.color.a, 0f, duration)
                .setEase(LeanTweenType.linear)
                .setOnComplete(() =>
                {
                    _baseColor.a = 0f;
                    _fadeImage.color = _baseColor;
                    _activeTcs = null;
                    tcs.TrySetResult();
                }).id;

            if (cancellationToken != default)
            {
                cancellationToken.Register(() =>
                {
                    CancelFade();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }

        public void CancelFade()
        {
            if (_tweenId != -1)
            {
                LeanTween.cancel(_tweenId);
                _tweenId = -1;
            }

            // Complete the orphaned tcs so the awaiting caller doesn't hang
            if (_activeTcs != null)
            {
                _activeTcs.TrySetCanceled();
                _activeTcs = null;
            }
        }

        private void OnDestroy()
        {
            CancelFade();
        }

        private void UpdateAlpha(float alpha)
        {
            _baseColor.a = alpha;
            _fadeImage.color = _baseColor;
        }
    }
}   

/*

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ScenFadeManager : MonoBehaviour
{
    public static ScenFadeManager instance;
     #pragma warning disable 0649
    [SerializeField] private Image _fadeOutImage;
     #pragma warning restore 0649
    [Range(0.1f, 10f), SerializeField] private float _fadeOutSpeed = 5f;
    [Range(0.1f, 10f), SerializeField] private float _fadeInSpeed = 5f;
    [SerializeField] private Color _fadeOutStartColor;
    [SerializeField] public bool StartBlack;

    public bool IsFadingOut { get; private set; }
    public bool IsFadingIn {get; private set;}

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        //set start at black based on bool "StartBlack"
        if (StartBlack)
        {

            _fadeOutStartColor.a = 1f;
            _fadeOutImage.color = _fadeOutStartColor;
            StartFadeIn();
        }

        //set alpha color at 0
        else
        {
            _fadeOutStartColor.a = 0f;

        }

    }

    private void Update()
    {
        if (IsFadingOut)
        {
            if (_fadeOutImage.color.a < 1f)
            {
                if (GameManager2.Instance.GameState1 == GameState1.Paused)
                {
                    _fadeOutStartColor.a += Time.deltaTime * 0.01f;
                }
                else
                {
                    _fadeOutStartColor.a += Time.deltaTime * _fadeOutSpeed;
                }
                
                _fadeOutImage.color = _fadeOutStartColor;
            }
            else
            {
                IsFadingOut = false;
            }
        }

        if (IsFadingIn)
        {
            if (_fadeOutImage.color.a > 0f)
            {
                if (GameManager2.Instance.GameState1 == GameState1.Paused)
                {
                    _fadeOutStartColor.a -= Time.deltaTime * 0.01f;
                }
                else
                {
                    _fadeOutStartColor.a -= Time.deltaTime * _fadeInSpeed;
                }
                
                _fadeOutImage.color = _fadeOutStartColor;
            }
            else
            {
                IsFadingIn = false;
            }
        }
    }

    public void StartFadeOut () {
        //Debug.Log("StartFadOut");
        _fadeOutImage.color = _fadeOutStartColor;
        IsFadingOut = true;
    }

    public void StartFadeIn () {
        if (_fadeOutImage.color.a >= 1f)
        {
            _fadeOutImage.color = _fadeOutStartColor;
            IsFadingIn = true;
        }
    }
}
*/