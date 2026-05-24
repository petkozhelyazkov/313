using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Infrastructure.Auth;
using Trading313.Api.Infrastructure.MarketData;
using Trading313.Api.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "FrontendCors";

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;

        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<Trading313.Api.Services.Auth.IAuthService, Trading313.Api.Services.Auth.AuthService>();
builder.Services.AddScoped<Trading313.Api.Services.Users.IUserService, Trading313.Api.Services.Users.UserService>();
builder.Services.AddScoped<Trading313.Api.Services.Users.IAchievementService, Trading313.Api.Services.Users.AchievementService>();

builder.Services.Configure<TwelveDataOptions>(builder.Configuration.GetSection(TwelveDataOptions.SectionName));
builder.Services.AddMemoryCache();

builder.Services
    .AddHttpClient(TwelveDataClient.HttpClientName, (sp, client) =>
    {
        var opts = sp.GetRequiredService<IConfiguration>().GetSection(TwelveDataOptions.SectionName).Get<TwelveDataOptions>()
                   ?? new TwelveDataOptions();
        client.BaseAddress = new Uri(opts.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(
        retryCount: 2,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));

builder.Services.AddSingleton<TwelveDataRateLimiter>();
builder.Services.AddSingleton<ITwelveDataClient, TwelveDataClient>();

builder.Services.AddScoped<Trading313.Api.Services.Stocks.IStockService, Trading313.Api.Services.Stocks.StockService>();
builder.Services.AddScoped<Trading313.Api.Services.Stocks.ICompanyProfileService, Trading313.Api.Services.Stocks.CompanyProfileService>();
builder.Services.AddScoped<Trading313.Api.Services.MarketData.IQuoteService, Trading313.Api.Services.MarketData.QuoteService>();
builder.Services.AddScoped<Trading313.Api.Services.MarketData.IHistoryService, Trading313.Api.Services.MarketData.HistoryService>();
builder.Services.AddScoped<Trading313.Api.Services.Portfolio.IPortfolioService, Trading313.Api.Services.Portfolio.PortfolioService>();
builder.Services.AddScoped<Trading313.Api.Services.Portfolio.IPortfolioQueryService, Trading313.Api.Services.Portfolio.PortfolioQueryService>();
builder.Services.AddScoped<Trading313.Api.Services.Portfolio.ITaxReportService, Trading313.Api.Services.Portfolio.TaxReportService>();
builder.Services.AddScoped<Trading313.Api.Services.Watchlist.IWatchlistService, Trading313.Api.Services.Watchlist.WatchlistService>();
builder.Services.AddScoped<Trading313.Api.Services.Analytics.ISnapshotService, Trading313.Api.Services.Analytics.SnapshotService>();
builder.Services.AddScoped<Trading313.Api.Services.Analytics.IEarningsService, Trading313.Api.Services.Analytics.EarningsService>();
builder.Services.AddScoped<Trading313.Api.Services.Analytics.IAdvancedMetricsService, Trading313.Api.Services.Analytics.AdvancedMetricsService>();
builder.Services.AddScoped<Trading313.Api.Services.Admin.IAdminUserService, Trading313.Api.Services.Admin.AdminUserService>();
builder.Services.AddScoped<Trading313.Api.Services.Orders.IOrdersService, Trading313.Api.Services.Orders.OrdersService>();
builder.Services.AddScoped<Trading313.Api.Services.Dividends.IDividendsService, Trading313.Api.Services.Dividends.DividendsService>();
builder.Services.AddScoped<Trading313.Api.Services.RecurringOrders.IRecurringOrdersService, Trading313.Api.Services.RecurringOrders.RecurringOrdersService>();
builder.Services.AddScoped<Trading313.Api.Services.Goals.IGoalsService, Trading313.Api.Services.Goals.GoalsService>();
builder.Services.AddScoped<Trading313.Api.Services.Stocks.IStockSplitsService, Trading313.Api.Services.Stocks.StockSplitsService>();
builder.Services.AddScoped<Trading313.Api.Services.Stocks.IAnalystService, Trading313.Api.Services.Stocks.AnalystService>();
builder.Services.AddScoped<Trading313.Api.Services.Stocks.IInsiderService, Trading313.Api.Services.Stocks.InsiderService>();
builder.Services.AddScoped<Trading313.Api.Services.Digests.IEmailDigestService, Trading313.Api.Services.Digests.EmailDigestService>();
builder.Services.Configure<Trading313.Api.Services.Digests.SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddHostedService<Trading313.Api.Background.EmailDigestBackgroundService>();
builder.Services.AddSingleton<Trading313.Api.Realtime.IPricePublisher, Trading313.Api.Realtime.PricePublisher>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<Trading313.Api.Background.DailySnapshotService>();
builder.Services.AddHostedService<Trading313.Api.Background.QuoteRefreshService>();
builder.Services.AddHostedService<Trading313.Api.Background.OrderExecutionService>();
builder.Services.AddHostedService<Trading313.Api.Background.AlertEvaluationService>();
builder.Services.AddHostedService<Trading313.Api.Background.RecurringOrderService>();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        // SignalR sends the JWT via ?access_token=… on the negotiate request,
        // since the browser WebSocket API can't set Authorization headers.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Brute-force protection on /api/auth/login and /api/auth/register: 5 attempts per minute per IP.
    options.AddPolicy("auth", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:5173", "http://localhost:5174" };

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Trading313 API",
        Version = "v1",
        Description = "Stock portfolio management & analytics API. Educational/paper-trading project.",
        Contact = new OpenApiContact
        {
            Name = builder.Configuration["App:AuthorName"] ?? string.Empty,
            Email = builder.Configuration["App:AuthorEmail"] ?? string.Empty,
        },
        License = new OpenApiLicense { Name = "MIT" }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT here. No \"Bearer \" prefix needed."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await using (var startupScope = app.Services.CreateAsyncScope())
{
    var seeder = startupScope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();

    if (args.Contains("seed"))
    {
        var demo = startupScope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await demo.SeedAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading313 API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Security headers — applied to every response. Production hardening for the thesis chapter.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    if (!app.Environment.IsDevelopment())
    {
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next();
});

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Trading313.Api.Realtime.PriceHub>("/hub/prices");

app.Run();
