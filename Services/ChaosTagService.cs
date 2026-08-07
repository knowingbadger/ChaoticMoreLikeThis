using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public class ChaosTagService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ChaosSettingsStore _store;
    private readonly ILogger<ChaosTagService> _logger;

    public ChaosTagService(
        ILibraryManager libraryManager,
        ChaosSettingsStore store,
        ILogger<ChaosTagService> logger)
    {
        _libraryManager = libraryManager;
        _store = store;
        _logger = logger;
    }

    public async Task<(int Processed, int Modified)> ApplyAsync(
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var admin = await _store.GetAdminSettingsAsync().ConfigureAwait(false);

        if (!admin.MasterEnabled || !admin.ApplyTagsGlobally || !admin.ApplyToAllLibraries)
        {
            if (admin.CleanupOnDisable)
            {
                return await CleanupAsync(progress, cancellationToken).ConfigureAwait(false);
            }

            return (0, 0);
        }

        var itemKinds = GetSelectedItemKinds(admin);

        if (itemKinds.Count == 0)
        {
            _logger.LogInformation("No item types selected for chaos application.");
            return (0, 0);
        }

        var query = new InternalItemsQuery
        {
            Recursive = true,
            IsVirtualItem = false,
            IncludeItemTypes = itemKinds.ToArray()
        };

        var items = _libraryManager.GetItemList(query).ToList();

        var processed = 0;
        var modified = 0;
        var total = items.Count;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (admin.ExcludedLibraryIds.Length > 0 &&
                admin.ExcludedLibraryIds.Contains(item.ParentId))
            {
                processed++;
                continue;
            }

            var desiredTags = ChaosTagGenerator.GetTags(item, admin);

            var existingTags = item.Tags ?? Array.Empty<string>();

            var cleanedTags = existingTags
                .Where(tag => !ShouldRemoveTag(tag, admin))
                .ToList();

            var newTags = cleanedTags
                .Concat(desiredTags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (!newTags.SequenceEqual(existingTags, StringComparer.OrdinalIgnoreCase))
            {
                item.Tags = newTags;
                await SaveItemAsync(item, cancellationToken).ConfigureAwait(false);
                modified++;
            }

            processed++;

            if (total > 0)
            {
                progress?.Report(processed * 100.0 / total);
            }
        }

        _logger.LogInformation(
            "Chaos apply completed. Processed: {Processed}, Modified: {Modified}",
            processed,
            modified);

        return (processed, modified);
    }

    public async Task<(int Processed, int Modified)> CleanupAsync(
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var admin = await _store.GetAdminSettingsAsync().ConfigureAwait(false);

        var query = new InternalItemsQuery
        {
            Recursive = true,
            IsVirtualItem = false,
            IncludeItemTypes = GetAllSupportedItemKinds().ToArray()
        };

        var items = _libraryManager.GetItemList(query).ToList();

        var processed = 0;
        var modified = 0;
        var total = items.Count;

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingTags = item.Tags ?? Array.Empty<string>();

            var newTags = existingTags
                .Where(tag => !ShouldRemoveTag(tag, admin))
                .ToArray();

            if (!newTags.SequenceEqual(existingTags, StringComparer.OrdinalIgnoreCase))
            {
                item.Tags = newTags;
                await SaveItemAsync(item, cancellationToken).ConfigureAwait(false);
                modified++;
            }

            processed++;

            if (total > 0)
            {
                progress?.Report(processed * 100.0 / total);
            }
        }

        _logger.LogInformation(
            "Chaos cleanup completed. Processed: {Processed}, Modified: {Modified}",
            processed,
            modified);

        return (processed, modified);
    }

    private async Task SaveItemAsync(BaseItem item, CancellationToken cancellationToken)
    {
        // This is the common modern Jellyfin path.
        await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken)
            .ConfigureAwait(false);

        // If your Jellyfin build does not expose UpdateToRepositoryAsync, use this instead:
        //
        // await _libraryManager.UpdateItemAsync(
        //     item,
        //     item.GetParent(),
        //     ItemUpdateType.MetadataEdit,
        //     cancellationToken).ConfigureAwait(false);
    }

    private static List<BaseItemKind> GetSelectedItemKinds(AdminChaosSettings admin)
    {
        var kinds = new List<BaseItemKind>();

        if (admin.ItemTypes.Movie)
        {
            kinds.Add(BaseItemKind.Movie);
        }

        if (admin.ItemTypes.Series)
        {
            kinds.Add(BaseItemKind.Series);
        }

        if (admin.ItemTypes.Episode)
        {
            kinds.Add(BaseItemKind.Episode);
        }

        if (admin.ItemTypes.MusicAlbum)
        {
            kinds.Add(BaseItemKind.MusicAlbum);
        }

        if (admin.ItemTypes.MusicArtist)
        {
            kinds.Add(BaseItemKind.MusicArtist);
        }

        if (admin.ItemTypes.Book)
        {
            kinds.Add(BaseItemKind.Book);
        }

        return kinds;
    }

    private static List<BaseItemKind> GetAllSupportedItemKinds()
    {
        return new List<BaseItemKind>
        {
            BaseItemKind.Movie,
            BaseItemKind.Series,
            BaseItemKind.Episode,
            BaseItemKind.MusicAlbum,
            BaseItemKind.MusicArtist,
            BaseItemKind.Book
        };
    }

    private static bool ShouldRemoveTag(string tag, AdminChaosSettings admin)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(admin.TagPrefix) &&
            tag.StartsWith(admin.TagPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (admin.CleanupPrefixes is not null)
        {
            foreach (var prefix in admin.CleanupPrefixes)
            {
                if (!string.IsNullOrWhiteSpace(prefix) &&
                    tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}