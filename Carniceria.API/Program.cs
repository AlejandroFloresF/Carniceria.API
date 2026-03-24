using System.Reflection;
using Carniceria.Infrastructure;
using Carniceria.Infrastructure.Persistence;
using Carniceria.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.Load("Carniceria.Application")));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.SetIsOriginAllowed(origin => {
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                // Permite localhost y cualquier IP privada 192.168.x.x / 10.x.x.x
                return uri.Host == "localhost"
                    || uri.Host.StartsWith("192.168.")
                    || uri.Host.StartsWith("10.")
                    || uri.Host.StartsWith("172.");
            }
            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
