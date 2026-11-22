using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Recruitment.Api.Services;

public class JwtSettings
{
    [Required] public string Issuer { get; set; } = "";
    [Required] public string Audience { get; set; } = "";
    [Required, MinLength(32)] public string Key { get; set; } = "";
    [Range(1, 1440)] public int LifetimeMinutes { get; set; }

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
