using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Data.SqlClient;
using BookingApinetcore.Models;

namespace BookingApinetcore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly SqlConnection _connection;
        public BookingController(IConfiguration config)
        {
            _connection = new SqlConnection(config.GetConnectionString("connString"));
        }
        // GET: api/<BookingController>
        //Get all bookings. Only admin
        [HttpGet]
        [Authorize(Policy = "AdminUser")]
        [Route("/api/Booking/GetAll")]
        public async Task<JsonResult> GetAll(int? start, int? end)
        {

            try
            {

                List<MBooking> bookings = new List<MBooking>();
                JsonResult result = new JsonResult(bookings);

                //Set defaults for start and end indeces if not given
                start = start ?? 0;
                end = end ?? 100000;

                // end = end > 1000 ? 100 : end;

                using (_connection)
                {
                    //Connect to database then read booking records
                    _connection.OpenAsync().Wait();

                    using (SqlCommand command = new SqlCommand("spSelectAllBookings", _connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("start", SqlDbType.Int).Value = start;
                        command.Parameters.AddWithValue("end", SqlDbType.Int).Value = 10000;

                        SqlDataReader reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            bookings.Add(new MBooking
                            {
                                BookingId = reader.GetInt64(0),
                                ExternalSchemeAdmin = reader.GetString(1),
                                CourseDate = reader.GetDateTime(2).Date.ToString(),
                                BookingType = reader.GetString(3),
                                RetirementSchemeName = reader.GetString(4),
                                SchemePosition = reader.GetString(5),
                                TrainingVenue = reader.GetString(6),
                                PaymentMode = reader.GetString(7),
                                AdditionalRequirements = reader.GetString(8),
                                UserId = reader.GetInt64(9)

                            });

                        }

                    }

                }

                return result;

            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message);
            }
        }

        [HttpGet]
        //Get a record per user
        public async Task<JsonResult> Get(int? userID)
        {


            try
            {

                List<MBooking> bookings = new List<MBooking>();
                JsonResult result = new JsonResult(bookings);

                userID = userID ?? (User.HasClaim(MUser.ADMIN_TYPE, "admin") ? 0 : int.Parse(User.FindFirst(ClaimTypes.PrimarySid)?.Value ?? "0"));

                //If not admin or owner of the record return empty list
                if (!User.HasClaim(MUser.ADMIN_TYPE, "admin")
                    && (int.Parse(User.FindFirst(ClaimTypes.PrimarySid)?.Value ?? "0") != userID)
                    )
                    return result;



                //end = end > 1000 ? 100 : end;

                //Connect to database then read booking records
                _connection.OpenAsync().Wait();

                using (SqlCommand command = new SqlCommand("spSelectUserBookings", _connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("id", SqlDbType.Int).Value = userID;

                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        bookings.Add(new MBooking
                        {
                            BookingId = reader.GetInt64(0),
                            ExternalSchemeAdmin = reader.GetString(1),
                            CourseDate = reader.GetDateTime(2).Date.ToString(),
                            BookingType = reader.GetString(3),
                            RetirementSchemeName = reader.GetString(4),
                            SchemePosition = reader.GetString(5),
                            TrainingVenue = reader.GetString(6),
                            PaymentMode = reader.GetString(7),
                            AdditionalRequirements = reader.GetString(8),
                            UserId = reader.GetInt64(9)

                        });

                    }

                }

                return result;

            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetbyDate")]
        public async Task<JsonResult> GetByDate(string startdate = "yyyy-mm-dd", string enddate = "yyyy-mm-dd")
        {
            try
            {
                List<MBooking> bookings = new List<MBooking>();
                JsonResult result = new JsonResult(bookings);

                int userId = int.Parse(User.FindFirst(ClaimTypes.PrimarySid)?.Value ?? "-1");

                if (User.HasClaim(MUser.ADMIN_TYPE, "admin"))
                    userId = 0;


                //DateOnly start, end, _default;

                //DateOnly.TryParse("default", out _default);


                //DateOnly.TryParse(startdate, out start);
                //DateOnly.TryParse(enddate, out end);

                //if (start == _default)
                //{
                //    start = end;
                //}


                //if (start > end)
                //{
                //    end = start;
                //}


                //Connect to database then read booking records
                _connection.OpenAsync().Wait();

                using (SqlCommand command = new SqlCommand("spSelectBookingDate", _connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("start", SqlDbType.NVarChar).Value = startdate;
                    command.Parameters.AddWithValue("end", SqlDbType.NVarChar).Value = enddate;
                    command.Parameters.AddWithValue("userID", SqlDbType.Int).Value = userId;

                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {

                        bookings.Add(new MBooking
                        {
                            BookingId = reader.GetInt64(0),
                            ExternalSchemeAdmin = reader.GetString(1),
                            CourseDate = reader.GetDateTime(2).Date.ToString(),
                            BookingType = reader.GetString(3),
                            RetirementSchemeName = reader.GetString(4),
                            SchemePosition = reader.GetString(5),
                            TrainingVenue = reader.GetString(6),
                            PaymentMode = reader.GetString(7),
                            AdditionalRequirements = reader.GetString(8),
                            UserId = reader.GetInt64(9)

                        });


                    }
                }


                return result;

            }
            catch (Exception ex)
            {
                return new JsonResult(ex.Message);
            }
        }


        // POST api/<BookingController>
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create([FromBody] UserBooking uBooking)
        {
            if (ModelState.IsValid) //Process data
            {
                Hashtable resBody = new Hashtable();

                //If user id is not the same as logged in user id return
                int userId = int.Parse(User.FindFirst(ClaimTypes.PrimarySid)?.Value ?? "0");

                if (true)//save booking else return unauthorized
                {
                    //Engulf in a exception
                    try
                    {
                        using (_connection)
                        {
                            //Connect to database then read booking records
                            _connection.OpenAsync().Wait();

                            using (SqlCommand command = new SqlCommand("spInsertUpdateBooking", _connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("bookingId", SqlDbType.Int).Value = 0;
                                command.Parameters.AddWithValue("externalSchemeAdmin", SqlDbType.NVarChar).Value = uBooking.Booking.ExternalSchemeAdmin;
                                command.Parameters.AddWithValue("bookingType", SqlDbType.NVarChar).Value = uBooking.Booking.BookingType;
                                command.Parameters.AddWithValue("retirementSchemeName", SqlDbType.NVarChar).Value = uBooking.Booking.RetirementSchemeName;
                                command.Parameters.AddWithValue("schemePosition", SqlDbType.NVarChar).Value = uBooking.Booking.SchemePosition;
                                command.Parameters.AddWithValue("trainingVenue", SqlDbType.NVarChar).Value = uBooking.Booking.TrainingVenue;
                                command.Parameters.AddWithValue("paymentMode", SqlDbType.NVarChar).Value = uBooking.Booking.PaymentMode;
                                command.Parameters.AddWithValue("additionalRequirements", SqlDbType.NVarChar).Value = uBooking.Booking.AdditionalRequirements;
                                command.Parameters.AddWithValue("userId ", SqlDbType.Int).Value = userId;

                                SqlDataReader reader = await command.ExecuteReaderAsync();

                            }

                        }

                        //Return resulst
                        resBody.Add("Success", true);
                        resBody.Add("Booking", uBooking.Booking);

                        return new OkObjectResult(resBody);

                    }
                    catch (Exception ex)
                    {
                        resBody.Add("Error_Message", ex.Message);
                        resBody.Add("Success", false);

                        return new BadRequestObjectResult(resBody);

                    }


                }
                else
                {
                    resBody.Add("Success", false);
                    resBody.Add("Message", "We could not process your booking. Try again");

                    return new UnauthorizedObjectResult(resBody);
                }

            }
            else
            {
                //Error occured
                return new BadRequestResult();
            }
        }
    }

    public class UserBooking
    {
        public MUser User { get; set; } = new MUser();

        public MBooking Booking { get; set; } = new MBooking();

    }
}
