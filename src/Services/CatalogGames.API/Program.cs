using Catalog.API.Data;
using Catalog.API.Integration;
using Catalog.API.Integration.ItemEvents;
using CatalogGames.API.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();

//builder.Services.AddDbContext<CatalogContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// la conexióna la bd estádefinidaen appsettings
string? db2Use = Environment.GetEnvironmentVariable("DB2USE");
// En caso de que el entorno de despliegue producción (Docker)
if (db2Use == "DockerSQLServer")
{
    builder.Services.AddDbContext<CatalogContext>(options => {
        options.UseSqlServer(builder.Configuration
        .GetConnectionString("SQLServerDockerConnection"),
        sqlServerOptionsAction: sqlOptions => {
            //Recuperamos el nombre del Ensamblado donde está la migración
            sqlOptions.MigrationsAssembly(
    Assembly.GetExecutingAssembly().GetName().Name);
            //Configuramos una conexión resiliente a la BD
            sqlOptions.
    EnableRetryOnFailure(maxRetryCount: 5,
    maxRetryDelay: TimeSpan.FromSeconds(30),
    errorNumbersToAdd: null);
        });
    });
}
else
{
    builder.Services.AddDbContext<CatalogContext>(options => options
    .UseSqlServer(builder.Configuration
    .GetConnectionString("DefaultConnection")));
}

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
     options => {
         options.SwaggerDoc("v1",
            new OpenApiInfo
            {  // Uso el array de parámetros por nombre.
                Title = "API Rest de Videojuegos eShopOnContainers",
                Contact = new OpenApiContact
                {
                    Email = "apmm2.@gcloud.ua.es", //cambiar estos datos por los nuestros
                    Name = "Antonio Pascual Mas Martínez", //cambiar estos datos por los nuestros
                    Url = new Uri("https://www.ua.es/")
                },
                License = new OpenApiLicense
                {
                    Name = "Apache License, 2.0",
                    Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
                },
                // Cuidado que Version es obligatorio.
                Version = "v1"
            });

         // Para añadir la información de documentación del XML generado a Swagger.
         var xmlfile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
         var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlfile);

     });

builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<RabbitMqExchange>(builder.Configuration.GetSection("RabbitMqExchange"));
builder.Services.AddSingleton<IEventsPublisher, EventsPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();


using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        //itcreatestheDB in case itdoesnot exist
        //thisis usedonlywhiledevelopingthesystem
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Anerror occurredseedingtheDB.");
    }
}

app.MapControllers();

try
{
    //registeringthepublisher
    app.Lifetime.ApplicationStarted.Register(() => {
        var eventsPublisher = app.Services.GetService<IEventsPublisher>();
        eventsPublisher.Start();
    });
    app.Lifetime.ApplicationStopping.Register(() => {
        var eventsPublisher = app.Services.GetService<IEventsPublisher>();
        eventsPublisher.Stop();
    });
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Anerror whileconnectingto RabbitMq.");
}

app.Run();
