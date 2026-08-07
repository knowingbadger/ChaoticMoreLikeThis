using System;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

public class ChaosStateService
{
    private readonly ChaosSettingsStore _store;

    public ChaosStateService(ChaosSettingsStore store)
    {
        _store = store;
    }

    public async Task<bool> GetEffectiveChaosEnabledAsync(Guid userId)
    {
        var admin = await _store.GetAdminSettingsAsync().ConfigureAwait(false);

        if (!admin.MasterEnabled)
        {
            return false;
        }

        if (admin.ForceEnabledForAllUsers)
        {
            return true;
        }

        if (!admin.AllowUserToggle)
        {
            return admin.DefaultUserEnabled;
        }

        var user = await _store.GetUserSettingsAsync(userId).ConfigureAwait(false);

        if (user is null)
        {
            return admin.DefaultUserEnabled;
        }

        return user.Enabled;
    }
}