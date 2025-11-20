namespace webWarehouseInventoryManagement.DataAccess.Models
{
    public class ResponseModel
    {
        public bool IsError { get; set; }
        public IEnumerable<dynamic>? Result { get; set; }
        public string? Message { get; set; }
        public string? fileName { get; set; }
        public string? WeightedVelocity { get; set; }
        public int WarehouseQty { get; set; }
        public int Quantity { get; set; }
    }
}
