using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public static class ChaosTagGenerator
{
    public static string[] GetTags(BaseItem item, AdminChaosSettings admin)
    {
        if (!admin.MasterEnabled || !admin.ApplyTagsGlobally)
        {
            return Array.Empty<string>();
        }

        var level = Math.Clamp(admin.ChaosLevel, 0, 10);

        var clusterCount = admin.ClusterCount > 0
            ? admin.ClusterCount
            : Math.Clamp(64 - level * 4, 8, 64);

        var tagsPerItem = admin.TagsPerItem > 0
            ? admin.TagsPerItem
            : 2 + level / 3;

        var prefix = string.IsNullOrWhiteSpace(admin.TagPrefix)
            ? "cmlt:"
            : admin.TagPrefix;

        var inputParts = new List<string>
        {
            admin.Seed.ToString(),
            admin.Mode.ToString(),
            item.ParentId.ToString("N"),
            item.Id.ToString("N"),
            level.ToString()
        };

        switch (admin.Mode)
        {
            case ChaosMode.DailyRotation:
                inputParts.Add(DateTime.UtcNow.ToString("yyyyMMdd"));
                break;

            case ChaosMode.AttributeMisuse:
                inputParts.Add($"nameLength:{item.Name?.Length ?? 0}");
                inputParts.Add($"year:{item.ProductionYear ?? 0}");
                inputParts.Add($"runtime:{item.RunTimeTicks ?? 0}");
                break;

            case ChaosMode.OppositeGenres:
                var genres = item.Genres ?? Array.Empty<string>();
                var normalizedGenres = string.Join(
                    ",",
                    genres
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .OrderBy(g => g, StringComparer.OrdinalIgnoreCase));

                inputParts.Add($"genres:{normalizedGenres}");
                break;

            case ChaosMode.HashCluster:
            case ChaosMode.AntiSimilarity:
            default:
                break;
        }

        var input = string.Join("|", inputParts);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < hashBytes.Length && tags.Count < tagsPerItem; i++)
        {
            var cluster = hashBytes[i] % clusterCount;
            tags.Add($"{prefix}{cluster:00}");
        }

        return tags.ToArray();
    }
}