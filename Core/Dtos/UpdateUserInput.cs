namespace Core.Dtos;

/// <summary>
/// DTO para atualização de usuário.
/// </summary>
public class UpdateUserInput :UserInput
{
    /// <summary>
    /// ID do usuário a ser atualizado.
    /// </summary>
    public int Id { get; set; }
}