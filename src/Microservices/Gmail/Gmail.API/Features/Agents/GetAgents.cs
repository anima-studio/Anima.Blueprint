namespace Moser.Archetype.Gmail.Domain.Features.Agents;

public static class GetAgents
{
    public record Query(Guid UserId) : IQuery<List<AgentStatus>>;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/agents", async (HttpContext ctx, IQueryBus bus) =>
        {
            var userId = Guid.Parse(ctx.User.FindFirst("userId")!.Value);
            var agents = await bus.Send(new Query(userId));
            return Results.Ok(agents);
        })
        .RequireAuthorization();
    }

    public class Handler : IQueryHandler<Query, List<AgentStatus>>
    {
        private readonly IGmailDbContext _db;
        private readonly IImapClient _imap;

        public Handler(IGmailDbContext db, IImapClient imap)
        {
            _db = db;
            _imap = imap;
        }

        public async Task<List<AgentStatus>> Handle(Query query, CancellationToken ct)
        {
            var user = await _db.Users.FindAsync(new object[] { query.UserId }, ct);
            return await _imap.FetchAgentDrafts(user!.GmailUsername, user.GmailAppPassword);
        }
    }
}
