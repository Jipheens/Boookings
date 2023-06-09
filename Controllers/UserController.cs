using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using BookingApinetcore.Models;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Net.Http;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace BookingApinetcore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly IConfiguration _config;
        public UserController(IConfiguration config)
        {
            _config = config;

        }

        // GET: api/<UserController>
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET Get a user by ID
        [HttpGet]
        public IActionResult Get()
        {
            Hashtable values = new Hashtable();

            values["Success"] = true;
            values["user"] = new MUser()
            {
                UserName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "",
                FullName = User.FindFirst(ClaimTypes.Name)?.Value ?? "",
                UserID = int.Parse(User.FindFirst(ClaimTypes.PrimarySid)?.Value ?? "0"),
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? ""

            };

            return new OkObjectResult(values);
        }

        //Update User
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] MUser user)
        {
            Hashtable values = new Hashtable(); //Return values in form of a message and result

            //Get user id
            int userID = int.Parse(User.FindFirst(ClaimTypes.PrimarySid)?.Value ?? "0");

            if (ModelState.IsValid)//Update user
            {
                bool response = await CreateUpdateUser(user, userID);

                if (response)
                {
                    user.Password = " ";
                    values.Add("user", user);
                    values.Add("success", true);

                    return new OkObjectResult(values);
                }

            }

            //Return values in case of an errror
            values.Add("Message", "Error occurred while processing your request. Try again");
            values.Add("Success", false);
            return new BadRequestObjectResult(values);



        }

        //Test api
        [HttpGet]
        [Route("Test")]
        [Authorize(AuthenticationSchemes = "microsoft")]
        public async Task<string> Test()
        {
            string email = User.FindFirst(ClaimTypes.Upn)?.Value ?? "Fail message";

            string username = User.FindFirst(ClaimTypes.Country)?.Value ?? "Fail message";

            
            return username;
        }

            private async Task<bool> CreateUpdateUser(MUser user, int id)
        {
            //Catch error while registering
            try
            {
                using (SqlConnection _connection = new SqlConnection(_config.GetConnectionString("connString")))
                {
                    _connection.OpenAsync().Wait();

                    using (SqlCommand command = new SqlCommand("spInsertUpdateUser", _connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("userID", SqlDbType.NVarChar).Value = id;
                        command.Parameters.AddWithValue("userName", SqlDbType.NVarChar).Value = user.UserName;
                        command.Parameters.AddWithValue("password", SqlDbType.NVarChar).Value = user.Password;
                        command.Parameters.AddWithValue("email", SqlDbType.NVarChar).Value = user.Email;
                        command.Parameters.AddWithValue("fullName", SqlDbType.NVarChar).Value = user.FullName;
                        command.Parameters.AddWithValue("physicalAddress", SqlDbType.NVarChar).Value = user.PhysicalAddress;
                        command.Parameters.AddWithValue("telephone", SqlDbType.NVarChar).Value = user.Telephone;
                        command.Parameters.AddWithValue("originCountry", SqlDbType.NVarChar).Value = user.OriginCountry;
                        command.Parameters.AddWithValue("employerName", SqlDbType.NVarChar).Value = user.EmployerName;
                        command.Parameters.AddWithValue("experience", SqlDbType.Int).Value = user.Experience;
                        command.Parameters.AddWithValue("position", SqlDbType.NVarChar).Value = user.Position;
                        command.Parameters.AddWithValue("disabilityStatus", SqlDbType.NVarChar).Value = user.DisabilityStatus;

                        SqlDataReader reader = await command.ExecuteReaderAsync();
                        reader.ReadAsync().Wait();

                        if (reader.GetInt32(0) == 1) //Success


                            return true;
                        else

                            return false;

                    }

                }



            }
            catch
            {
                //Log error message

                //values.Add("Message", e.Message);
                //values.Add("Success", false);
                //return new BadRequestObjectResult(values);
                return false;
            }

        }


        //Used to renew a token, as well as return user information.
        //Used when the user tried to authenticate using a third party or trying to renew a token before expire
        [HttpGet]
        [Route("gettoken")] //Get token
        [Authorize(AuthenticationSchemes = "microsoft")]
        public async Task<IActionResult> GetToken()
        {
            //check flag that ensure that the user has a locally generate token.
            //If true regenerate token else
            //Create a new token
            string local = User.FindFirst("LocalToken")?.Value ?? "No";
            if (local.ToLower() != "yes")
            {
                //Regenerate token
                return await GenerateJwt();

            }
            else //Regenerate a local token as well as return user info
            {
                return RedirectToAction("/api/Booking/");
            }

        }

        
        private async Task<IActionResult> GenerateJwt()
        {
            Hashtable values = new Hashtable(); //To hold return values

            //use passed in email to get user information
            //Get Userrecord if exists
            try
            {
                //Connect to database.
                //Return 1 if email exists, 0 if user does not and -1 if an error occured
                using (SqlConnection _connection = new SqlConnection(_config.GetConnectionString("connString")))
                {
                    //Connect to database then read booking records
                    _connection.OpenAsync().Wait();

                    using (SqlCommand command = new SqlCommand("spSelectUser", _connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("userName", SqlDbType.NVarChar).Value = " "; //We use email instead

                        //Get email from the user claims
                        command.Parameters.AddWithValue("email", SqlDbType.NVarChar).Value =
                            User.FindFirst(ClaimTypes.Upn)?.Value ?? "Fail message";

                        SqlDataReader reader = await command.ExecuteReaderAsync();

                        if (reader.HasRows)
                        {
                            reader.Read();
                            //Create a user principal. I.e login user
                            //Check if password match then return ok
                                //Create a user model
                            MUser loggedINUser = new MUser()
                            {
                                UserID = reader.GetInt64(0),
                                Email = reader.GetString(2),
                                UserName = reader.GetString(1),
                                FullName = reader.GetString(3),
                                Telephone = reader.GetString(5),
                                PhysicalAddress = reader.GetString(4),
                                OriginCountry = reader.GetString(6),
                                EmployerName = reader.GetString(7),
                                Experience = reader.GetInt32(8),
                                Position = reader.GetString(9),
                                DisabilityStatus = reader.GetString(10),
                                Password = " "

                            };

                            //Now we create relevant logged in user
                         
                            string tokenstring = GenerateTokenString(loggedINUser);

                            values.Add("token", tokenstring);
                            values.Add("success", true);
                            values.Add("user", loggedINUser);

                            return new OkObjectResult(values);
                        }

                        //Tell user that their creadential are either wrong or do not exist
                        //Register user
                        var user = await RegisterUser();

                        if(user != null)
                        {
                            values.Add("success", true);
                            values.Add("user", user);
                            values.Add("token", GenerateTokenString(user));
                            

                            return new OkObjectResult(values);

                        }

                        

                       return new UnauthorizedObjectResult(values);


                    }
                }


            }
            catch (Exception e)
            {
                values.Add("Message", e.Message);//"Error trying to process your request"
                values.Add("Success", false);

                return new BadRequestObjectResult(values)
                {
                    StatusCode = 408
                };

            }

        }

        private string GenerateTokenString(MUser loggedINUser)
        {
            //Creating a Jwt object
            Jwt jwt = _config.GetSection("Jwt").Get<Jwt>();


            //Add relevant claims
            List<Claim> claims = new List<Claim>()
                                {
                                    new Claim(JwtRegisteredClaimNames.Sub, jwt.Subject),
                                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()),
                                    new Claim(ClaimTypes.PrimarySid, ""+ loggedINUser.UserID), //User id as identified by database
                                    new Claim(ClaimTypes.GivenName, loggedINUser.UserName), //Username. 
                                    new Claim(ClaimTypes.Country,loggedINUser.OriginCountry), //Country 
                                    new Claim(ClaimTypes.Name, loggedINUser.FullName),
                                    new Claim(ClaimTypes.Email, loggedINUser.Email),
                                        
                                    //Source of token indicator
                                    new Claim("LocalToken", "Yes")

                                    //And many more

                            };

                    //Most importantly. If username is admin add that claim to enable admin access
                    if (loggedINUser.UserName.ToLower() == "admin")
                    {
                        claims.Add(new Claim(MUser.ADMIN_TYPE, loggedINUser.UserName.ToLower()));
                    }

            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

            SigningCredentials signin = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken
            (
                jwt.Issuer,
                jwt.Audience,
                claims,
                expires: DateTime.Now.AddHours(1), //Token expires in one hour of in activities
                signingCredentials: signin
            );

            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        private async Task<MUser> RegisterUser()
        {
            //We create a user model
            MUser user = new MUser
            {
                UserName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "username",
                Email = User.FindFirst(ClaimTypes.Upn)?.Value ?? "mail@mail.com",
                Password = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "password",
                FullName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "John Doe",
                PhysicalAddress = User.FindFirst(ClaimTypes.StreetAddress)?.Value ?? "123 Nairobi",
                Telephone = User.FindFirst(ClaimTypes.MobilePhone)?.Value ?? "07922",
                OriginCountry = User.FindFirst(ClaimTypes.Country)?.Value ?? "Kenya",
                Experience = 0,
                Position = "Not specified",
                DisabilityStatus = "Not disabled"

            };
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://192.168.1.200:7030");

            HttpContent body = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");

            HttpResponseMessage res = await client.PostAsync("/register", body);

            if(res.IsSuccessStatusCode)
                return user;
            else
                return null;

            
        }

    }

    internal class Jwt
    {
        public string Key { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;
    }
}
