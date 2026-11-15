using UsersApi.Domain.Entities;
using UsersApi.Dtos;

namespace UsersApi.Repositories;

/// <summary>
/// Repository: esconde o acesso a dados atrás de uma interface.
/// O service pede "um usuário por e-mail" e não sabe se por trás há PostgreSQL, SQL Server ou outro banco.
/// </summary>
public interface IUserRepository
{
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PagedResult<User>> ListPagedAsync(UserFilter filter, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
