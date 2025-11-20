using webWarehouseInventoryManagement.DataAccess.DbAccess;
using static System.Collections.Specialized.BitVector32;

namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public class UsersActivityLog : IUsersActivityLog
    {
        private readonly ISqlDataAccess _dataAccess;
        public UsersActivityLog(ISqlDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task AddActivity(string userName, string section, string logText)
        {
            string quary = "INSERT INTO UsersActivityLog(UserName,Section,LogText)VALUES(@UserName,@Section,@LogText)";
            await _dataAccess.ExecuteDML(quary, new { UserName = userName, Section = section, LogText = logText });
        }

    }
}
