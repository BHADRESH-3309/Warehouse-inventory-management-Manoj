using System.Globalization;
using webWarehouseInventoryManagement.DataAccess.DbAccess;
using webWarehouseInventoryManagement.DataAccess.Models;
using webWarehouseInventoryManagement.Models;

namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public class Log : ILog
    {
        ISqlDataAccess _db;
        private readonly IUsersActivityLog _activityLog;
        public string UserName { get; set; }
        public string PageName { get; set; }
        public string Section { get; set; }
        public Log(ISqlDataAccess db, IUsersActivityLog activityLog)
        {
            _db = db;
            _activityLog = activityLog;
        }
        public async Task<IEnumerable<LogModel>> Loglist()
        {
            string Query = string.Empty;
            string query1 = string.Empty;
            query1 = "Delete from tblLog  Where DateAdd <= DATEADD(dd,-30, GETDATE())";
            await _db.ExecuteDML(query1, new { });

            Query = @"SELECT L.LogDetails, P.WarehouseSKU, L.idProduct, L.DateAdd
                    FROM ( SELECT LogDetails, idProduct,DateAdd FROM tblLog
                    WHERE DateAdd >= DATEADD(DAY, -20, GETDATE()) ) AS L
                    JOIN tblProduct AS P ON L.idProduct = P.idProduct
                    ORDER BY L.DateAdd DESC;";
            return await _db.GetData<LogModel, dynamic>(Query, new { });
        }
        public async Task<ResponseModel> ClearLogList(string section)
        {
            ResponseModel reslut = new ResponseModel();
            try
            {
                string quary = $"delete from UsersActivityLog where Section='{section}'";
                await _db.ExecuteDML(quary, new { });
                reslut.IsError = false;
            }
            catch (Exception ex)
            {
                reslut.IsError = true;
                reslut.Message = $"Something went wrong while removing logs for the {section} tab. Error: {ex.Message}";

            }
            return reslut;
        }

        public async Task<ResponseModel> GetActivityLog(string section)
        {
            ResponseModel response = new ResponseModel();
            IEnumerable<LogResultModel> data = new List<LogResultModel>();

            try
            {
                string quary = $"select UserName,LogText from UsersActivityLog where LOWER(Section)=LOWER('{section}') order by DateAdd desc";
                response.IsError = false;
                data = await _db.GetData<LogResultModel, dynamic>(quary, new { });
                response.Result = data;
            }
            catch
            {
                response.IsError = true;
                response.Message = "An error occurred while processing your request. Please try again later.";
                response.Result = data;
            }
            return response;
        }

        public async Task AddUserActivityLog(string section, string message)
        {
            string logMessage = $"{ActivityLogSting()}{message}";
            await _activityLog.AddActivity(UserName, section, logMessage);
        }
        private string ActivityLogSting()
        {
            return PageName + " || " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) + " || ";
        }
    }
}
