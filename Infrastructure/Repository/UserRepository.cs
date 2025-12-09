using Core.Dtos;
using Core.Entity;
using Core.Repository;

namespace Infrastructure.Repository;

public class UserRepository : EfRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }
    public UserDto GetUserGames(int id)
    {
        var user = _dbSet.FirstOrDefault(u => u.Id == id) ?? throw new Exception("User not found");
        
        
        var userDto = new UserDto()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            // Orders = user.Orders.Where(o => o.UserId == user.Id)
            //     .Select(o => new OrderDto()
            //     {
            //         
            //         Id = o.Id,
            //         CreatedAt = o.CreatedAt,
            //         UserId = o.UserId,
            //         GameId = o.GameId,
            //         Game = new GameDto()
            //         {
            //             Id = o.Game.Id,
            //             CreatedAt = o.Game.CreatedAt,
            //             Active = o.Game.Active,
            //             Category = o.Game.Category,
            //             Name = o.Game.Name,
            //             Price = o.Game.Price,
            //
            //         }
            //     }).ToList()
        };
        
        
        return userDto;
    }

    public User? GetUserByEmail(string email)
    {
        var user = _dbSet.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

        return user;

    }

    public User? GetUserByUsername(string username)
    {
        var user = _dbSet.FirstOrDefault(u=>u.Name.ToLower() == username.ToLower());
        
        return user;
    }
}