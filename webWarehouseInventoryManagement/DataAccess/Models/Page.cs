namespace webWarehouseInventoryManagement.DataAccess.Models
{
    public class Page
    {
        public int Id { get; set; }
        public string PageName { get; set; } // e.g., Home, AdminPage
        public string PageUrl { get; set; } // e.g., /Home/Index
    }

}
