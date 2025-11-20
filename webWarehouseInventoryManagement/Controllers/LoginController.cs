using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using webWarehouseInventoryManagement.Service;
using webWarehouseInventoryManagement.DataAccess.Data;
using webWarehouseInventoryManagement.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.VisualBasic;


namespace webWarehouseInventoryManagement.Controllers
{
    public class LoginController : Controller
    {
        private readonly IUser _userRepository;
        private readonly ITokenService _tokenService;
        private readonly AccessService _accessService;

        private string generatedToken = string.Empty;

        public LoginController(IUser userRepository, ITokenService tokenService, AccessService accessService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _accessService = accessService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            string token = HttpContext.Session.GetString("Token");

            if (!string.IsNullOrEmpty(token))
            {
                if (_tokenService.IsTokenValid(token))
                {                   
                    var userRole = User.Claims.FirstOrDefault(c => c.Type.Contains("role")).Value;
                    if (userRole.ToLower() == "AmazonCustomOrder".ToLower())
                    {
                        return Redirect("~/Order/Index");
                    }

                    return Redirect("~/ListingForm/Index");
                }
            }

            return View();
        }

        [AllowAnonymous]
        [Route("login")]
        [HttpPost]
        public async Task<ActionResult> Login(UserModel userModel)
        {
            ViewBag.Error = "Your login attempt was not successful or don't have permission to access.";
            if (string.IsNullOrEmpty(userModel.Name) || string.IsNullOrEmpty(userModel.PasswordHas))
            {
                return View();
            }

            var validUser = await GetUser(userModel,string.Empty);

            if (validUser != null)
            {
                List<string> accessiblePages = await _accessService.GetUserAccessiblePagesAsync(validUser.Id);
                string commaseperatedAccessiblePages = string.Join(",", accessiblePages);

                generatedToken = _tokenService.GenerateToken(validUser, commaseperatedAccessiblePages);
                if (generatedToken != null)
                {
                    HttpContext.Session.SetString("Token", generatedToken);

                    _userRepository.UpdateLoginTime(validUser.Id.ToString());
                    if (validUser.RoleName.ToLower() == "AmazonCustomOrder".ToLower())
                    {
                        return Redirect("~/Order/Index");
                    }

                    return Redirect("~/ListingForm/Index");
                }
            }

            return View();
        }

        #region google Auth Functinality 

        [HttpPost]
        public async Task GoogleLogin()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties()
            {
                RedirectUri = Url.Action("GoogleResponse")
            });
        }


        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            UserModel googleUser = new UserModel();
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result != null)
            {
                if (result.Principal != null)
                {
                    if (result.Principal.Claims != null)
                    {
                        var claims = result.Principal.Identities
                            .FirstOrDefault();
                        if (claims != null)
                        {
                            googleUser.Name = claims.Name.ToString();
                            googleUser.Email = claims.Claims.ElementAt(4).Value;
                        }

                        if (googleUser.Email != null)
                        {
                            var dbGooglevalidUser = await GetUser(googleUser, "GoogleAuth");
                            if (dbGooglevalidUser != null)
                            {
                                List<string> accessiblePages = await _accessService.GetUserAccessiblePagesAsync(dbGooglevalidUser.Id);
                                string commaseperatedAccessiblePages = string.Join(",", accessiblePages);

                                generatedToken = _tokenService.GenerateToken(dbGooglevalidUser, commaseperatedAccessiblePages);
                                if (generatedToken != null)
                                {
                                    HttpContext.Session.SetString("Token", generatedToken);

                                    _userRepository.UpdateLoginTime(dbGooglevalidUser.Id.ToString());

                                    return RedirectToAction("Index", "Failed");
                                }
                            }
                            else
                            {

                                int dbGoogleUserId = await _userRepository.InsertGoogleUser(googleUser);
                                if (dbGoogleUserId != 0)
                                {
                                    UserJWT user = new UserJWT();
                                    user.Id = dbGoogleUserId;
                                    user.Email = googleUser.Email;
                                    user.Username = googleUser.Name;
                                    user.RoleName = "User";

                                    List<string> accessiblePages = await _accessService.GetUserAccessiblePagesAsync(dbGoogleUserId);
                                    string commaseperatedAccessiblePages = string.Join(",", accessiblePages);
                                    generatedToken = _tokenService.GenerateToken(user, commaseperatedAccessiblePages);
                                    if (generatedToken != null)
                                    {
                                        HttpContext.Session.SetString("Token", generatedToken);

                                        _userRepository.UpdateLoginTime(user.Id.ToString());

                                        return RedirectToAction("Index", "Failed");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return RedirectToAction("Login");
        }


        #endregion 



        [Route("logout")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
            return RedirectToAction("Login");
        }

        private async Task<UserJWT> GetUser(UserModel userModel,string userType)
        {
            return await _userRepository.GetUser(userModel, userType);
        }  
    }
}
