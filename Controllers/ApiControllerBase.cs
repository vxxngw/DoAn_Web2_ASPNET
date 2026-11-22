using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Recruitment.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int? ViewerId =>
        int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out int id) ? id : null;

    // Dùng trong action có [Authorize]. JWT đã được kiểm tra sub khi xác thực.
    protected int UserId => ViewerId!.Value;
}
