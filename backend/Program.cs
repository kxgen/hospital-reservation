using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"] ?? "fallback_key_for_dev_only";

// Configure Email Settings
builder.Services.Configure<Backend.Models.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<Backend.Services.IEmailService, Backend.Services.EmailService>();

// Register Repositories for DI
var conn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddScoped(sp => new AccountRepository(conn));
builder.Services.AddScoped(sp => new DoctorRepository(conn));
builder.Services.AddScoped(sp => new PatientRepository(conn));
builder.Services.AddScoped(sp => new ReceptionistRepository(conn));
builder.Services.AddScoped(sp => new AdminRepository(conn));
builder.Services.AddScoped(sp => new AppointmentRepository(conn));
builder.Services.AddScoped(sp => new AuditLogRepository(conn));
builder.Services.AddScoped(sp => new NotificationRepository(conn));
builder.Services.AddSingleton<Backend.Utils.JwtGenerator>();

// Register Background Services
builder.Services.AddHostedService<Backend.Services.AppointmentStatusService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

// --- CORS setup ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("CombinedPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
                {
                    if (string.IsNullOrWhiteSpace(origin)) return false;
                    var uri = new Uri(origin);
                    return uri.Host == "localhost" || 
                           uri.Host == "trinityspecializedcenter.vercel.app" || 
                           uri.Host.EndsWith(".vercel.app");
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            return new BadRequestObjectResult(context.ModelState);
        };
    })
    .AddJsonOptions(options =>
    {
        // Convert all enums to their string representation in JSON
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors("CombinedPolicy");

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
