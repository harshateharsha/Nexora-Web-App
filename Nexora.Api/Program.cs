using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexora.Api.Data;
using Nexora.Api.Hubs;
using Nexora.Api.Models;
using Nexora.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add DbContext (SQL Server)
builder.Services.AddDbContext<NexoraDbContext>(opts =>
    opts.UseSqlServer(config.GetConnectionString("Default")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
{
    opts.User.RequireUniqueEmail = true;
    opts.SignIn.RequireConfirmedAccount = false; // we'll enforce via OTP manually in login
})
.AddEntityFrameworkStores<NexoraDbContext>()
.AddDefaultTokenProviders();

// JWT
var jwt = config.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new Exception("Jwt:Key is not configured in appsettings");
var issuer = jwt["Issuer"] ?? "Nexora";
var audience = jwt["Audience"] ?? "Nexora";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
    };

    // Allow access_token in query string for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].FirstOrDefault();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// CORS - allow Angular dev origin
builder.Services.AddCors(p => p.AddPolicy("spa", b =>
    b.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// SignalR
builder.Services.AddSignalR();

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// App services
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("spa");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
