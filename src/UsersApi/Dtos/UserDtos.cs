using UsersApi.Domain.Entities;

namespace UsersApi.Dtos;

// DTOs = contrato público da API. A entidade User nunca sai (nem entra) direto pelo controller.

public record CreateUserRequest(string Name, string Email, string Password, UserProfile Profile);

public record UpdateUserRequest(string Name, string Email, UserProfile Profile);

public record UserResponse(
    string Id,
    string Name,
    string Email,
    UserProfile Profile,
    DateTime CreatedAt,
    DateTime LastUpdated)
{
    public static UserResponse From(User user) =>
        new(user.PublicId, user.Name, user.Email, user.Profile, user.CreatedAt, user.LastUpdated);
}

/// <summary>Filtro de listagem: vem da query string (?page=1&amp;quantity=10&amp;query=ana).</summary>
public class UserFilter
{
    public int Page { get; init; } = 1;
    public int Quantity { get; init; } = 10;
    public string? Query { get; init; }
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Count);
