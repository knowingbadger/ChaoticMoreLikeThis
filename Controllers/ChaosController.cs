using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ChaoticMoreLikeThis;

[ApiController]
[Route("ChaoticMoreLikeThis")]
public class ChaosController : ControllerBase
{
    private readonly ChaosSettingsStore _store;
    private readonly ChaosStateService _state;
    private readonly ChaosTagService _tagService;
    private readonly IUserManager _userManager;
    private readonly ILogger<ChaosController> _logger;

    public ChaosController(
        ChaosSettingsStore store,
        ChaosStateService state,
        ChaosTagService tagService,
        IUserManager userManager,
        ILogger<ChaosController> logger)
    {
        _store = store;
        _state = state;
        _tagService = tagService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("AdminConfig")]
    [Authorize]
    public async Task<IActionResult> GetAdminConfig()
    {
        if (!await IsAdminAsync().ConfigureAwait(false))
        {
            return Unauthorized();
        }

        var settings = await _store.GetAdminSettingsAsync().ConfigureAwait(false);
        return Ok(settings);
    }

    [HttpPost("AdminConfig")]
    [Authorize]
    public async Task<IActionResult> SetAdminConfig([FromBody] AdminChaosSettings settings)
    {
        if (!await IsAdminAsync().ConfigureAwait(false))
        {
            return Unauthorized();
        }

        await _store.SaveAdminSettingsAsync(settings).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("UserConfig")]
    [Authorize]
    public async Task<IActionResult> GetUserConfig()
    {
        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var admin = await _store.GetAdminSettingsAsync().ConfigureAwait(false);
        var user = await _store.GetUserSettingsAsync(userId).ConfigureAwait(false);
        var effective = await _state.GetEffectiveChaosEnabledAsync(userId).ConfigureAwait(false);

        var response = new UserConfigResponse
        {
            UserId = userId,
            Enabled = user?.Enabled ?? admin.DefaultUserEnabled,
            EffectiveEnabled = effective,
            UserToggleAllowed = admin.MasterEnabled && admin.AllowUserToggle && !admin.ForceEnabledForAllUsers,
            AdminForced = admin.ForceEnabledForAllUsers,
            MasterEnabled = admin.MasterEnabled
        };

        return Ok(response);
    }

    [HttpPost("UserConfig")]
    [Authorize]
    public async Task<IActionResult> SetUserConfig([FromBody] UpdateUserChaosRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var admin = await _store.GetAdminSettingsAsync().ConfigureAwait(false);

        if (!admin.AllowUserToggle)
        {
            return Conflict(new
            {
                error = "User toggles are disabled by the administrator."
            });
        }

        if (admin.ForceEnabledForAllUsers)
        {
            return Conflict(new
            {
                error = "The administrator has forced chaos for all users."
            });
        }

        var settings = await _store.GetUserSettingsAsync(userId).ConfigureAwait(false);

        settings ??= new UserChaosSettings
        {
            UserId = userId
        };

        settings.Enabled = request.Enabled;
        settings.UpdatedAt = DateTime.UtcNow;

        await _store.SaveUserSettingsAsync(settings).ConfigureAwait(false);

        return NoContent();
    }

    [HttpPost("Apply")]
    [Authorize]
    public async Task<IActionResult> Apply()
    {
        if (!await IsAdminAsync().ConfigureAwait(false))
        {
            return Unauthorized();
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _tagService.ApplyAsync(null, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background chaos apply failed.");
            }
        });

        return Accepted();
    }

    [HttpPost("Cleanup")]
    [Authorize]
    public async Task<IActionResult> Cleanup()
    {
        if (!await IsAdminAsync().ConfigureAwait(false))
        {
            return Unauthorized();
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _tagService.CleanupAsync(null, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background chaos cleanup failed.");
            }
        });

        return Accepted();
    }

    private Guid GetCurrentUserId()
    {
        var claims = User?.Claims;

        if (claims is null)
        {
            return Guid.Empty;
        }

        foreach (var claim in claims)
        {
            if (claim.Type == ClaimTypes.NameIdentifier || claim.Type == "sub")
            {
                if (Guid.TryParse(claim.Value, out var userId))
                {
                    return userId;
                }
            }
        }

        return Guid.Empty;
    }

    private async Task<bool> IsAdminAsync()
    {
        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = _userManager.GetUserById(userId);

        if (user is null)
        {
            return false;
        }

        return user.HasPermission(PermissionKind.IsAdministrator);
    }
}
