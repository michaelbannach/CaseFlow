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

// -------------------- DB --------------------

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// -------------------- Services --------------------

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFormCaseService, FormCaseService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IClarificationService, ClarificationService>();
builder.Services.AddScoped<IPdfAttachmentRepository, PdfAttachmentRepository>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IFormCaseRepository, FormCaseRepository>();
builder.Services.AddScoped<IClarificationMessageRepository, ClarificationMessageRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttachmentStorage, LocalAttachmentStorage>();

// -------------------- CORS --------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// -------------------- Auth (JWT) --------------------

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKeyString = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtKeyString))
    throw new InvalidOperationException("Jwt:Key is missing. Check appsettings.json / appsettings.Development.json.");

var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKeyString);
if (jwtKeyBytes.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits) for HS256.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var isTesting = builder.Environment.IsEnvironment("Testing");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // In Tests: Issuer/Audience nicht erzwingen
            ValidateIssuer = !isTesting,
            ValidIssuer = jwtSection["Issuer"],

            ValidateAudience = !isTesting,
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
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// -------------------- Pipeline --------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    await DevelopmentSeeder.SeedAsync(app.Services);
}

// IMPORTANT for Integration Tests:
// - TestServer does not need HTTPS redirect and it can produce unwanted redirects.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
