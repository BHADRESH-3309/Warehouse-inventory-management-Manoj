namespace webWarehouseInventoryManagement.DataAccess.Models
{
    public class UserJWT
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string RoleName { get; set; }
    }

}
