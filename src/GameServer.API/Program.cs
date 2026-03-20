using GameServer.Application;
using GameServer.Infrastructure;
using GameServer.Infrastructure.Data;
using GameServer.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GameServer API", Version = "v1", Description = "Game Server - Onion Architecture - .NET 8" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{{
        new OpenApiSecurityScheme{Reference=new OpenApiReference{Type=ReferenceType.SecurityScheme,Id="Bearer"}},
        Array.Empty<string>()
    }});
});

// JWT Authentication
var secret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is missing from appsettings.json!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrated OK.");
    }
    catch (Exception ex) { Log.Error(ex, "Migration failed: {Msg}", ex.Message); }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameServer API v1"));
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("GameServer API started!");
app.Run();
