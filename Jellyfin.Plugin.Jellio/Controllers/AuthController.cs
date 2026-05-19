using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellio.Helpers;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Controllers;

[ApiController]
[Route("jelliopp")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController(ISessionManager sessionManager) : ControllerBase
{
    [Authorize]
    [HttpPost("start-session")]
    public async Task<IActionResult> StartSession()
    {
        var userId = RequestHelpers.GetCurrentUserId(User) ?? throw new ResourceNotFoundException();

        var authenticationResult = await sessionManager
            .AuthenticateDirect(
                new AuthenticationRequest
                {
                    UserId = userId,
                    DeviceId = Guid.NewGuid().ToString(),
                    DeviceName = "Jellio++",
                    App = "Jellio++",
                    AppVersion = Plugin.Instance?.Version.ToString() ?? "unknown",
                }
            )
            .ConfigureAwait(false);

        return Ok(new { accessToken = authenticationResult.AccessToken });
    }
}
