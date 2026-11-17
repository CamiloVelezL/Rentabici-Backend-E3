var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. Configurar Swagger
builder.Services.AddSwaggerGen();

// 2. Inyección de dependencias
builder.Services.AddSingleton<RentabiciBackend.Data.MemoryDatabase>();
builder.Services.AddScoped<RentabiciBackend.Services.RentalManagerService>();
builder.Services.AddScoped<RentabiciBackend.Services.IRentalService, RentabiciBackend.Services.RentalServiceProxy>();

var app = builder.Build();

// 3. Activar Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();