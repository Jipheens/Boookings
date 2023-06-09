using BookingApinetcore.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BookingApinetcore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            //services.AddMicrosoftIdentityWebApiAuthentication(Configuration);

            services.AddAuthentication(
            opt =>
            {
                opt.DefaultChallengeScheme = "localjwt";
                opt.DefaultAuthenticateScheme = "localjwt";
            }
           )
                .AddJwtBearer("localjwt", ops =>
            {
                ops.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),
                    ClockSkew = TimeSpan.Zero

                };


                //ops.ForwardSignIn = "/api/Account/Login";

            })
           .AddMicrosoftIdentityWebApi(Configuration, jwtBearerScheme:"microsoft");

            //Add admin policy and other policies
            services.AddAuthorization(ops =>
            {
                ops.AddPolicy("AdminUser", policy => policy.RequireClaim(MUser.ADMIN_TYPE, "admin"));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors(ops =>
                {
                    ops.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            }

            app.UseRouting();



            app.UseAuthentication();
            app.UseAuthorization();


            //Redirect back if user is not authenticated
            app.Use(async (Context, next) =>
            {
                if (!Context.User.Identity.IsAuthenticated)
                {
                    Context.Response.StatusCode = 401;
                    await Context.Response.WriteAsync("Unauthorized. Try logging in");
                }
                else
                {
                    await next();
                   
                }

            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
