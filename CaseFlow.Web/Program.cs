using System.Text;
using CaseFlow.Application.Interfaces;
using CaseFlow.Application.Services;
using Microsoft.EntityFrameworkCore;
using CaseFlow.Infrastructure.Seeding;
using CaseFlow.Infrastructure.Data;
using CaseFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using CaseFlow.Infrastructure.Models;
using CaseFlow.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT
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
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IFormCaseRepository, FormCaseRepository>();
builder.Services.AddScoped<IFormCaseService, FormCaseService>();

builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IPdfAttachmentRepository, PdfAttachmentRepository>();
builder.Services.AddScoped<IAttachmentStorage, LocalAttachmentStorage>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await DevelopmentSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program { }