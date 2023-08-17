using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using User.Management.API.Models;
using User.Management.API.Models.Authentication.Login;
using User.Management.API.Models.Authentication.SignUp;
using User.Management.API.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace User.Management.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly JwtService _jwtService;
        public AuthenticationController(UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager, IConfiguration configuration
            , JwtService jwtService, IConfiguration config)
        {
            this._userManager = userManager;
            this._roleManager = roleManager;
            this._configuration = configuration;
            this._jwtService = jwtService;
            this._config = config;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterUser registerUser, string role)
        {
            //Check User Exist
            if (registerUser == null)
            {
                throw new ArgumentNullException(nameof(registerUser));
            }

            //Add the User in the database
            IdentityUser user = new IdentityUser()
            {
                Email = registerUser.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registerUser.UserName,
            };
            if (await _roleManager.RoleExistsAsync(role))
            {
                var result = await _userManager.CreateAsync(user, registerUser.Password);
                if (!result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new Response { Status = "Error", Message = string.Join(",", result.Errors) });
                }

                await _userManager.AddToRoleAsync(user, role);
                return StatusCode(StatusCodes.Status201Created,
                    new Response { Status = "Success", Message = "User Create Successfully" });
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = "This Role doesnt exist" });
            }


            //Assign a role
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(loginModel.Username);
                var useridprot = _config.GetValue<string>("UserIdNameProtocal");

                if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
                {
                    var authClaims = new List<Claim>
                    {
                        new Claim(useridprot, user.Id),
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name,user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                    };

                    var jwtToken = _jwtService.GetToken(authClaims);

                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                        expiration = jwtToken.ValidTo
                    });
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                // Log the exception to a text file or any other logging mechanism
                LogException(ex);
                // Return an appropriate response to the client
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the request.");
            }
        }

        private void LogException(Exception ex)
        {
            // Get the base directory of the application
            var baseDirectory = AppContext.BaseDirectory;

            // Specify the file path for the log file in the root directory
            var filePath = Path.Combine(baseDirectory, "error_log.txt");

            // Build the log message
            var logText = $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";

            // Check if there is an inner exception
            if (ex.InnerException != null)
            {
                logText += $"\nInner Exception: {ex.InnerException.Message}\nInner Exception StackTrace: {ex.InnerException.StackTrace}";
            }

            // Add additional exception details if available
            if (ex.Data.Count > 0)
            {
                logText += "\nAdditional Exception Data:";
                foreach (var key in ex.Data.Keys)
                {
                    logText += $"\n{key}: {ex.Data[key]}";
                }
            }

            // Log the exception to the specified file path
            System.IO.File.WriteAllText(filePath, logText);
        }

        [HttpGet("test")]
        public string test()
        {
            return "1234";
        }

        [HttpGet("randomnumber")]
        [Authorize]
        public IActionResult GetRandomNumber()
        {
            Random random = new Random();
            int randomNumber = random.Next(1111111, 9999999);
            return Ok(randomNumber.ToString());
        }

    }
}
