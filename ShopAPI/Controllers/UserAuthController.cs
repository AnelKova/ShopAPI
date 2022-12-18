using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Context;
using ShopAPI.Models;
using System.Text.RegularExpressions;
using System.Text;
using ShopAPI.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using ShopAPI.Models.DTO;

namespace ShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAuthController : ControllerBase
    {
        private readonly ItemContext _itemContext;
        //private readonly IConfiguration _config;
        public UserAuthController(ItemContext itemContext/*, IConfiguration config*/)
        {
            _itemContext = itemContext;
            //_config = config;
        }



        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            var user = await _itemContext.Users.FirstOrDefaultAsync(x => x.UserName == userObj.UserName);
            if (user == null)
                return NotFound(new { Message = "User Not Found!" });

            if(!PasswordHasher.VerifyPassword(userObj.Password,user.Password))
            {
                return BadRequest(new { Message = "Password is incorect" });
            }
            user.Token = CreateJwt(user);
            var newAccessToken = user.Token;
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(5);
            await _itemContext.SaveChangesAsync();

            return Ok(new TokenApiDTO()
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            }
           ) ;
        }

        [HttpPost("register")]

        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();
            //check username
            if (await CheckUserNameExistAsync(userObj.UserName))
                return BadRequest(new { Message = "Username Already Exists" });

            //check email
            if (await CheckEmailExistAsync(userObj.Email))
                return BadRequest(new { Message = "Email Already Exists" });

            //check password srength
            var pass = CheckPasswordStrength(userObj.Password);
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass.ToString() });


            userObj.Password = PasswordHasher.HashPassword(userObj.Password);
            userObj.Role = "User";
            userObj.Token= CreateJwt(userObj);  
           
            await _itemContext.Users.AddAsync(userObj);
            await _itemContext.SaveChangesAsync();
            return Ok(new
            {
                Message = "User Registered",

            });
        }
        private async Task<bool> CheckUserNameExistAsync(string username)
        {
            return await _itemContext.Users.AnyAsync(x => x.UserName == username);
        }
        private async Task<bool> CheckEmailExistAsync(string email)
        {
            return await _itemContext.Users.AnyAsync(x => x.Email == email);
        }
        private string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();
            if (password.Length < 8)
                sb.Append("Minimum password should be 8+" + Environment.NewLine);
            if ((Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]") && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password should be Alphanumeric" + Environment.NewLine);
            if (!Regex.IsMatch(password, "[<,>,@,/,!,#,%,&,),€,(,?]"))
                sb.Append("Password should contain special chars" + Environment.NewLine);
            return sb.ToString();
        }

        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("veryverysecret...");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, user.Role), 
                new Claim(ClaimTypes.Name,$"{user.UserName}"),
                
            });
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddSeconds(10),
                SigningCredentials = credentials
            };
            var token= jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }

        private string CreateRefreshToken()
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var refreshToken = Convert.ToBase64String(tokenBytes);

            var tokenInUser=_itemContext.Users
                .Any(a=>a.RefreshToken == refreshToken);
            if (tokenInUser) 
            {
                return CreateRefreshToken();
            }
            return refreshToken;
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken (string token)
        {
            var key = Encoding.ASCII.GetBytes("veryverysecret...");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token,tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken; 
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
             throw new SecurityTokenException("this is Invalid Token!");
            return principal;
                
            
        }
        [Authorize]
        [HttpGet]

        public async Task<ActionResult<User>> GetAllUsers()
        {
            return Ok(await _itemContext.Users.ToListAsync());
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenApiDTO tokenApiDTO)
        {
            if (tokenApiDTO is null)
                return BadRequest("Invalid Client Request");
            string accessToken= tokenApiDTO.AccessToken;
            string refreshToken=tokenApiDTO.RefreshToken;
            var pricipal = GetPrincipalFromExpiredToken(accessToken);
            var username = pricipal.Identity.Name;
            var user = await _itemContext.Users.FirstOrDefaultAsync(u => u.UserName == username);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return BadRequest("Invalid Request");
            var newAccessToken = CreateJwt(user);
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken=newRefreshToken;
            await _itemContext.SaveChangesAsync();
            return Ok(new TokenApiDTO()
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
            });
        }

    }
}
