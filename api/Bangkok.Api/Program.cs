using Bangkok.Application.Configuration;
using Bangkok.Application.Models;
using Bangkok.Api.Middleware;
using Bangkok.Api.Services;
using Bangkok.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog: replace default logging
builder.Host.UseSerilog((context, services, configuration) =>
{
    var env = context.HostingEnvironment;
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithProperty("Environment", env.EnvironmentName)
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "Bangkok.Api")
        .Enrich.FromLogContext();
});

// Options
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection(CorsSettings.SectionName));

// SQL + Application services (Dapper, Repositories, Auth)
builder.Services.AddInfrastructure(builder.Configuration);

// In-memory IP brute force protection (single-instance only)
builder.Services.AddSingleton<IIpBlockService, IpBlockService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (!string.IsNullOrEmpty(jwtSettings?.SigningKey))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
}
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "sql",
        tags: new[] { "ready", "db" });

builder.Services.AddControllers();

// CORS: configurable origins, headers, methods (from appsettings Cors section)
var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsSettings?.AllowedOrigins?.Length > 0)
            policy.WithOrigins(corsSettings.AllowedOrigins);
        if (corsSettings?.AllowedHeaders?.Length > 0)
            policy.WithHeaders(corsSettings.AllowedHeaders);
        if (corsSettings?.AllowedMethods?.Length > 0)
            policy.WithMethods(corsSettings.AllowedMethods);
    });
});

// Rate limiting: IP-based, global + endpoint-specific auth limits
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    static string GetPartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.AddPolicy("GlobalPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 100
        }));

    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5
        }));

    options.AddPolicy("RegisterPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5
        }));

    options.AddPolicy("ForgotPasswordPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetPartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(5),
            PermitLimit = 3
        }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        if (httpContext.Response.HasStarted)
            return;

        var clientIp = GetPartitionKey(httpContext);
        var endpointName = httpContext.GetEndpoint()?.DisplayName ?? httpContext.Request.Path.ToString();
        Log.Warning("Rate limit exceeded. Endpoint: {Endpoint}, IP: {ClientIp}, Timestamp: {Timestamp:O}",
            endpointName, clientIp, DateTime.UtcNow);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var retrySeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
            retrySeconds = (int)retryAfter.TotalSeconds;
        httpContext.Response.Headers.RetryAfter = retrySeconds.ToString(CultureInfo.InvariantCulture);

        httpContext.Response.ContentType = "application/json";
        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? httpContext.TraceIdentifier;
        var body = ApiResponse<object>.Fail(new ErrorResponse
        {
            Code = "RATE_LIMIT_EXCEEDED",
            Message = "Too many requests. Please try again later."
        }, correlationId);
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await httpContext.Response.WriteAsync(json, cancellationToken).ConfigureAwait(false);
    };
});

// Swagger (development only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.EnableAnnotations();
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Bangkok API",
            Version = "v1",
            Description = "Enterprise Web API foundation with JWT authentication."
        });
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Bearer. Enter your token."
        });
        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

var app = builder.Build();

try
{
    Log.Information("Starting Bangkok API");
}
catch { }

// Seed role management (Roles, Permissions, RolePermissions, assign Admin to legacy admins)
try
{
    using var scope = app.Services.CreateScope();
    var seedRunner = scope.ServiceProvider.GetRequiredService<Bangkok.Infrastructure.Services.RoleSeedRunner>();
    await seedRunner.RunAsync(CancellationToken.None).ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Warning(ex, "Role seed failed (tables may not exist yet). Run SQL migrations first.");
}

// Correlation ID (sets TraceIdentifier), RequestId for Serilog, then Exception handling
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestIdEnricherMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bangkok API v1");
        c.DisplayRequestDuration();
    });
}

static async Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var result = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        entries = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds
        })
    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
    await context.Response.WriteAsync(result).ConfigureAwait(false);
}

await app.RunAsync().ConfigureAwait(false);
