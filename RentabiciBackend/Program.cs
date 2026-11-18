using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using RentabiciBackend.BackgroundJobs;
using RentabiciBackend.Data;
using RentabiciBackend.Dtos;
using RentabiciBackend.Filters;
using RentabiciBackend.Middleware;
using RentabiciBackend.Security;
using RentabiciBackend.Services;
using RentabiciBackend.Services.Notificaciones;
using RentabiciBackend.Services.Pagos;
using RentabiciBackend.Services.Tarifas;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores + JSON (enums como texto) + formato único de errores de validación
builder.Services.AddControllers(o => o.Filters.Add<PersistenciaFilter>()) // guarda los datos tras cada operación
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errores = ctx.ModelState
                .Where(kv => kv.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor inválido." : e.ErrorMessage)
                        .ToArray());
            return new BadRequestObjectResult(new ErrorResponse("VALIDACION", "Los datos enviados no son válidos.", errores));
        };
    });

// 2. Swagger con botón "Authorize" para probar con el token
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rentabici API", Version = "v1", Description = "MVP RentaBici – Sistema de alquiler de bicicletas – Equipo 4" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        In = ParameterLocation.Header,
        Description = "Pegue el token obtenido en POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// 3. CORS: permite que el frontend (Live Server, file://, otro puerto) consuma la API
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// 4. Configuración de reglas de negocio
builder.Services.Configure<TarifaOptions>(builder.Configuration.GetSection("Tarifas"));
builder.Services.Configure<ReservaOptions>(builder.Configuration.GetSection("Reservas"));

// 5. Inyección de dependencias (DIP: los controladores dependen de interfaces)
builder.Services.AddSingleton<IPersistenciaDatos, ArchivoJsonPersistencia>(); // archivo JSON (SQL Server proyectado)
builder.Services.AddSingleton<MemoryDatabase>();
builder.Services.AddSingleton<ISessionService, SessionService>();

builder.Services.AddSingleton<TarifaEstandarStrategy>();                 // Strategy
builder.Services.AddSingleton<TarifaConRecargoStrategy>();
builder.Services.AddSingleton<ITarifaSelector, TarifaSelector>();

builder.Services.AddSingleton<ISistemaRecaudo, RecaudoSimulado>();
builder.Services.AddScoped<IPaymentFacade, PaymentFacade>();             // Facade
builder.Services.AddScoped<INotificacionService, NotificacionService>(); // Factory Method

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEstacionService, EstacionService>();
builder.Services.AddScoped<IBicicletaService, BicicletaService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<RentalManagerService>();                       // sujeto real
builder.Services.AddScoped<IRentalService, RentalServiceProxy>();         // Proxy
builder.Services.AddScoped<IAlquilerConsultaService, AlquilerConsultaService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IncidenciaService>();                          // una clase,
builder.Services.AddScoped<IReportadorIncidencias>(sp => sp.GetRequiredService<IncidenciaService>()); // dos interfaces
builder.Services.AddScoped<IGestorMantenimiento>(sp => sp.GetRequiredService<IncidenciaService>());   // (ISP)

builder.Services.AddHostedService<ReservaExpirationService>();            // RN-02

var app = builder.Build();

app.UseCors("Frontend");
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 6. Frontend: el backend sirve la carpeta Front/ en http://localhost:5181 (index.html por defecto).
//    Así frontend y API comparten origen y no se depende de Live Server.
var rutaFront = Path.Combine(app.Environment.ContentRootPath, "Front");
if (Directory.Exists(rutaFront))
{
    var archivosFront = new PhysicalFileProvider(rutaFront);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = archivosFront });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = archivosFront });
}

app.MapControllers();

app.Run();
