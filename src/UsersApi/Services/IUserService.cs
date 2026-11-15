using UsersApi.Dtos;

namespace UsersApi.Services;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetPagedAsync(UserFilter filter, CancellationToken cancellationToken);
    Task<UserResponse> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
