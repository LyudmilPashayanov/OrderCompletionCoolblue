namespace OrderCompletion.Api.Extensions;

public static class CorsExtensions
{
    public static void RegisterCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("CorsSettings").Get<string[]>();

        services.AddCors(
            opt =>
            {
                opt.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders("Location", "content-disposition", "content-type");
                    }
                );
            });
    }
}
