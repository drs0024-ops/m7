using VContainer;
using VContainer.Unity;

namespace Game.Gameplay
{
    public class IntroVideoSceneLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<VideoPlayerMaster>()
                .AsImplementedInterfaces();
        }
    }
}   