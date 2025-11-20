using webWarehouseInventoryManagement.DataAccess.Models;

namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public interface IManageUserService
    {
         string UserName { get; set; }
         string PageName { get; set; }
         string Section { get; set; }
        Task<IEnumerable<ManageUserModel>> GetManageUserData();
        Task<IEnumerable<Role>> GetRoles();
        Task<IEnumerable<Page>> GetPages();
        Task<ResponseModel> RemoveUserData(string idUser);
        Task<ResponseModel> AddUpdateUserData(string? idUser, string userName, string password, string role, List<string> pageIds);
        Task<ResponseModel> UpdateGoogleUserData(string? googleUserId, string googleUsername, string googleEmail, string googleUserRole, List<string> GooglepageIds);
        Task AddUserActivityLog(string message);
        string GetdbUserName(string idUser);
    }
}
