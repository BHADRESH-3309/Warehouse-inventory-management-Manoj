using System.Security.Claims;
using webWarehouseInventoryManagement.DataAccess.Models;

namespace webWarehouseInventoryManagement.Service
{
    public interface ITokenService
    {
        string GenerateToken(UserJWT user, string commaseperatedAccessiblePages);
        bool IsTokenValid(string token);
    }
}
