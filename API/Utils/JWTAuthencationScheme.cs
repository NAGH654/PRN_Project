using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Utils;

public static class JWTAuthenticationScheme
{
    public static IServiceCollection AddJWTAuthenticationScheme(this IServiceCollection services, IConfiguration config)
    {
        //add JWT service
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).
            AddJwtBearer("Bearer", options =>
            {
                var key = Encoding.UTF8.GetBytes(config.GetSection("Authentication:Key").Value!);
                string issuer = config.GetSection("Authentication:Issuer").Value!;
                string audience = config.GetSection("Authentication:Audience").Value!;

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
                options.Events = new JwtBearerEvents
                {
                    // Customize the 401 response
                    OnChallenge = context =>
                    {
                        // Skip the default response
                        context.HandleResponse();

                        // Customize the response
                        context.Response.StatusCode = Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var result = JsonSerializer.Serialize(new
                        {
                            message = "Access Denied. Token is missing or invalid."
                        });

                        return context.Response.WriteAsync(result);
                    },

                    // Customize the 403 response
                    OnForbidden = context =>
                    {
                        // Customize the response
                        context.Response.StatusCode = Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var result = JsonSerializer.Serialize(new
                        {
                            message = "Access Denied. You do not have permission to access this resource."
                        });

                        return context.Response.WriteAsync(result);
                    }
                };
            });
        return services;
    }
}