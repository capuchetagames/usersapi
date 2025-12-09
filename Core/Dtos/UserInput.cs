namespace Core.Dtos;

/// <summary>
/// DTO para criação de usuário (Admin).
/// </summary>
public class UserInput : BaseUserDto
{ 
    /// <summary>
    /// Nível de permissão do usuário.
    /// </summary>
    public required PermissionType Permission  { get; set; }
}