using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core;
using Core.Dtos;
using Core.Entity;
using Core.Models;
using Core.Repository;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CloudGamesApi.Controllers;

/// <summary>
/// Gerencia a autenticação de usuários e a geração de tokens JWT.
/// </summary>
[ApiController]
[Route("/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    
    private IValidator<LoginDto>  _loginDtoValidator;

    private IBaseLogger<AuthController> _logger;
    
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(IConfiguration configuration, IUserRepository  userRepository,
        IValidator<LoginDto> loginDtoValidator, IBaseLogger<AuthController> logger, IPasswordHasher<User> passwordHasher)
    {
        _configuration = configuration;
        _userRepository =  userRepository;
        _loginDtoValidator =  loginDtoValidator;
        _logger =  logger;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT.
    /// </summary>
    /// <remarks>
    /// Este endpoint valida as credenciais do usuário.<br/>
    /// Em caso de sucesso, gera um token JWT com validade de 30 minutos,<br/>
    /// contendo as claims 'sub' (username) e 'role' (permissão).
    /// </remarks>
    /// <param name="loginDto">Objeto contendo o 'Name' (username) e 'Password'.</param>
    /// <returns>Um objeto contendo o token JWT.</returns>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var validationResult = await _loginDtoValidator.ValidateAsync(loginDto);
        
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.ToDictionary()); 
            }
        
            _logger.LogInformation($"Tentativa de login para o usuário: {loginDto.Name}");
        
            var user = _userRepository.GetUserByUsername(loginDto.Name);
        
            if(user == null)
            {
                _logger.LogWarning($"Falha na autenticação: {loginDto.Name}. Usuário não encontrado.");
                return Unauthorized(new { message = "Usuário ou senha inválidos." });
            }
            
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password);
            
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning($"Falha na autenticação: {loginDto.Name}. Senha inválida.");
                return Unauthorized(new { message = "Usuário ou senha inválidos." });
            }
            
            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                _logger.LogInformation($"Usuário {loginDto.Name} autenticado. Atualizando hash de senha para novo padrão.");
                // Atualiza o hash no banco com o novo padrão, de forma transparente
                user.Password = _passwordHasher.HashPassword(user, loginDto.Password);
                _userRepository.Update(user); // Salva o novo hash
            }
            
            _logger.LogInformation($"Usuário {loginDto.Name} autenticado com sucesso.");
        
        
            var token = GenerateToken(user.Name, user.Permission);

            var loginResponse = new LoginResponseDto { Token = token };
            
            return Ok(loginResponse);

        }
        catch (Exception e)
        {
            _logger.LogError($"{e.Message} / Erro inesperado durante o login.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocorreu um erro interno."});
        }
    }
    
    /// <summary>
    /// Gera um token JWT para o usuário especificado.
    /// </summary>
    /// <param name="username">O nome de usuário (será usado como 'sub').</param>
    /// <param name="role">O nível de permissão (será usado como 'role').</param>
    /// <returns>Uma string contendo o token JWT.</returns>
    private string GenerateToken(string username, PermissionType role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer:_configuration["Jwt:Issuer"],
            claims:claims,
            expires:DateTime.Now.AddMinutes(30),
            signingCredentials:credential);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}