using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(
        IServiceCollection serviceCollection,
        IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<ChaosSettingsStore>();
        serviceCollection.AddSingleton<ChaosStateService>();
        serviceCollection.AddSingleton<ChaosTagService>();
    }
}
