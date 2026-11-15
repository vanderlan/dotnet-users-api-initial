namespace UsersApi.Domain.Entities;

public abstract class BaseEntity
{
    /// <summary>Chave interna do banco (nunca exposta na API).</summary>
    public long Id { get; set; }

    /// <summary>Identificador público (GUID) gerado ao persistir a entidade.</summary>
    public string PublicId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
}
