using System.ComponentModel.DataAnnotations;

namespace webWarehouseInventoryManagement.DataAccess.Models
{
    public class UserModel
    {
        [Required]
        public string Name { get; set; }

        public string? Email { get; set; }
        [Required]
        public string PasswordHas { get; set; }
    }
}
