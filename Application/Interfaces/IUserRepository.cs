using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserRepository
    {
        Task<Users?> GetByEmailAsync(string email);
        Task<Users?> GetByIdAsync(Guid id);
        Task<List<Users>> GetAllAsync();
        Task AddAsync(Users user);
        Task UpdateAsync(Users user);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
