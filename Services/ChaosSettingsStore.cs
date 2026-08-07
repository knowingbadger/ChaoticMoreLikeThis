using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public class ChaosSettingsStore
{
    private readonly string _adminPath;
    private readonly string _usersDirectory;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public ChaosSettingsStore(IApplicationPaths applicationPaths)
    {
        var directory = Path.Combine(applicationPaths.PluginConfigurationsPath, "ChaoticMoreLikeThis");
        Directory.CreateDirectory(directory);

        _usersDirectory = Path.Combine(directory, "users");
        Directory.CreateDirectory(_usersDirectory);

        _adminPath = Path.Combine(directory, "admin.json");
    }

    public async Task<AdminChaosSettings> GetAdminSettingsAsync()
    {
        await _mutex.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!File.Exists(_adminPath))
            {
                return Normalize(new AdminChaosSettings());
            }

            var json = await File.ReadAllTextAsync(_adminPath).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Normalize(new AdminChaosSettings());
            }

            var settings = JsonSerializer.Deserialize<AdminChaosSettings>(json, JsonOptions);
            return Normalize(settings ?? new AdminChaosSettings());
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SaveAdminSettingsAsync(AdminChaosSettings settings)
    {
        settings = Normalize(settings);

        await _mutex.WaitAsync().ConfigureAwait(false);

        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_adminPath, json).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<UserChaosSettings?> GetUserSettingsAsync(Guid userId)
    {
        var path = GetUserPath(userId);

        await _mutex.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<UserChaosSettings>(json, JsonOptions);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task SaveUserSettingsAsync(UserChaosSettings settings)
    {
        var path = GetUserPath(settings.UserId);

        await _mutex.WaitAsync().ConfigureAwait(false);

        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private string GetUserPath(Guid userId)
    {
        return Path.Combine(_usersDirectory, $"{userId:N}.json");
    }

    private static AdminChaosSettings Normalize(AdminChaosSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TagPrefix))
        {
            settings.TagPrefix = "cmlt:";
        }

        settings.TagPrefix = settings.TagPrefix.Trim();

        if (!settings.TagPrefix.EndsWith(':'))
        {
            settings.TagPrefix += ":";
        }

        if (settings.ClusterCount <= 0)
        {
            settings.ClusterCount = 32;
        }

        if (settings.TagsPerItem <= 0)
        {
            settings.TagsPerItem = 3;
        }

        if (settings.ChaosLevel < 0)
        {
            settings.ChaosLevel = 0;
        }

        if (settings.ChaosLevel > 10)
        {
            settings.ChaosLevel = 10;
        }

        settings.ExcludedLibraryIds ??= Array.Empty<Guid>();
        settings.CleanupPrefixes ??= Array.Empty<string>();
        settings.ItemTypes ??= new ItemTypeSettings();

        return settings;
    }
}