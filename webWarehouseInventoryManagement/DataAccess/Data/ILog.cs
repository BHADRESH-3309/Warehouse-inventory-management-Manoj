using Aspose.Foundation.UriResolver.RequestResponses;
using webWarehouseInventoryManagement.DataAccess.Models;
using webWarehouseInventoryManagement.Models;

namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public interface ILog
    {
        string UserName { get; set; }
        string PageName { get; set; }
        string Section { get; set; }
        Task<IEnumerable<LogModel>> Loglist();
        Task<ResponseModel> ClearLogList(string section);

        Task<ResponseModel> GetActivityLog(string section);
        Task AddUserActivityLog(string section,string message);
    }
}
