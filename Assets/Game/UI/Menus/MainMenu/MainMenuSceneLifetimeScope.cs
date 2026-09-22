using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public class MainMenuSceneLifetimeScope : LifetimeScope
    {
        [Header("UI")]
        [SerializeField] private MenuMaster _mainMenuView;
        [SerializeField] private BootTextController _bootText;
        [SerializeField] private CRTBootAnimation cRTBootAnimation;

        
        protected override void Configure(IContainerBuilder builder)
        {
            if (_mainMenuView != null)
                builder.RegisterComponent(_mainMenuView);

            if (_bootText != null)
                builder.RegisterComponent(_bootText);

            if (cRTBootAnimation != null)
                builder.RegisterComponent(cRTBootAnimation);

            
        }
    }
}   