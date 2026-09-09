using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RetroBackend.Auth;
using RetroBackend.Config;
using RetroBackend.Data;
using RetroBackend.Hubs;
using RetroBackend.Models;
using RetroBackend.Repositories;
using RetroBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = AvatarImage.MaxBytes;
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "RetroBackend API",
        Version = "v1",
        Description = "A REST API for managing retrospectives.",
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token.",
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<RetroDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        IdentityPasswordPolicy.Apply(options);
        options.Tokens.PasswordResetTokenProvider = PasswordResetTokenProviderOptions.ProviderName;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<RetroDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<PasswordResetTokenProvider<AppUser>>(PasswordResetTokenProviderOptions.ProviderName)
    .AddTokenProvider<InvitationTokenProvider<AppUser>>(InvitationTokenProviderOptions.ProviderName);

builder.Services.Configure<PasswordResetTokenProviderOptions>(options =>
{
    options.Name = PasswordResetTokenProviderOptions.ProviderName;
    options.TokenLifespan = TimeSpan.FromMinutes(30);
});

builder.Services.Configure<InvitationTokenProviderOptions>(options =>
{
    options.Name = InvitationTokenProviderOptions.ProviderName;
    options.TokenLifespan = TimeSpan.FromDays(30);
});

DataProtectionKeys.AddPersisted(builder.Services, builder.Configuration, builder.Environment);

var jwtSigningKey = JwtSigningKey.Resolve(builder.Configuration);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey)),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                    return;

                var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var presentedStamp = identity.FindFirst(AuthClaims.SecurityStamp)?.Value;
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(presentedStamp))
                {
                    context.Fail("Invalid token.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId);
                if (user is null || await userManager.GetSecurityStampAsync(user) != presentedStamp)
                {
                    context.Fail("Token is no longer valid.");
                    return;
                }

                var roleValues = identity.Claims
                    .Where(c =>
                        string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "roles", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var roleValue in roleValues)
                {
                    if (!identity.HasClaim(ClaimTypes.Role, roleValue))
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));

                    if (!identity.HasClaim("role", roleValue))
                        identity.AddClaim(new Claim("role", roleValue));
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1),
            }));
});
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddSingleton<RetrospectiveHubConnectionTracker>();
builder.Services.AddSignalR();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();
builder.Services.AddScoped<IRetrospectiveRepository, EfRetrospectiveRepository>();
builder.Services.AddScoped<IRetrospectiveService, RetrospectiveService>();
builder.Services.AddScoped<IItemRepository, EfItemRepository>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IRetroAuthorizationService, RetroAuthorizationService>();
builder.Services.AddScoped<IRetrospectiveRealtimeService, RetrospectiveRealtimeService>();
builder.Services.AddScoped<IRetrospectiveLiveNotifier, RetrospectiveLiveNotifier>();
builder.Services.AddSingleton<IRetrospectiveHubMembership, RetrospectiveHubMembership>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RetroDbContext>();
    await db.Database.MigrateAsync();
    await IdentityBootstrap.EnsureRolesAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RetroBackend API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RetrospectiveHub>("/hubs/retrospective");

app.Run();

