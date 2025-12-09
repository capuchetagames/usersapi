using Core.Dtos;
using Core.Entity;

namespace Core.Repository;

public interface IUserRepository : IRepository<User>
{
    UserDto GetUserGames(int id);
    User? GetUserByEmail(string email);
    User? GetUserByUsername(string username);
}