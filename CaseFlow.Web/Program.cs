using System.Text;
using System.Text.Json.Serialization;
using CaseFlow.Application.Interfaces;
using CaseFlow.Application.Services;
using CaseFlow.Infrastructure.Data;
using CaseFlow.Infrastructure.Models;
using CaseFlow.Infrastructure.Repositories;
using CaseFlow.Infrastructure.Seeding;
using CaseFlow.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Data / Identity --------------------

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// -------------------- Auth (JWT) --------------------

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKeyString = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKeyString))
    throw new InvalidOperationException("Jwt:Key is missing. Check appsettings.json / appsettings.Development.json.");

var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKeyString);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),

            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// -------------------- Controllers / JSON --------------------

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")   // Vite default
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// -------------------- Swagger --------------------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -------------------- DI (Repositories / Services) --------------------

builder.Services.AddScoped<IFormCaseRepository, FormCaseRepository>();
builder.Services.AddScoped<IFormCaseService, FormCaseService>();

builder.Services.AddScoped<IPdfAttachmentRepository, PdfAttachmentRepository>();
builder.Services.AddScoped<IAttachmentStorage, LocalAttachmentStorage>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IClarificationMessageRepository, ClarificationMessageRepository>();
builder.Services.AddScoped<IClarificationService, ClarificationService>();

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();

// -------------------- Build --------------------

var app = builder.Build();

// -------------------- Pipeline --------------------

// Production hardening
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Dev-only tooling + seeding
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await DevelopmentSeeder.SeedAsync(app.Services);
}

// IMPORTANT for Integration Tests:
// - TestServer does not need HTTPS redirect and it can produce unwanted redirects.
// - Keep HTTPS redirect only for real hosting environments.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Optional: only useful if you actually serve static assets
app.MapStaticAssets();

app.Run();

public partial class Program { }
