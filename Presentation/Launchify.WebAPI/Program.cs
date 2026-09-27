using Launchify.Application.Interfaces;
using Launchify.Infrastructure.Repositories;
using Masalimiz.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

string firebaseKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "firebase-key.json");
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebaseKeyPath);
// 1. Controller ve Swagger Ayarlarý
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. CORS Politikasý (Next.js localhost:3000'den gelen isteklere izin ver)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Frontend portun
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 3. Dependency Injection (Arayüzleri gerçek sýnýflara baðlýyoruz)
builder.Services.AddScoped<ILandingPageRepository, LandingPageRepository>();
builder.Services.AddScoped<IAiGeneratorService, GeminiAiService>();

// 4. MediatR (CQRS) Kurulumu
// Projedeki tüm Command ve Handler'larý otomatik bulup kaydeder
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

var app = builder.Build();

// 5. HTTP Ýstek Hattý (Pipeline)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS'u uygulamaya dahil et
app.UseCors("AllowNextJs");

app.UseAuthorization();
app.MapControllers();

app.Run();