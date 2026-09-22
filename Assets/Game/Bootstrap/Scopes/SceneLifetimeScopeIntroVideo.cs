using Game.Gameplay;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public class SceneLifetimeScopeIntroVideo : LifetimeScope
    {
        [SerializeField] private VideoPlayerMaster _videoPlayerMaster;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_videoPlayerMaster != null)
                builder.RegisterComponent(_videoPlayerMaster).AsImplementedInterfaces();
            else
                builder.RegisterComponentInHierarchy<VideoPlayerMaster>().AsImplementedInterfaces();

        }
    }
}   