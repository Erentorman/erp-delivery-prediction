using MockErp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MockErpDataStore>();
builder.Services.AddHealthChecks();

var app = builder.Build();

_ = app.Services.GetRequiredService<MockErpDataStore>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

// Docker/Compose healthcheck target. MockErp.Api has no authentication
// middleware configured, so no anonymous-access opt-in is needed.
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
