# dotnet-users-api-initial

Ponto de partida da oficina **C# e .NET na prática** (Tech Week · Facisa): uma API de usuários em **.NET 10 + PostgreSQL**,
versão enxuta do [Orion API](https://github.com/vanderlan/Orion-Api).

O projeto **sobe e responde**, mas ainda não tem **migrations**, **autenticação (JWT)** nem as evoluções pequenas
(filtro por perfil, filter de tempo, campo `Phone` e `GET /api/users/me`). Tudo isso são os exercícios da oficina
(`EX01` a `EX06`, abaixo). O resultado completo está em
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

# 2. roda a API (abre o Scalar no navegador)
dotnet run --project src/UsersApi --launch-profile Dev
```

- API: http://localhost:5042
- Documentação interativa (Scalar): http://localhost:5042/scalar/v1
- pgAdmin: http://localhost:5050 (servidor **Users DB** já cadastrado; senha do banco: `postgres`)
- Requisições prontas: [`src/UsersApi/UsersApi.http`](src/UsersApi/UsersApi.http)

> Enquanto a migration não existir (**EX01**), os endpoints que tocam o banco respondem `500`. É esperado.
>
> Depois de alterar o código em cada exercício, pare a API (Ctrl+C) e rode de novo.

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
// EX03 - Adicionando filters - Aqui é a classe do filter
```

Para achar tudo de um exercício, abra o repositório final no VS Code e pesquise por `EX03` (Ctrl+Shift+F).

| Exercício | Tema | Etapas | Etapa da oficina |
|-----------|------|--------|------------------|
| **EX01** | Criando as migrations | 4 | 2 · EF Core: migrations e consultas |
| **EX02** | Filtro por perfil na listagem | 2 | 2 · EF Core: migrations e consultas |
| **EX03** | Adicionando filters (`TimingFilter`) | 2 | 6 · Pipeline e filters |
| **EX04** | Novo campo no usuário (segunda migration) | 6 | 7 · Validação e erros |
| **EX05** | Autenticação com JWT | 12 | 8 · JWT e autenticação |
| **EX06** | Endpoint `GET /api/users/me` | 2 | 8 · JWT e autenticação |

> Faça os exercícios **na ordem**, do mais simples para o mais complexo. O EX01 vem primeiro: sem as tabelas no banco, nenhum endpoint que grava ou lê usuários funciona.
> O EX02 e o EX04 também precisam do banco. O EX05 fecha a API (a partir dele, listar, buscar, atualizar e remover exigem token) e o EX06 usa esse token.

**Como ler os trechos de código**

- Arquivo novo: o bloco traz o arquivo inteiro.
- Arquivo existente: o bloco `diff` mostra só o que muda. Linhas com `-` saem, linhas com `+` entram e as linhas sem sinal são o contexto, para você achar o lugar. Cada `@@` separa um trecho do outro.
- Os caminhos são relativos a `src/UsersApi` (por exemplo, `Program.cs` é `src/UsersApi/Program.cs`).
- Cada exercício começa com o **resumo das etapas**, na ordem em que você as faz. Os títulos abaixo repetem o resumo.

---

## EX01 · Criando as migrations

Gera o esquema do banco a partir do modelo C# (`User` + `UserMapping`) e o aplica no PostgreSQL.

**Resumo (4 etapas)**

* 1 - Subir o banco
* 2 - Compilar o projeto uma vez
* 3 - Gerar a migration
* 4 - Aplicar a migration no banco

### Etapa 1 · Subir o banco

```bash
docker compose up -d
```

### Etapa 2 · Compilar o projeto uma vez

Isso restaura os pacotes de que o `dotnet ef` precisa (se você já rodou a API, já está feito):

```bash
dotnet build
```

### Etapa 3 · Gerar a migration

```bash
dotnet ef migrations add InitialCreate --project src/UsersApi --output-dir Data/Migrations
```

Abra `Data/Migrations/..._InitialCreate.cs` e veja os métodos `Up` (cria) e `Down` (desfaz).

### Etapa 4 · Aplicar a migration no banco

```bash
dotnet ef database update --project src/UsersApi
```

### Como testar

No pgAdmin (http://localhost:5050) confira as tabelas `Users` e `__EFMigrationsHistory`. No `UsersApi.http`, a requisição **Criar usuário** agora responde `201`.

Para recomeçar do zero: `docker compose down -v` (apaga o banco) e apague a pasta `Data/Migrations`.

---

## EX02 · Filtro por perfil na listagem

Objetivo: `GET /api/users?profile=Admin` devolve só os administradores (e `?profile=Operator`, só os operadores).

**Resumo (2 etapas)**

* 1 - Adicionar o novo campo no filtro (`UserFilter`)
* 2 - Aplicar o filtro na cláusula `Where` do repository

### Etapa 1 · Adicionar o novo campo no filtro

Em `Dtos/UserDtos.cs`, o filtro ganha o parâmetro `Profile`:

```diff
     public string? Query { get; init; }
+    public UserProfile? Profile { get; init; }
 }
```

### Etapa 2 · Aplicar o filtro na cláusula Where do repository

Em `Repositories/UserRepository.cs`, no `ListPagedAsync`, logo depois do filtro por nome:

```diff
             query = query.Where(x => EF.Functions.ILike(x.Name, $"%{filter.Query}%"));
 
+        if (filter.Profile is not null)
+            query = query.Where(x => x.Profile == filter.Profile.Value);
+
         var count = await query.CountAsync(cancellationToken);
```

O controller **não muda**: o `[FromQuery] UserFilter filter` já recebe o `?profile=` (o *model binding* converte o texto `Admin` no enum `UserProfile`).

### Como testar

Reinicie a API, crie um usuário `Admin` e outro `Operator` (a requisição **Criar usuário** do `UsersApi.http`, trocando o `profile`) e adicione ao `UsersApi.http`:

```http
### Listar só os Admins
GET {{host}}/api/users?profile=Admin

### Perfil inexistente -> 400
GET {{host}}/api/users?profile=Master
```

- `?profile=Admin` devolve só Admins e `?profile=Operator`, só Operators.
- `?profile=Master` responde `400`: o model binding recusa o valor antes de chegar ao controller.
- No terminal, o log mostra o SQL gerado pelo EF Core com o filtro.

---

## EX03 · Adicionando filters

Um *action filter* roda antes e depois de cada action. Vamos medir o tempo de cada chamada.

**Resumo (2 etapas)**

* 1 - Criar o filter (`TimingFilter`)
* 2 - Registrar o filter no `Program.cs`

### Etapa 1 · Criar o filter

Crie `Filters/TimingFilter.cs`:

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

### Etapa 2 · Registrar o filter no Program.cs

Em `Program.cs`, registre o filter junto do outro:

```diff
 builder.Services.AddControllers(options =>
-        options.Filters.Add<AutoValidationActionFilter>())
+    {
+        options.Filters.Add<AutoValidationActionFilter>();
+        options.Filters.Add<TimingFilter>();
+    })
     // Enums trafegam como texto ("Admin") em vez de número.
```

### Como testar

Rode a API, chame qualquer endpoint e veja no terminal: `/api/users levou N ms`.

---

## EX04 · Novo campo no usuário (segunda migration)

Objetivo: guardar o telefone (opcional) do usuário. O banco evolui com uma **nova migration**, sem apagar nada.

**Resumo (6 etapas)**

* 1 - Adicionar o campo `Phone` na entidade `User`
* 2 - Mapear a coluna no `UserMapping`
* 3 - Adicionar o campo nos DTOs (criar, atualizar e resposta)
* 4 - Gravar o telefone no `UserService`
* 5 - Validar o tamanho do telefone
* 6 - Gerar e aplicar a segunda migration

### Etapa 1 · Adicionar o campo na entidade

Em `Domain/Entities/User.cs`, o novo campo (o `?` permite `null`):

```diff
     public required UserProfile Profile { get; set; }
 
+    public string? Phone { get; set; }
+
     /// <summary>Hash da senha (PBKDF2). Nunca a senha em texto puro.</summary>
```

### Etapa 2 · Mapear a coluna

Em `Data/Mappings/UserMapping.cs`, a coluna (tamanho máximo 20, opcional):

```diff
         builder.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
+        builder.Property(x => x.Phone).HasMaxLength(20);
```

### Etapa 3 · Adicionar o campo nos DTOs

Em `Dtos/UserDtos.cs`, o campo entra nos DTOs de criar, atualizar e na resposta:

```diff
-public record CreateUserRequest(string Name, string Email, string Password, UserProfile Profile);
+public record CreateUserRequest(
+    string Name, string Email, string Password, UserProfile Profile, string? Phone = null);
 
-public record UpdateUserRequest(string Name, string Email, UserProfile Profile);
+public record UpdateUserRequest(
+    string Name, string Email, UserProfile Profile, string? Phone = null);
@@
     string Email,
+    string? Phone,
     UserProfile Profile,
     DateTime CreatedAt,
     DateTime LastUpdated)
 {
     public static UserResponse From(User user) =>
-        new(user.PublicId, user.Name, user.Email, user.Profile, user.CreatedAt, user.LastUpdated);
+        new(user.PublicId, user.Name, user.Email, user.Phone,
+            user.Profile, user.CreatedAt, user.LastUpdated);
 }
```

### Etapa 4 · Gravar o telefone no service

Em `Services/UserService.cs`, grave o telefone ao criar e ao atualizar:

```diff
             Profile = request.Profile,
+            Phone = request.Phone?.Trim(),
             PasswordHash = passwordHasher.Hash(request.Password)
@@
         user.Profile = request.Profile;
+        user.Phone = request.Phone?.Trim();
```

### Etapa 5 · Validar o tamanho do telefone

Em `Validators/Validators.cs`, valide o tamanho (a coluna aceita até 20 caracteres), no validator de criar e no de atualizar:

```diff
         RuleFor(x => x.Password).NotEmpty().WithMessage("A senha é obrigatória.")
             .MinimumLength(6).WithMessage("A senha deve ter pelo menos 6 caracteres.");
 
         RuleFor(x => x.Profile).IsInEnum().WithMessage("Perfil inválido.");
+
+        RuleFor(x => x.Phone).MaximumLength(20)
+            .WithMessage("O telefone deve ter no máximo 20 caracteres.");
     }
 }
@@
             .MaximumLength(200);
 
         RuleFor(x => x.Profile).IsInEnum().WithMessage("Perfil inválido.");
+
+        RuleFor(x => x.Phone).MaximumLength(20)
+            .WithMessage("O telefone deve ter no máximo 20 caracteres.");
     }
 }
```

### Etapa 6 · Gerar e aplicar a segunda migration

Não edite a primeira migration: gere uma nova.

```bash
dotnet ef migrations add AddUserPhone --project src/UsersApi --output-dir Data/Migrations
dotnet ef database update --project src/UsersApi
```

Abra a nova migration: o `Up` faz um `AddColumn` e o `Down`, um `DropColumn`. No pgAdmin, a tabela `Users` ganhou a coluna `Phone` e os usuários que já existiam continuam lá (com `Phone` vazio).

### Como testar

Reinicie a API e adicione ao `UsersApi.http`:

```http
### Criar usuário com telefone
POST {{host}}/api/users
Content-Type: application/json

{
  "name": "Maria Silva",
  "email": "maria@exemplo.com",
  "password": "Senha@123",
  "profile": "Operator",
  "phone": "(83) 99999-0000"
}
```

- A resposta traz `"phone": "(83) 99999-0000"`; sem enviar o campo, `"phone": null`.
- Telefone com mais de 20 caracteres → `400`.
- O `PUT` substitui o usuário: se o JSON não trouxer `phone`, o telefone volta a ser `null`.

---

## EX05 · Autenticação com JWT

Objetivo: `POST /api/auth/login` devolve um token; `GET /api/users` e as demais rotas (menos o cadastro) passam a exigir um token de **Admin**.

**Resumo (12 etapas)**

* 1 - Instalar o pacote `JwtBearer`
* 2 - Configurar o JWT no `appsettings.Development.json`
* 3 - Criar as opções do JWT (`JwtOptions`)
* 4 - Criar os DTOs do login
* 5 - Criar o gerador de token (`JwtTokenService`)
* 6 - Criar a exceção de login inválido e mapeá-la para `401`
* 7 - Criar o serviço de login (`AuthService`)
* 8 - Criar o `AuthController`
* 9 - Criar o validator do login
* 10 - Registrar serviços, opções e autenticação no `Program.cs`
* 11 - Ligar autenticação e autorização no pipeline
* 12 - Proteger o `UsersController`

### Etapa 1 · Instalar o pacote

```bash
dotnet add src/UsersApi package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.14
```

### Etapa 2 · Configurar o JWT

Em `appsettings.Development.json`, adicione a seção (chave com 64+ caracteres; só para desenvolvimento):

```diff
+  "JwtOptions": {
+    "SymmetricSecurityKey": "SOMENTE-PARA-DESENVOLVIMENTO-troque-em-producao-chave-com-pelo-menos-64-caracteres!!",
+    "Issuer": "UsersApi",
+    "Audience": "UsersApi.Clients",
+    "TokenExpirationMinutes": 60
+  },
   "ConnectionStrings": {
```

### Etapa 3 · Criar as opções do JWT

Crie `Options/JwtOptions.cs`:

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

### Etapa 4 · Criar os DTOs do login

Crie `Dtos/AuthDtos.cs`:

```csharp
namespace UsersApi.Dtos;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime Expiration);
```

### Etapa 5 · Criar o gerador de token

Crie `Security/JwtTokenService.cs`:

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

### Etapa 6 · Criar a exceção de login inválido e mapeá-la para 401

Em `Domain/Exceptions/BusinessException.cs`, adicione **ao final** do arquivo a exceção de login inválido:

```csharp

public class UnauthorizedUserException(string message)
    : BusinessException(message, "Não autenticado");
```

Em `ExceptionHandling/DomainExceptionHandler.cs`, o `switch` passa a devolver `401` para essa exceção:

```diff
             ConflictException => StatusCodes.Status409Conflict,
+            UnauthorizedUserException => StatusCodes.Status401Unauthorized,
             BusinessException => StatusCodes.Status400BadRequest,
```

### Etapa 7 · Criar o serviço de login

Crie `Services/AuthService.cs`:

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
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

        // Mesma mensagem para "e-mail não existe" e "senha errada".
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new UnauthorizedUserException("E-mail ou senha inválidos.");

        return tokenService.CreateAccessToken(user);
    }
}
```

### Etapa 8 · Criar o AuthController

Crie `Controllers/AuthController.cs`:

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

### Etapa 9 · Criar o validator do login

Em `Validators/Validators.cs`, adicione **ao final** do arquivo o validator do login:

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

### Etapa 10 · Registrar serviços, opções e autenticação

Em `Program.cs`, os `using` novos:

```diff
 using FluentValidation;
+using Microsoft.AspNetCore.Authentication.JwtBearer;
 using Microsoft.EntityFrameworkCore;
+using Microsoft.Extensions.Options;
 using Scalar.AspNetCore;
@@
 using UsersApi.Filters;
+using UsersApi.Options;
 using UsersApi.Repositories;
```

Ainda em `Program.cs`, registre o login, o gerador de token, as opções do JWT e a autenticação:

```diff
 builder.Services.AddScoped<IUserService, UserService>();
 builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
+builder.Services.AddScoped<IAuthService, AuthService>();
+builder.Services.AddSingleton<ITokenService, JwtTokenService>();
+
+builder.Services.AddOptions<JwtOptions>()
+    .BindConfiguration(JwtOptions.SectionName)
+    .ValidateDataAnnotations()
+    .ValidateOnStart();
+
+builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
+    .AddJwtBearer();
+builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
+    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
+        bearer.TokenValidationParameters =
+            JwtTokenService.CreateValidationParameters(jwt.Value));
+builder.Services.AddAuthorization();
 
 // Documentação: OpenAPI + Scalar
```

### Etapa 11 · Ligar autenticação e autorização no pipeline

Em `Program.cs`, no pipeline, **antes** do `MapControllers`:

```diff
+app.UseAuthentication();     // QUEM é você?  (lê e valida o JWT)
+app.UseAuthorization();      // O QUE você pode fazer?  ([Authorize])
+
 app.MapControllers();
```

### Etapa 12 · Proteger o UsersController

Em `Controllers/UsersController.cs`, os `using` novos e o `[Authorize]` na classe:

```diff
+using Microsoft.AspNetCore.Authorization;
 using Microsoft.AspNetCore.Mvc;
+using UsersApi.Domain.Entities;
 using UsersApi.Dtos;
@@
 [Route("api/[controller]")]
+[Authorize]
 public class UsersController(IUserService userService) : ControllerBase
```

Ainda em `Controllers/UsersController.cs`, exija o perfil **Admin** nas ações de listar, buscar, atualizar e remover, e libere o cadastro (assim dá para criar o primeiro usuário):

```diff
     [HttpGet]
+    [Authorize(Roles = nameof(UserProfile.Admin))]
@@
     [HttpGet("{id}")]
+    [Authorize(Roles = nameof(UserProfile.Admin))]
@@
     [HttpPost]
+    [AllowAnonymous]
@@
     [HttpPut("{id}")]
+    [Authorize(Roles = nameof(UserProfile.Admin))]
@@
     [HttpDelete("{id}")]
+    [Authorize(Roles = nameof(UserProfile.Admin))]
```

### Como testar

No `UsersApi.http`, adicione no topo `@token = COLE_O_TOKEN_AQUI`, o login abaixo e, nas requisições de listar, listar só os Admins, buscar, atualizar e remover (inclusive as dos exercícios anteriores), a linha `Authorization: Bearer {{token}}` logo abaixo da primeira linha:

```http
### Login do admin -> devolve o token (copie para @token)
POST {{host}}/api/auth/login
Content-Type: application/json

{
  "email": "admin@exemplo.com",
  "password": "Admin@123"
}
```

Antes, crie o administrador (o cadastro é aberto):

```http
### Criar usuário admin
POST {{host}}/api/users
Content-Type: application/json

{
  "name": "Admin",
  "email": "admin@exemplo.com",
  "password": "Admin@123",
  "profile": "Admin"
}
```

- `GET /api/users` sem token → `401`; com o token do Admin → `200`.
- Crie um usuário `Operator`, entre com ele e liste → `403`.
- Altere um caractere do token → `401`.

> Ponto de discussão: com o `POST /api/users` anônimo e o `Profile` vindo do cliente, qualquer pessoa cria um Admin. Em um sistema real, o primeiro administrador viria de um *seed* e o perfil nunca seria escolhido pelo cliente.

---

## EX06 · Endpoint `GET /api/users/me`

Objetivo: o usuário logado consulta os próprios dados, sem informar id. O id vem do token.

**Resumo (2 etapas)**

* 1 - Importar o namespace de claims no `UsersController`
* 2 - Criar a action `GetMe`, sem exigir perfil

### Etapa 1 · Importar o namespace de claims

Em `Controllers/UsersController.cs`, o `using` novo:

```diff
+using System.Security.Claims;
 using Microsoft.AspNetCore.Authorization;
```

### Etapa 2 · Criar a action GetMe

Ainda em `Controllers/UsersController.cs`, a nova action, logo depois do `GetById`. Ela **não** tem `Roles`: qualquer perfil logado pode chamar.

```diff
         return Ok(await userService.GetByIdAsync(id, cancellationToken));
     }
 
+    /// <summary>Devolve os dados do usuário autenticado (o id vem da claim do token).</summary>
+    [HttpGet("me")]
+    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
+    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
+    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
+    {
+        var id = User.FindFirstValue(ClaimTypes.Sid)!;
+
+        return Ok(await userService.GetByIdAsync(id, cancellationToken));
+    }
+
     /// <summary>Cria um usuário.</summary>
```

### Como testar

Reinicie a API e adicione ao `UsersApi.http` (cole em `@tokenOperator` o token de um usuário `Operator`):

```http
@tokenOperator = COLE_O_TOKEN_DO_OPERATOR_AQUI

### Meus dados (qualquer perfil logado)
GET {{host}}/api/users/me
Authorization: Bearer {{tokenOperator}}

### O mesmo Operator não pode listar -> 403
GET {{host}}/api/users
Authorization: Bearer {{tokenOperator}}
```

- `GET /api/users/me` com o token do Operator → `200` com os dados dele (inclusive `phone`).
- `GET /api/users` com o mesmo token → `403`.
- Sem token → `401`.

> Por que `me` não conflita com `{id}`? Quando uma rota tem um trecho fixo (`me`) e outra tem parâmetro (`{id}`), o ASP.NET Core escolhe a rota com o trecho fixo.

---

# Roteiro da oficina (120 min)

| Min | Etapa | Conteúdo |
|-----|-------|----------|
| 10  | 0. História do .NET | slides |
| 8   | 1. Introdução | `dotnet new`, `.csproj`, `docker compose up -d` |
| 17  | 2. EF Core: migrations e consultas | `Data/` · **EX01** · **EX02** |
| 15  | 3. Controllers, rotas, verbos e status codes | ler `UsersController`, chamar os endpoints pelo `.http`/Scalar |
| 8   | 4. DTOs e model binding | `Dtos/` |
| 16  | 5. Repository, injeção de dependência e Services | `IUserRepository`, `UserRepository`, `UserService` e os `AddScoped` |
| 12  | 6. Pipeline e filters | `Program.cs` · **EX03** |
| 16  | 7. Validação e `DomainExceptionHandler` | provocar 400, 404 e 409 · **EX04** |
| 18  | 8. JWT e autenticação | **EX05** · **EX06** |
|     | Opcional: Orion API | CQRS e MediatR, só se sobrar tempo |

## Desafios para depois da oficina

1. Trocar a própria senha (`PATCH /api/users/me/password`), usando a mesma claim do `/me`.
2. Refresh token com rotação (veja `LoginWithRefreshToken` no Orion).
3. Seed do administrador inicial.
4. Soft delete (`DeletedAt`) com *query filter* global do EF Core.
5. Testes de integração com Testcontainers.
6. Trocar os services por **CQRS + MediatR**, como no Orion.
