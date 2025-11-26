using Anima.Blueprint.BuildingBlocks.Application;

using System.Reflection;

namespace Anima.Blueprint.Gmail.API.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        // CQRS
        builder.Services.AddCqrs(Assembly.GetExecutingAssembly());

        // Database
        builder.Services.AddDbContext<GmailDbContext>(opt =>
            opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
        builder.Services.AddScoped<IGmailDbContext>(sp => sp.GetRequiredService<GmailDbContext>());

        // Infrastructure
        builder.Services.AddScoped<IImapClient, ImapClient>();
        builder.Services.AddSingleton<IQueuePublisher>(sp =>
            new AzureQueuePublisher(builder.Configuration.GetConnectionString("StorageQueue")));

        // JWT Auth
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });
        builder.Services.AddAuthorization();

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        return builder;
    }
}
