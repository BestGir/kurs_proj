using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Hse.Ratings.Api.Controllers
{
    // Запрос на логин: ожидаем только пароль
    public record LoginRequest(string Password);

    // Ответ на логин: отдаём JWT-токен
    public record LoginResponse(string Token);

    // Ответ на whoami: имя, роль и список клеймов
    public record WhoAmIResponse(string Name, string? Role, IEnumerable<object> Claims);

    /// <summary>
    /// Контроллер аутентификации: вход по паролю администратора и проверка токена.
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _cfg;

        // Внедрение конфигурации (для Admin:Password, Jwt:* и т.д.)
        public AuthController(IConfiguration cfg) => _cfg = cfg;

        // POST /api/v1/auth/login
        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest req)
        {
            // Простой локальный пароль. Можно вынести в Secret Manager или переменные окружения.
            var expected = _cfg["Admin:Password"] ?? "admin123";
            if (string.IsNullOrWhiteSpace(req.Password) || req.Password != expected)
                return Unauthorized();

            // Настройки JWT: издатель, аудитория, ключ
            var issuer   = _cfg["Jwt:Issuer"]   ?? "Hse.Ratings";
            var audience = _cfg["Jwt:Audience"] ?? "Hse.Ratings";
            var key      = _cfg["Jwt:Key"]      ?? "DEV_SUPER_SECRET_KEY_MIN_32_CHARS";

            // Клеймы пользователя (минимальный набор для админа)
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "admin"),
                new(ClaimTypes.Role, "Admin")
            };

            // Подпись токена (симметричный ключ HMAC-SHA256)
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            // Формирование JWT с ограниченным сроком действия
            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds);

            // Сериализация токена в строку
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Ok(new LoginResponse(token));
        }

        // GET /api/v1/auth/whoami  (проверка токена)
        [Authorize]
        [HttpGet("whoami")]
        public ActionResult<WhoAmIResponse> WhoAmI()
        {
            // Достаём имя, роль и все клеймы из контекста пользователя
            var name  = User.Identity?.Name ?? "unknown";
            var role  = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var all   = User.Claims.Select(c => new { c.Type, c.Value });

            return Ok(new WhoAmIResponse(name, role, all));
        }

        // Просто «пинг» без авторизации, для быстрой проверки живости API
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { ok = true, time = DateTime.UtcNow });
    }
}
