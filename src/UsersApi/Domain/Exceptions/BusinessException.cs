namespace UsersApi.Domain.Exceptions;

/// <summary>
/// Erro de regra de negócio. A mensagem é feita para o cliente da API.
/// O DomainExceptionHandler converte cada tipo em um status HTTP.
/// </summary>
public class BusinessException(string message, string? title = null) : Exception(message)
{
    public string? Title { get; } = title;
}

public class NotFoundException(string id)
    : BusinessException($"Recurso não encontrado: {id}", "Não encontrado");

public class ConflictException(string message)
    : BusinessException(message, "Conflito");
