using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChaosMode
{
    HashCluster = 0,
    AttributeMisuse = 1,
    OppositeGenres = 2,
    DailyRotation = 3,
    AntiSimilarity = 4
}

public class ItemTypeSettings
{
    public bool Movie { get; set; } = true;
    public bool Series { get; set; } = true;
    public bool Episode { get; set; } = false;
    public bool MusicAlbum { get; set; } = false;
    public bool MusicArtist { get; set; } = false;
    public bool Book { get; set; } = false;
}

public class AdminChaosSettings
{
    public bool MasterEnabled { get; set; } = true;

    /// <summary>
    /// If true, the plugin will write chaos tags into item metadata.
    /// This affects the whole server.
    /// </summary>
    public bool ApplyTagsGlobally { get; set; } = true;

    public bool ApplyToAllLibraries { get; set; } = true;

    public Guid[] ExcludedLibraryIds { get; set; } = Array.Empty<Guid>();

    public int Seed { get; set; } = 20260807;

    /// <summary>
    /// 0-10. Used as a general hint if ClusterCount or TagsPerItem are left at 0.
    /// </summary>
    public int ChaosLevel { get; set; } = 8;

    /// <summary>
    /// Number of chaos buckets. Lower means more unrelated items share tags.
    /// </summary>
    public int ClusterCount { get; set; } = 32;

    /// <summary>
    /// Number of chaos tags assigned per item.
    /// </summary>
    public int TagsPerItem { get; set; } = 3;

    public string TagPrefix { get; set; } = "cmlt:";

    /// <summary>
    /// Extra prefixes to remove during cleanup if you ever changed the prefix.
    /// </summary>
    public string[] CleanupPrefixes { get; set; } = Array.Empty<string>();

    public ChaosMode Mode { get; set; } = ChaosMode.HashCluster;

    public bool CleanupOnDisable { get; set; } = true;

    /// <summary>
    /// Reserved for future use.
    /// </summary>
    public bool RunOnLibraryScan { get; set; } = true;

    public bool AllowUserToggle { get; set; } = true;

    public bool ForceEnabledForAllUsers { get; set; } = false;

    public bool DefaultUserEnabled { get; set; } = false;

    /// <summary>
    /// Reserved for future per-user chaos generation.
    /// </summary>
    public bool AllowUserPersonalSeed { get; set; } = false;

    /// <summary>
    /// Reserved for future per-user chaos generation.
    /// </summary>
    public bool AllowUserChaosLevelOverride { get; set; } = false;

    public ItemTypeSettings ItemTypes { get; set; } = new();
}

public class UserChaosSettings
{
    public Guid UserId { get; set; }

    public bool Enabled { get; set; }

    public int? PersonalSeed { get; set; }

    public int? PersonalChaosLevel { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class UpdateUserChaosRequest
{
    public bool Enabled { get; set; }
}

public class UserConfigResponse
{
    public Guid UserId { get; set; }

    public bool Enabled { get; set; }

    public bool EffectiveEnabled { get; set; }

    public bool UserToggleAllowed { get; set; }

    public bool AdminForced { get; set; }

    public bool MasterEnabled { get; set; }
}