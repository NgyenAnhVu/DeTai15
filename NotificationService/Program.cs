using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));
builder.Services.AddSignalR();
builder.Services.AddHostedService<NotificationConsumer>();

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => "NotificationService đang chạy...");
app.MapHub<NotificationHub>("/notificationHub");

app.Run("http://localhost:5200");