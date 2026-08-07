using AttendVisionReportsApi.Data;
using AttendVisionReportsApi.Services;
using AttendVisionReportsApi.Services.HikCentral;
using AttendVisionReportsApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Linq;
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
        builder.Services.AddScoped<IFilterService, FilterService>();
        builder.Services.AddScoped<ITimeOverrideService, TimeOverrideService>();
        builder.Services.AddScoped<IEmployeeLeaveService, EmployeeLeaveService>();
        builder.Services.AddScoped<ITimeManagementService, TimeManagementService>();
        builder.Services.AddScoped<IEmployeeSyncService, EmployeeSyncService>();

        // Department Payment Rate Service
        builder.Services.AddScoped<DepartmentPaymentRateService>();

        // Health Check Service
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

        // HikCentral OpenAPI (Artemis) Integration
        builder.Services.AddTransient<ArtemisSigningHandler>();
        builder.Services.AddHttpClient<IHikCentralService, HikCentralService>(client =>
        {
            var baseUrl = builder.Configuration["HikCentral:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
                client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<ArtemisSigningHandler>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = builder.Configuration.GetValue<bool>("HikCentral:AllowInvalidCertificate")
                ? (_, _, _, _) => true
                : null
        });

        // CORS Policy Configuration
        var allowedOriginsString = builder.Configuration["CorsSettings:AllowedOrigins"];
        var allowedOrigins = (allowedOriginsString ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .ToArray();
        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    // In Development, also allow any VS Code dev tunnel origin (e.g. https://xxxx-4200.devtunnels.ms)
                    // so port-forwarded testing works without editing config every time a new tunnel is created.
                    policy.SetIsOriginAllowed(origin =>
                              allowedOrigins.Contains(origin) ||
                              (isDevelopment &&
                               Uri.TryCreate(origin, UriKind.Absolute, out var originUri) &&
                               originUri.Host.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase)))
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
        {
            app.MapOpenApi();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseRouting();

        // CORS must run between UseRouting and UseEndpoints, and before authentication/authorization
        app.UseCors("CorsPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }
}