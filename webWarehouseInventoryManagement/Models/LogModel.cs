namespace webWarehouseInventoryManagement.Models
{
    public class LogModel
    {
        public Guid idLog { get; set; }
        public string WarehouseSKU { get; set; }

        public string LogDetails { get; set; }
        public DateTime DateAdd { get; set; }
        public string Message { get; set; }
    }
}
