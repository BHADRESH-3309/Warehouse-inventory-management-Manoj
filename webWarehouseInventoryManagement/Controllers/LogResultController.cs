using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using webWarehouseInventoryManagement.DataAccess.Data;
using webWarehouseInventoryManagement.DataAccess.Models;
using webWarehouseInventoryManagement.Models;

namespace webWarehouseInventoryManagement.Controllers
{
    [Authorize]
    public class LogResultController : Controller
    {
        ILog _db;
        public LogResultController(ILog db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetTabLogs(string section)
        {
            ResponseModel result = await _db.GetActivityLog(section);

            return Json(new { IsError = result.IsError, Message = result.Message, data = result.Result });
        }

        public async Task<IActionResult> ClearLog(string section)
        {
            GetUserName();
            GetPageName("Log Page");
            ResponseModel result = await _db.ClearLogList(section);
            if (result.IsError == false)
            {
                string message = $"Section: {section} - Logs cleared successfully!";
                await _db.AddUserActivityLog(section, message);
            }
            return Json(new { IsError = result.IsError, Message = result.Message });
        }

        private void GetUserName()
        {
            _db.UserName = User.Identity.Name;
        }
        private void GetPageName(string name)
        {
            _db.PageName = name;
        }


    }
}

