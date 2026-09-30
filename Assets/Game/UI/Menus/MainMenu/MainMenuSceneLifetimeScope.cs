using VContainer.Unity;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    [DefaultExecutionOrder(-100)]
    public class MainMenuSceneLifetimeScope : LifetimeScope
    {
        [Header("UI")]
        [SerializeField] private MenuMaster _mainMenuView;
        [SerializeField] private BootTextController _bootText;
        [SerializeField] private CRTBootAnimation cRTBootAnimation;

        protected override void Configure(IContainerBuilder builder)
        {
            // FIX #70: Fail-fast on missing references instead of silent skip
            if (_mainMenuView == null)
                throw new System.InvalidOperationException(
                    $"[MainMenuSceneScope] MenuMaster not assigned on '{gameObject.name}'.");

            if (_bootText == null)
                throw new System.InvalidOperationException(
                    $"[MainMenuSceneScope] BootTextController not assigned on '{gameObject.name}'.");

            if (cRTBootAnimation == null)
                throw new System.InvalidOperationException(
                    $"[MainMenuSceneScope] CRTBootAnimation not assigned on '{gameObject.name}'.");

            builder.RegisterComponent(_mainMenuView);
            builder.RegisterComponent(_bootText);
            builder.RegisterComponent(cRTBootAnimation);
        }
    }
}   