using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public class ApplyChaosTask : IScheduledTask
{
    private readonly ChaosTagService _service;

    public ApplyChaosTask(ChaosTagService service)
    {
        _service = service;
    }

    public string Name => "Apply Chaotic More Like This";

    public string Key => "ChaoticMoreLikeThisApply";

    public string Description => "Applies deterministic chaos tags to library items.";

    public string Category => "Library";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }

    public async Task ExecuteAsync(
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        await _service.ApplyAsync(progress, cancellationToken).ConfigureAwait(false);
    }
}