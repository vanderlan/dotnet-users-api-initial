using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using UsersApi.Data;
using UsersApi.Data.Interceptors;
using UsersApi.ExceptionHandling;
using UsersApi.Filters;
using UsersApi.Repositories;
using UsersApi.Security;
using UsersApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ───────────────────────────── 1. SERVIÇOS (container de DI) ─────────────────────────────

// Controllers + filtro global de validação (roda antes de toda action)
builder.Services.AddControllers(options =>
        options.Filters.Add<AutoValidationActionFilter>())
    // Enums trafegam como texto ("Admin") em vez de número.
    .AddJsonOptions(json => json.JsonSerializerOptions.Converters
        .Add(new JsonStringEnumConverter()));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Erros padronizados: Problem Details (RFC 9457) + nosso handler de exceções de domínio
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Entity Framework Core + PostgreSQL
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TimestampsInterceptor>();
builder.Services.AddDbContext<AppDbContext>((provider, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"));
    options.AddInterceptors(
        provider.GetRequiredService<TimestampsInterceptor>());
});

// Camadas da aplicação. Scoped = uma instância por requisição HTTP.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

// Documentação: OpenAPI + Scalar
builder.Services.AddOpenApi();

var app = builder.Build();

// ───────────────────────────── 2. PIPELINE HTTP (ordem importa!) ─────────────────────────────

app.UseExceptionHandler();   // 1º: captura qualquer exceção das etapas seguintes
app.UseStatusCodePages();    // 404 e outros status vazios viram Problem Details

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference();      // /scalar/v1
}

app.MapControllers();

await app.RunAsync();
