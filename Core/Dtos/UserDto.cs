namespace Core.Dtos;

public class UserDto 
{ 
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string Name  { get; set; }
    public required string Email  { get; set; }
    //public virtual ICollection<OrderDto> Orders { get; set; }
}