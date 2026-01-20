namespace Core.Dtos;

public record UserCreatedEvent
(
    int UserId,
    string Nome,
    string Email
);