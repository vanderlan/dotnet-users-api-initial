using Microsoft.AspNetCore.Mvc;
using UsersApi.Dtos;
using UsersApi.Services;

namespace UsersApi.Controllers;

/// <summary>Gerenciamento de usuários.</summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Lista usuários com paginação. Use ?query= para filtrar pelo nome.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAll(
        [FromQuery] UserFilter filter, CancellationToken cancellationToken)
    {
        return Ok(await userService.GetPagedAsync(filter, cancellationToken));
    }

    /// <summary>Busca um usuário pelo id público.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        return Ok(await userService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Cria um usuário.</summary>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await userService.CreateAsync(request, cancellationToken);

        // 201 + header Location apontando para GET /api/users/{id}
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Atualiza nome, e-mail e perfil.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await userService.UpdateAsync(id, request, cancellationToken);

        return NoContent();
    }

    /// <summary>Remove um usuário.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
