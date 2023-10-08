namespace api.Authentication;
public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication().AddJwtBearer();

        return builder;
    }
}