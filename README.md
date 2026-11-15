# dotnet-users-api-initial

Ponto de partida da oficina **C# e .NET na prática** (Tech Week · Facisa): uma API de usuários em **.NET 10 + PostgreSQL**,
versão enxuta do [Orion API](https://github.com/vanderlan/Orion-Api).

O projeto **sobe e responde**, mas ainda não tem **migrations**, **filter de tempo**, **Unit of Work** nem **autenticação (JWT)**.
Esses quatro itens são os exercícios da oficina (`EX01` a `EX04`, abaixo). O resultado completo está em
**[dotnet-users-api-final](https://github.com/vanderlan/dotnet-users-api-final)**.

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` → `10.x`)
- Docker + Docker Compose
- Um editor: VS Code (extensão C# Dev Kit), Visual Studio ou Rider

## Instalando a ferramenta de migrations (dotnet-ef)

```bash
dotnet tool install --global dotnet-ef --version 10.*
```

Se já tiver uma versão antiga instalada, atualize:

```bash
dotnet tool update --global dotnet-ef --version 10.*
```

Confira (deve mostrar `10.x`):

```bash
dotnet ef --version
```

## Como rodar

```bash
git clone https://github.com/vanderlan/dotnet-users-api-initial.git
cd dotnet-users-api-initial

# 1. sobe PostgreSQL (5432) e pgAdmin (5050)
docker compose up -d

# 2. roda a API
dotnet run --project src/UsersApi --launch-profile http
```

- API: http://localhost:5042
- Documentação interativa (Scalar): http://localhost:5042/scalar/v1
- pgAdmin: http://localhost:5050 (servidor **Users DB** já cadastrado; senha do banco: `postgres`)
- Requisições prontas: [`src/UsersApi/UsersApi.http`](src/UsersApi/UsersApi.http)

> Enquanto a migration não existir (**EX01**), os endpoints que tocam o banco respondem `500`. É esperado.

## Endpoints (estado inicial)

| Verbo    | Rota              | Sucesso        | Erros possíveis |
|----------|-------------------|----------------|-----------------|
| `GET`    | `/api/users`      | `200`          | —               |
| `GET`    | `/api/users/{id}` | `200`          | 404             |
| `POST`   | `/api/users`      | `201` Location | 400, 409        |
| `PUT`    | `/api/users/{id}` | `204`          | 400, 404, 409   |
| `DELETE` | `/api/users/{id}` | `204`          | 404             |

## Estrutura

```
src/UsersApi
├── Controllers/        # rotas, verbos e status codes (só HTTP)
├── Dtos/               # contrato público: requests, responses, filtro
├── Filters/            # AutoValidationActionFilter (FluentValidation)
├── ExceptionHandling/  # DomainExceptionHandler → Problem Details (RFC 9457)
├── Services/           # regras de negócio (UserService)
├── Repositories/       # IUserRepository + UserRepository (acesso a dados)
├── Domain/             # entidades e exceções de negócio
├── Data/               # AppDbContext, mapping, interceptor
├── Security/           # hash de senha PBKDF2
├── Validators/         # regras FluentValidation
└── Program.cs          # serviços (DI) + pipeline HTTP
```

Fluxo de uma requisição: `Controller → Service → Repository → AppDbContext → PostgreSQL`.

---

# Roteiro de exercícios

No repositório **final**, todo trecho de código de cada exercício tem um comentário no formato:

```csharp
// EX02 - Adicionando filters - Aqui é a classe do filter
```

Para achar tudo de um exercício, pesquise por `EX02` (Ctrl+Shift+F no VS Code/Visual Studio) ou rode, na pasta do repo final:

```bash
git grep -n "EX02"
```

| Exercício | Tema | Etapa da oficina |
|-----------|------|------------------|
| **EX01** | Criando as migrations | 8 · EF Core e Migrations |
| **EX02** | Adicionando filters (`TimingFilter`) | 2 · Program.cs e filters |
| **EX03** | Implementando o Unit of Work | 5 · Repository e Unit of Work |
| **EX04** | Autenticação com JWT | 10 · JWT e autenticação |

> Faça o **EX01 primeiro**: sem as tabelas no banco, nenhum endpoint que grava ou lê usuários funciona.
> O **EX04** usa o Unit of Work, então faça o **EX03** antes.

---

## EX01 · Criando as migrations

Gera o esquema do banco a partir do modelo C# (`User` + `UserMapping`) e o aplica no PostgreSQL.

1. Suba o banco: `docker compose up -d`
2. Instale o `dotnet-ef` (veja acima) e gere a migration:

```bash
dotnet ef migrations add InitialCreate --project src/UsersApi --output-dir Data/Migrations
```

3. Abra `Data/Migrations/..._InitialCreate.cs` e veja os métodos `Up` (cria) e `Down` (desfaz).
4. Aplique no banco:

```bash
dotnet ef database update --project src/UsersApi
```

5. No pgAdmin (http://localhost:5050) confira as tabelas `Users` e `__EFMigrationsHistory`.
6. Teste: `POST /api/users` agora responde `201`.

Para recomeçar do zero: `docker compose down -v` (apaga o banco) e apague a pasta `Data/Migrations`.

---

## EX02 · Adicionando filters

Um *action filter* roda antes e depois de cada action. Vamos medir o tempo de cada chamada.

1. Crie `src/UsersApi/Filters/TimingFilter.cs`:

```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UsersApi.Filters;

public class TimingFilter(ILogger<TimingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var timer = Stopwatch.StartNew();

        await next();                       // executa a action

        logger.LogInformation("{Path} levou {Ms} ms",
            context.HttpContext.Request.Path, timer.ElapsedMilliseconds);
    }
}
```

2. Registre em `Program.cs`, junto do outro filter:

```csharp
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<AutoValidationActionFilter>();
        options.Filters.Add<TimingFilter>();
    })
```

3. Chame qualquer endpoint e veja no terminal: `/api/users levou N ms`.

---

## EX03 · Implementando o Unit of Work

Hoje cada método do repositório grava sozinho (`SaveChangesAsync`). Com o Unit of Work, os repositórios só
marcam as mudanças e **um único `CommitAsync`** grava tudo (tudo ou nada).

1. Crie `Repositories/IUnitOfWork.cs` e `Repositories/UnitOfWork.cs`:

```csharp
public interface IUnitOfWork
{
    IUserRepository Users { get; }

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IUserRepository? _users;

    public IUserRepository Users => _users ??= new UserRepository(context);

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
```

2. `IUserRepository`: remova `UpdateAsync` e troque `DeleteAsync` por `void Remove(User user);`.
3. `UserRepository`: tire os `SaveChangesAsync`. O `AddAsync` só adiciona (`await context.Users.AddAsync(user, cancellationToken); return user;`) e o `Remove` fica `context.Users.Remove(user)`. Remova o `UpdateAsync`.
4. `Program.cs`: troque `AddScoped<IUserRepository, UserRepository>()` por:

```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

5. `UserService`: injete `IUnitOfWork unitOfWork` no lugar do repositório, troque `repository.` por `unitOfWork.Users.` e chame `await unitOfWork.CommitAsync(cancellationToken);` depois do `AddAsync`, depois de alterar os campos no `UpdateAsync` e depois do `Remove` no `DeleteAsync`.
6. Teste o CRUD completo (criar, buscar, atualizar, remover).

---

## EX04 · Autenticação com JWT

Objetivo: `POST /api/auth/login` devolve um token; `GET /api/users` passa a exigir um token de **Admin**.

**1. Pacote**

```bash
dotnet add src/UsersApi package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.14
```

**2. Configuração** em `appsettings.Development.json` (chave com 64+ caracteres; só para desenvolvimento):

```json
"JwtOptions": {
  "SymmetricSecurityKey": "SOMENTE-PARA-DESENVOLVIMENTO-troque-em-producao-chave-com-pelo-menos-64-caracteres!!",
  "Issuer": "UsersApi",
  "Audience": "UsersApi.Clients",
  "TokenExpirationMinutes": 60
},
```

**3. `Options/JwtOptions.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace UsersApi.Options;

public sealed class JwtOptions
{
    public const string SectionName = "JwtOptions";

    [Required, MinLength(64)]
    public string SymmetricSecurityKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int TokenExpirationMinutes { get; set; } = 60;
}
```

**4. `Dtos/AuthDtos.cs`**

```csharp
namespace UsersApi.Dtos;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime Expiration);
```

**5. `Security/JwtTokenService.cs`**

```csharp
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UsersApi.Domain.Entities;
using UsersApi.Dtos;
using UsersApi.Options;

namespace UsersApi.Security;

public interface ITokenService
{
    LoginResponse CreateAccessToken(User user);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public LoginResponse CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.TokenExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(CreateKey(_options), SecurityAlgorithms.HmacSha512),
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Sid, user.PublicId),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Profile.ToString())
            ])
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new LoginResponse(token, expires);
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions options) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = CreateKey(options),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha512],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role
    };

    private static SymmetricSecurityKey CreateKey(JwtOptions options) =>
        new(Encoding.UTF8.GetBytes(options.SymmetricSecurityKey));
}
```

**6. Exceção de login inválido**: em `Domain/Exceptions/BusinessException.cs` adicione

```csharp
public class UnauthorizedUserException(string message)
    : BusinessException(message, "Não autenticado");
```

e em `DomainExceptionHandler`, no `switch`, antes de `BusinessException`:

```csharp
UnauthorizedUserException => StatusCodes.Status401Unauthorized,
```

**7. `Services/AuthService.cs`**

```csharp
using UsersApi.Domain.Exceptions;
using UsersApi.Dtos;
using UsersApi.Repositories;
using UsersApi.Security;

namespace UsersApi.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public sealed class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

        // Mesma mensagem para "e-mail não existe" e "senha errada".
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new UnauthorizedUserException("E-mail ou senha inválidos.");

        return tokenService.CreateAccessToken(user);
    }
}
```

**8. `Controllers/AuthController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersApi.Dtos;
using UsersApi.Services;

namespace UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }
}
```

**9. Validator do login**: em `Validators/Validators.cs` adicione

```csharp
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("O e-mail é obrigatório.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
```

**10. `Program.cs`**: adicione os `using Microsoft.AspNetCore.Authentication.JwtBearer;`, `using Microsoft.Extensions.Options;` e `using UsersApi.Options;`, registre os serviços (junto dos outros `AddScoped`/`AddSingleton`):

```csharp
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
        bearer.TokenValidationParameters =
            JwtTokenService.CreateValidationParameters(jwt.Value));
builder.Services.AddAuthorization();
```

e, no pipeline, **antes** de `app.MapControllers();`:

```csharp
app.UseAuthentication();     // QUEM é você?
app.UseAuthorization();      // O QUE você pode fazer?
```

**11. `UsersController`**: adicione `using Microsoft.AspNetCore.Authorization;` e `using UsersApi.Domain.Entities;`, coloque `[Authorize]` na classe, `[Authorize(Roles = nameof(UserProfile.Admin))]` em `GetAll`, `GetById`, `Update` e `Delete`, e `[AllowAnonymous]` no `Create` (para conseguir cadastrar o primeiro usuário).

> Ponto de discussão: com o `POST /api/users` anônimo e o `Profile` vindo do cliente, qualquer pessoa cria um Admin. Em um sistema real, o primeiro administrador viria de um *seed* e o perfil nunca seria escolhido pelo cliente.

**12. Teste**

1. `POST /api/users` (admin e operator) → `201`
2. `POST /api/auth/login` → copie o `token`
3. `GET /api/users` sem token → `401`; com token de Admin → `200`; com token de Operator → `403`
4. Altere um caractere do token → `401`

---

# Roteiro da oficina (120 min)

| Min | Etapa | Conteúdo |
|-----|-------|----------|
| 10  | 0. História do .NET | slides |
| 8   | 1. Introdução | `dotnet new`, `.csproj`, `docker compose up -d` |
| 12  | 2. Program.cs, pipeline e filters | ler `Program.cs` · **EX02** |
| 8   | 3. Controllers e rotas | ler `UsersController` |
| 7   | 4. Verbos HTTP e status codes | chamar os endpoints pelo `.http`/Scalar |
| 10  | 5. Repository e Unit of Work | `IUserRepository`/`UserRepository` · **EX03** |
| 8   | 6. DTOs e model binding | `Dtos/` |
| 8   | 7. Services e injeção de dependência | `UserService` e os `AddScoped` |
| 12  | 8. EF Core e migrations | `Data/` · **EX01** |
| 9   | 9. Validação e `DomainExceptionHandler` | provocar 400, 404 e 409 |
| 13  | 10. JWT e autenticação | **EX04** |
| 15  | Opcional: Orion API | CQRS e MediatR |

## Desafios para depois da oficina

1. Refresh token com rotação (veja `LoginWithRefreshToken` no Orion).
2. Seed do administrador inicial.
3. Soft delete (`DeletedAt`) com *query filter* global do EF Core.
4. Testes de integração com Testcontainers.
5. Trocar os services por **CQRS + MediatR**, como no Orion.
