using webWarehouseInventoryManagement.DataAccess.DbAccess;
using webWarehouseInventoryManagement.DataAccess.Models;

namespace webWarehouseInventoryManagement.Service
{
    public class AccessService
    {
        private readonly ISqlDataAccess _context;

        public AccessService(ISqlDataAccess context) 
        {
            _context = context;
        }

        public async Task<List<string>> GetUserAccessiblePagesAsync(int userId)
        {
            string query = @"
            SELECT p.PageUrl FROM Users u
            JOIN UserPageAccess upa ON u.Id = upa.UserId AND upa.HasAccess = 1
            JOIN Pages p ON upa.PageId = p.Id
            WHERE u.Id = @userId";
            var data = await _context.GetData<Page, dynamic>(query, new { userId });

            return data.Select(r => r.PageUrl).ToList();
        }
    }

}
