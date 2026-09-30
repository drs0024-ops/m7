using VContainer.Unity;
using UnityEngine;
using VContainer;

namespace Game.Gameplay
{
    [DefaultExecutionOrder(-100)]
    public class IntroVideoSceneLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<VideoPlayerMaster>()
                .AsImplementedInterfaces();
        }
    }
}   