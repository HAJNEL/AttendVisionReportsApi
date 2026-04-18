using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddEnvironmentVariables();

        // Database
        var connectionString = builder.Configuration.GetConnectionString("Default") ?? "";
        if (!string.IsNullOrEmpty(connectionString))
        {
            builder.Services.AddNpgsqlDataSource(connectionString);
            builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));
        }

        // JWT Authentication
        var jwtKeyString = builder.Configuration["Jwt:Key"] ?? "a-very-long-and-secure-default-key-for-development-purposes";
        var jwtKey = Encoding.UTF8.GetBytes(jwtKeyString);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        // Services
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IDepartmentsService, DepartmentsService>();
        builder.Services.AddScoped<IReportsService, ReportsService>();
        builder.Services.AddScoped<ICompanyService, CompanyService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();

        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IRoleService, RoleService>();

        builder.Services.AddScoped<IDepartmentUserService, DepartmentUserService>();

        // Health Check Service
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

        // CORS Policy Configuration
        var allowedOriginsString = builder.Configuration["CorsSettings:AllowedOrigins"];
        var allowedOrigins = (allowedOriginsString ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    // Fallback to allow any origin for debugging if none are specified in config
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }

                var maxAge = builder.Configuration.GetValue<int>("CorsSettings:PreflightMaxAge");
                if (maxAge > 0)
                {
                    policy.SetPreflightMaxAge(TimeSpan.FromSeconds(maxAge));
                }
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseHttpsRedirection();
        app.UseRouting();

        // CORS must run between UseRouting and UseEndpoints, and before authentication/authorization
        app.UseCors("CorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }
}