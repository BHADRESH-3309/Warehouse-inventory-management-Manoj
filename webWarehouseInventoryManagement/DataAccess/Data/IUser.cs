using webWarehouseInventoryManagement.DataAccess.Models;
namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public interface IUser
    {
        Task<UserJWT> GetUser(UserModel userMode,string userType);
        void UpdateLoginTime(string idUser);

        Task<int> InsertGoogleUser(UserModel googleUser);
    }
}
