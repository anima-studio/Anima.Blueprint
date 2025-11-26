namespace Anima.Blueprint.Gmail.Domain.Features.Auth;

public static class Register
{
    public record Request(string Gmail, string Password, string AppPassword);
    public record Command(string Gmail, string Password, string AppPassword) : ICommand<Guid>;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (Request req, ICommandBus bus) =>
        {
            var userId = await bus.Send(new Command(req.Gmail, req.Password, req.AppPassword));
            return Results.Ok(new { userId });
        });
    }

    public class Handler : ICommandHandler<Command, Guid>
    {
        private readonly IGmailDbContext _db;

        public Handler(IGmailDbContext db) => _db = db;

        public async Task<Guid> Handle(Command cmd, CancellationToken ct)
        {
            var user = User.Register(cmd.Gmail, cmd.Password, cmd.AppPassword);
            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
            return user.Id;
        }
    }
}
