using UsersApi.Domain.Entities;
using UsersApi.Domain.Exceptions;
using UsersApi.Dtos;
using UsersApi.Repositories;
using UsersApi.Security;

namespace UsersApi.Services;

// Injeção de dependência por construtor primário: o container entrega o repositório e o hasher.
public sealed class UserService(IUserRepository repository, IPasswordHasher passwordHasher) : IUserService
{
    public async Task<PagedResult<UserResponse>> GetPagedAsync(UserFilter filter, CancellationToken cancellationToken)
    {
        var users = await repository.ListPagedAsync(filter, cancellationToken);

        return new PagedResult<UserResponse>(users.Items.Select(UserResponse.From).ToList(), users.Count);
    }

    public async Task<UserResponse> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var user = await repository.GetByPublicIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(id);

        return UserResponse.From(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        await EnsureEmailIsAvailableAsync(email, ignoreId: null, cancellationToken);

        var user = await repository.AddAsync(new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Profile = request.Profile,
            PasswordHash = passwordHasher.Hash(request.Password)
        }, cancellationToken);

        return UserResponse.From(user);
    }

    public async Task UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByPublicIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(id);

        var email = NormalizeEmail(request.Email);

        await EnsureEmailIsAvailableAsync(email, ignoreId: id, cancellationToken);

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Profile = request.Profile;

        await repository.UpdateAsync(user, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var user = await repository.GetByPublicIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(id);

        await repository.DeleteAsync(user, cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task EnsureEmailIsAvailableAsync(string email, string? ignoreId, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByEmailAsync(email, cancellationToken);

        if (existing is not null && existing.PublicId != ignoreId)
            throw new ConflictException("Já existe um usuário com este e-mail.");
    }
}
