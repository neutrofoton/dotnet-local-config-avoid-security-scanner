using Microsoft.AspNetCore.Mvc;

namespace SecureConfigApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigDemoController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigDemoController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("safe")]
    public IActionResult Safe()
    {
        return Ok(new
        {
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            EmailHost = _configuration["Email:Host"]
        });
    }

    [HttpGet("danger")]
    public IActionResult Danger()
    {
        return Ok(new
        {
            ConnectionString = _configuration["ConnectionStrings:DefaultConnection"],
            JwtKey = _configuration["Jwt:Key"],
            EmailPassword = _configuration["Email:Password"]
        });
    }
}
