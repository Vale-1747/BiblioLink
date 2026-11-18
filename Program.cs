using Microsoft.EntityFrameworkCore;
using BiblioLink.Interfaces;
using BiblioLink.Integrations;
using BiblioLink.Services;
using BiblioLink.Middleware;
using BiblioLink.Models;
using BiblioLink.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers(); //registra los controladores
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresá el token JWT."
    });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API Key para consultar préstamos activos por matrícula."
    });
    options.OperationFilter<ApiKeyOperationFilter>();
});
builder.Services.AddDbContext<BibliotecaContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("BibliotecaDb")));

var jwtKey = builder.Configuration["Auth:Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Auth:Jwt:Key debe configurarse con al menos 32 bytes.");

var jwtIssuer = builder.Configuration["Auth:Jwt:Issuer"] ?? "BiblioLink";
var jwtAudience = builder.Configuration["Auth:Jwt:Audience"] ?? "BiblioLink";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<Cuenta>, PasswordHasher<Cuenta>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHostedService<CuentaSeederHostedService>();

//configuracion de prestamo dais y limite leida de appsettigs.json
builder.Services.Configure<PrestamosOptions>(builder.Configuration.GetSection(PrestamosOptions.Seccion));

//servicios
builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<IPrestamoService, PrestamoService>();
builder.Services.Configure<UniversidadApiOptions>(builder.Configuration.GetSection(UniversidadApiOptions.Seccion));
builder.Services.AddHttpClient<IUniversidadClient, UniversidadClient>();
builder.Services.AddScoped<IApiExternaService, ApiExternaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); //contecta rutas con controladores

app.Run();