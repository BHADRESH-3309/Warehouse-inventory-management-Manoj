namespace webWarehouseInventoryManagement.DataAccess.Models
{
    public class ManageUserModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string PasswordHash { get; set; }
        public string AccessiblePageIds { get; set; }
        public string AccessiblePages { get; set; }
        public string? DateAdd { get; set; }
        public string? LastActivityDate { get; set; }
    }
}
