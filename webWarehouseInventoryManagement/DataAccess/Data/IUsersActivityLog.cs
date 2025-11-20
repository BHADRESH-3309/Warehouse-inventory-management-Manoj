namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public interface IUsersActivityLog
    {
        Task AddActivity(string userName, string section, string logText);
    }
}
