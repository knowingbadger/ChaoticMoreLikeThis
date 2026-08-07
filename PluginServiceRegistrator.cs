using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(
        IServiceCollection serviceCollection,
        IApplicationPaths applicationPaths)
    {
        serviceCollection.AddSingleton<ChaosSettingsStore>();
        serviceCollection.AddSingleton<ChaosStateService>();
        serviceCollection.AddSingleton<ChaosTagService>();
    }
}