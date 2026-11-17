using Microsoft.EntityFrameworkCore;
using BiblioLink.Interfaces;
using BiblioLink.Services;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers(); //registra los controladores
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BibliotecaContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("BibliotecaDb")));

//configuracion de prestamo dais y limite leida de appsettigs.json
builder.Services.Configure<PrestamosOptions>(builder.Configuration.GetSection(PrestamosOptions.Seccion));

//servicios
builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<IPrestamoService, PrestamoService>();
builder.Services.Configure<UniversidadApiOptions>(builder.Configuration.GetSection(UniversidadApiOptions.Seccion));
builder.Services.AddHttpClient<IUniversidadClient, UniversidadClient>();
builder.Services.AddScoped<IApiExternaService, ApiExternaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers(); //contecta rutas con controladores

app.Run();