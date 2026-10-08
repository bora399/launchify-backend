using FluentValidation;
using Google.Cloud.Firestore;
using Launchify.API.Middlewares;
using Launchify.Application.Common.Behaviors;
using Launchify.Application.Interfaces;
using Launchify.Infrastructure.Repositories;
using Launchify.Infrastructure.Services;
using Launchify.Persistence.Repositories;
using LaunchifyBackend.Hubs;
using Microsoft.AspNetCore.HttpOverrides;
using System.IO;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Render / Cloudflare Proxy Header Desteði
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_JSON");

if (!string.IsNullOrEmpty(firebaseJson))
{
    string tempCredentialsFilePath = Path.GetTempFileName();
    File.WriteAllText(tempCredentialsFilePath, firebaseJson);
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempCredentialsFilePath);
}
else
{
    string firebaseKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-key.json");
    if (File.Exists(firebaseKeyPath))
    {
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseKeyPath);
    }
    else
    {
        Console.WriteLine("UYARI: Firebase kimliði bulunamadý! Veritabaný iþlemleri baþarýsýz olabilir.");
    }
}

var projectId = builder.Configuration["Firebase:ProjectId"] ?? "masalimiz-2d8a4";
builder.Services.AddSingleton(provider => FirestoreDb.Create(projectId));

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("AiCreationLimit", httpContext =>
    {
        string partitionKey = httpContext.Request.Headers["X-User-Id"].FirstOrDefault()
                              ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                              ?? httpContext.Connection.RemoteIpAddress?.ToString()
                              ?? Guid.NewGuid().ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync("{\"message\": \"Saatlik oluþturma sýnýrýna ulaþýldý. Lütfen biraz bekleyin.\"}", token);
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "https://launchify-frontend-theta.vercel.app",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddScoped<ILandingPageRepository, LandingPageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAiGeneratorService, GeminiAiService>();
builder.Services.AddSingleton<AnalyticsQueueService>();
builder.Services.AddHostedService<AnalyticsBackgroundWorker>();
builder.Services.AddScoped<IWaitlistRepository, WaitlistRepository>();

builder.Services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();
app.MapHub<GenerationHub>("/generationHub");

app.Run();