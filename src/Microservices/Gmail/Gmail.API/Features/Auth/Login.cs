using System.Security.Claims;
using System.Text;

namespace Anima.Blueprint.Gmail.Domain.Features.Auth;

public static class Login
{
    public record Request(string Gmail, string Password);
    public record Query(string Gmail, string Password) : IRequest<string>;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (Request req, ISender sender) =>
        {
            var token = await sender.Send(new Query(req.Gmail, req.Password));
            return Results.Ok(new { token });
        })
        .WithName("Login")
        .Produces<object>(200);
    }

    public class Handler : IRequestHandler<Query, string>
    {
        private readonly IGmailDbContext _db;
        private readonly IConfiguration _config;

        public Handler(IGmailDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<string> Handle(Query query, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.GmailUsername == query.Gmail, ct);
            if (user == null || !BCrypt.Net.BCrypt.Verify(query.Password, user.PasswordHash))
                throw new UnauthorizedAccessException();

            return GenerateJwt(user.Id);
        }

        private string GenerateJwt(Guid userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                claims: new[] { new Claim("userId", userId.ToString()) },
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
