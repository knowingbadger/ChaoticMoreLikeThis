using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static Plugin Instance { get; private set; } = null!;

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "Chaotic More Like This";

    public override Guid Id => Guid.Parse("6F9E6C2E-3C3B-4A5D-9C0B-2E4D6E5A7C91");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "Chaotic Admin",
                EmbeddedResourcePath = $"{GetType().Namespace}.web.adminPage.html"
            },
            new PluginPageInfo
            {
                Name = "Chaotic User",
                EmbeddedResourcePath = $"{GetType().Namespace}.web.userPage.html"
            }
        };
    }
}

public class PluginConfiguration : BasePluginConfiguration
{
    // The real admin settings are stored as JSON by ChaosSettingsStore.
    // This empty configuration class satisfies Jellyfin's plugin requirements.
}