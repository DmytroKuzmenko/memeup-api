using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Memeup.Api.Data;
using Memeup.Api.Domain.Auth;
using Microsoft.Extensions.FileProviders;
using HealthChecks.NpgSql;

var builder = WebApplication.CreateBuilder(args);

// ----- Serilog -----
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ----- DbContext + Identity -----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? builder.Configuration["ConnectionStrings:DefaultConnection"];

builder.Services.AddDbContext<MemeupDbContext>(opt =>
{
    opt.UseNpgsql(connectionString, npg => npg.EnableRetryOnFailure());
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<MemeupDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ----- JWT -----
var jwtKey = builder.Configuration["JWT:Key"] ?? "dev_secret_key_please_change_dev_secret_key_please_change";
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "memeup";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "memeup_frontend";
var jwtLifetime = int.TryParse(builder.Configuration["JWT:LifetimeMinutes"], out var l) ? l : 60;
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ----- Controllers + Swagger -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Memeup API", Version = "v1" });

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: Bearer {token}",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme{ Reference = new OpenApiReference{ Type=ReferenceType.SecurityScheme, Id="Bearer"}}, Array.Empty<string>() }
    });
});

// ----- CORS -----
string[] GetAllowedOrigins()
{
    var csv = builder.Configuration["Cors:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(csv))
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var arr = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    return (arr is { Length: > 0 }) ? arr : new[] { "http://localhost:4200" };
}
builder.Services.AddCors(o => o.AddPolicy("Frontend",
    p => p.WithOrigins(GetAllowedOrigins()).AllowAnyHeader().AllowAnyMethod()));

// ----- Health -----
builder.Services.AddHealthChecks().AddNpgSql(connectionString!, name: "postgres");

// ----- Build -----
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { ok = true, name = "Memeup API", version = "v1" }));
app.MapHealthChecks("/health");

// Статика /uploads
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// ---- Миграции (всегда) + опционально "только миграции и выйти"
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MemeupDbContext>();
        // Применяем все миграции при обычном запуске
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to apply migrations on startup");
        throw;
    }
}

// Если запрошен режим "только миграции" — выходим
var runMigrationsOnly = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase)
                        || string.Equals(builder.Configuration["RUN_MIGRATIONS"], "1", StringComparison.Ordinal);
if (runMigrationsOnly)
{
    Log.Information("RUN_MIGRATIONS flag detected. Exiting after migrations.");
    return; // завершить процесс
}

// ---- Сидинг администратора ----
using (var scope = app.Services.CreateScope())
{
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var adminEmail = builder.Configuration["ADMIN_EMAIL"];
    var adminPassword = builder.Configuration["ADMIN_PASSWORD"];

    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
    {
        if (!await roleMgr.RoleExistsAsync("Admin"))
            await roleMgr.CreateAsync(new IdentityRole<Guid>("Admin"));

        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userMgr.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userMgr.AddToRoleAsync(admin, "Admin");
                Log.Information("Seeded admin user {Email}", adminEmail);
            }
            else
            {
                Log.Error("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

app.Run();
