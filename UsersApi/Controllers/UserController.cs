using Core;
using Core.Dtos;
using Core.Entity;
using Core.Repository;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CloudGamesApi.Controllers;

/// <summary>
/// Gerencia as operações CRUD para os usuários da plataforma.
/// </summary>
[ApiController]
[Route("/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    
    private IValidator<BaseUserDto> _validator;
    private readonly IPasswordHasher<User> _passwordHasher;
    
    public UserController(IUserRepository userRepository, IValidator<BaseUserDto>  validator, IPasswordHasher<User>  passwordHasher)
    {
        _userRepository = userRepository;
        _validator = validator;
        _passwordHasher = passwordHasher;
    }
    
    /// <summary>
    /// Lista todos os usuários cadastrados.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a usuários com permissão de 'Admin'.<br/>
    /// ATENÇÃO: Retorna a entidade User completa, incluindo hash de senha.
    /// </remarks>
    /// <returns>Uma lista de objetos User.</returns>
    [HttpGet] 
    [Authorize(Policy = nameof(PermissionType.Admin))]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Get()
    {
        try
        {
            var users = _userRepository.GetAll();
            var responseDtos = users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Permission = u.Permission
            });
            
            return Ok(responseDtos);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }
    
    
    /// <summary>
    /// Busca um usuário específico pelo ID.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a usuários com permissão de 'Admin'.
    /// </remarks>
    /// <param name="id">O ID (int) do usuário.</param>
    /// <returns>O objeto User.</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = nameof(PermissionType.Admin))]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Get([FromRoute] int id)
    {
        try
        {
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = $"Usuário com ID {id} não encontrado." });
            }
            
            var responseDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Permission = user.Permission
            };
            
            return Ok(responseDto);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }
    
    /// <summary>
    /// Lista os jogos associados a um usuário específico.
    /// </summary>
    /// <remarks>
    /// Requer autenticação. Um usuário comum só pode ver os próprios jogos (depende da lógica do repositório).
    /// </remarks>
    /// <param name="id">O ID (int) do usuário.</param>
    /// <returns>Uma lista de objetos Game.</returns>
    // [HttpGet("{id:int}/games")]
    // [Authorize] // Qualquer usuário autenticado pode acessar
    // [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // public IActionResult GetGames([FromRoute] int id)
    // {
    //     try
    //     {
    //         var userExists = _userRepository.GetById(id) != null;
    //         if (!userExists)
    //         {
    //             return NotFound(new { message = $"Usuário com ID {id} não encontrado." });
    //         }
    //         
    //         return Ok(_userRepository.GetUserGames(id));
    //     }
    //     catch (Exception e)
    //     {
    //         return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
    //     }
    // }

    /// <summary>
    /// Cria um novo usuário com permissões específicas.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a 'Admin'. Permite definir o nível de permissão (User ou Admin).
    /// </remarks>
    /// <param name="userInput">Dados do novo usuário, incluindo permissão.</param>
    /// <returns>O usuário recém-criado.</returns>
    [HttpPost]
    [Authorize(Policy = nameof(PermissionType.Admin))]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Post([FromBody] UserInput userInput)
    {
        try
        {
            var validationResult =  _validator.Validate(userInput);
        
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
            
            var user = new User()
            {
                Name = userInput.Name,
                Email = userInput.Email,
                Password = "",
                Permission = userInput.Permission,
            };
            
            user.Password = _passwordHasher.HashPassword(user, userInput.Password);
            
            _userRepository.Add(user);
            
            var responseDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Permission = user.Permission
            };
            
            return CreatedAtAction(nameof(Get), new { id = user.Id }, responseDto);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }
    
    /// <summary>
    /// Registra um novo usuário na plataforma.
    /// </summary>
    /// <remarks>
    /// Endpoint público para auto-registro.<br/>
    /// Usuários criados por aqui sempre terão a permissão 'User'.
    /// </remarks>
    /// <param name="userDto">Dados básicos do novo usuário (nome, email, senha).</param>
    /// <returns>O usuário recém-criado.</returns>
    [HttpPost("/new-user")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult PostNewUser([FromBody] BaseUserDto userDto)
    {
        try
        {
            var validationResult =  _validator.Validate(userDto);
        
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
            
            var user = new User()
            {
                Name = userDto.Name,
                Email = userDto.Email,
                Password = "",
                Permission = PermissionType.User,
            };
            
            user.Password = _passwordHasher.HashPassword(user, userDto.Password);
            
            _userRepository.Add(user);
            
            var responseDto = new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Permission = user.Permission
            };
            
            return CreatedAtAction(nameof(Get), new { id = user.Id }, responseDto);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }
    
    /// <summary>
    /// Atualiza um usuário existente.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a 'Admin'.
    /// </remarks>
    /// <param name="userInput">Dados do usuário a ser atualizado (incluindo ID).</param>
    /// <returns>Nenhum conteúdo.</returns>
    [HttpPut]
    [Authorize(Policy = nameof(PermissionType.Admin))]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Put([FromBody] UpdateUserInput userInput)
    {
        try
        {
            var validationResult =  _validator.Validate(userInput);
        
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
            
            var user = _userRepository.GetById(userInput.Id);
            
            if (user == null)
            {
                return NotFound(new { message = $"Usuário com ID {userInput.Id} não encontrado." });
            }
            
            user.Name = userInput.Name;
            user.Email = userInput.Email;
            user.Permission = userInput.Permission;
            
            if (!string.IsNullOrEmpty(userInput.Password))
            {
                user.Password = _passwordHasher.HashPassword(user, userInput.Password);
            }
            
            _userRepository.Update(user);
            
            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }

    /// <summary>
    /// Exclui um usuário pelo ID.
    /// </summary>
    /// <remarks>
    /// Acesso restrito a 'Admin'.
    /// </remarks>
    /// <param name="id">O ID (int) do usuário a ser excluído.</param>
    /// <returns>Nenhum conteúdo.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = nameof(PermissionType.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Delete([FromRoute] int id)
    {
        try
        {
            var user = _userRepository.GetById(id);
            if (user == null)
            {
                return NotFound(new { message = $"Usuário com ID {id} não encontrado." });
            }
            
            _userRepository.Delete(id);
            
            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro interno", error = e.Message });
        }
    }
    
}