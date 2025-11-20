namespace webWarehouseInventoryManagement.DataAccess.Models
{
    public class MappingSkuVM
    {
        public Guid idSKU { get; set; }
        public string WarehouseSKU { get; set; }
        public string Type { get; set; }
        public Guid idMappingSKU { get; set; }
        public string SKU { get; set; }
    }
}
