using System.Reflection;

namespace Anima.Blueprint.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetMethod("Map", BindingFlags.Public | BindingFlags.Static) != null);

        foreach (var type in endpointTypes)
            type.GetMethod("Map")!.Invoke(null, new object[] { app });

        return app;
    }
}
