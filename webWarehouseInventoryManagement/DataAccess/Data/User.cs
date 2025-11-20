using webWarehouseInventoryManagement.DataAccess.DbAccess;
using webWarehouseInventoryManagement.DataAccess.Models;

namespace webWarehouseInventoryManagement.DataAccess.Data
{
    public class User :IUser
    {
        private readonly ISqlDataAccess _db;
        public User(ISqlDataAccess db)
        {
            _db = db;
        }
        public async Task<UserJWT> GetUser(UserModel userModel,string userType)
        {
            //SELECT Name FROM Users WHERE Name='scraper' AND PasswordHas='Scraper@2020'
            string query = string.Empty;
            if (string.IsNullOrEmpty(userType))
            {
                //SELECT Name FROM Users WHERE Name='scraper' AND PasswordHas='Scraper@2020'
                query = @"SELECT u.Id, u.Username, r.RoleName FROM Users u LEFT JOIN Roles r ON u.RoleId = r.Id WHERE u.Username = @Username AND u.PasswordHash = @PasswordHash";
            }
            else
            {
                query = @"SELECT u.Id, u.Username, r.RoleName FROM Users u LEFT JOIN Roles r ON u.RoleId = r.Id WHERE u.Email = @Email";
            }
            var data = await _db.GetData<UserJWT, dynamic>(query, new { Username = userModel.Name, PasswordHash = userModel.PasswordHas, Email = userModel.Email });
            return data.FirstOrDefault();
        }

        public void UpdateLoginTime(string idUser)
        {
            _db.ExecuteDML("UPDATE Users SET LastActivityDate = GETDATE() WHERE Id = @idUser", new { idUser }).GetAwaiter().GetResult();
        }

        public async Task<int> InsertGoogleUser(UserModel googleUser)
        {
            // Add new user
            int userId = 0;
            try
            {
                string addUserSQL = @"INSERT INTO Users(Username, Email, RoleId, DateAdd)
                                   VALUES(@Username, @Email, @RoleId, GETDATE())";
                await _db.ExecuteDML(addUserSQL, new { Username = googleUser.Name, Email = googleUser.Email, RoleId = 2 });


                string getUserIdQuery = $"SELECT TOP 1 Id FROM Users WHERE Email = '{googleUser.Email}' ORDER BY Id DESC";
                string stringuserId = _db.GetSingleValue(getUserIdQuery);
                if (!string.IsNullOrEmpty(stringuserId))
                {
                    userId = int.Parse(stringuserId);
                }

                string insertPageAccess =
                               @"INSERT INTO UserPageAccess (UserId, PageId, HasAccess) 
                                VALUES (@UserId,(select Id from Pages where PageName='Failed'),1)";

                await _db.ExecuteDML(insertPageAccess, new { UserId = stringuserId });


            }
            catch (Exception)
            {

            }
            return userId;

        }
    }
}
