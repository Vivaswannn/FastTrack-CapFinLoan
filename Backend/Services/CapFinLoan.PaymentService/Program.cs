using System.Text;
using CapFinLoan.PaymentService.Data;
using CapFinLoan.PaymentService.Exceptions;
using CapFinLoan.PaymentService.Messaging;
using CapFinLoan.PaymentService.Services;
using CapFinLoan.PaymentService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ── Controllers & Swagger ────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "CapFinLoan Payment Service",
        Version = "v1",
        Description = "Loan disbursement Saga participant (port 5006)"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("AllowReact", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ── JWT Authentication ───────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer   = true,
        ValidIssuer      = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience    = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew        = TimeSpan.Zero,
        RoleClaimType    = "role",
        NameClaimType    = "name"
    };
});

builder.Services.AddAuthorization();

// ── Database ─────────────────────────────────────────────
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// ── Dependency Injection ─────────────────────────────────
builder.Services.AddScoped<IPaymentService, PaymentProcessingService>();
builder.Services.AddSingleton<PaymentEventPublisher>();

// ── Saga: RabbitMQ background consumer ──────────────────
builder.Services.AddHostedService<LoanApprovedConsumer>();

// ── Build ─────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CapFinLoan Payment Service v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("AllowReact");
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => new
{
    status  = "Healthy",
    service = "CapFinLoan.PaymentService",
    timestamp = DateTime.UtcNow
});

Log.Information("CapFinLoan PaymentService starting on port 5006");

app.Run();
