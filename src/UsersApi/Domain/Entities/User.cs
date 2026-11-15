namespace UsersApi.Domain.Entities;

public class User : BaseEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required UserProfile Profile { get; set; }

    /// <summary>Hash da senha (PBKDF2). Nunca a senha em texto puro.</summary>
    public string PasswordHash { get; set; } = string.Empty;
}
