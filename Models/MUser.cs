using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace BookingApinetcore.Models
{
    public class MUser
    {
        public static readonly string ADMIN_TYPE = "admin";

        private string hashedPassword;

        //Used for password hashing
        const int keySize = 64;
        const int iterations = 350000;
        static HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA512;


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserID { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        public string? UserName { get; set; } = string.Empty;


        public string? Password
        {
            get { return hashedPassword; }
            set
            {

                //Hash password here
                if (value?.Trim() == string.Empty) hashedPassword = "-";

                else
                    hashedPassword = passwordHash(value ?? "");

            }
        }

        public string FullName { get; set; } = string.Empty;
        public string PhysicalAddress { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.PhoneNumber)]
        public string Telephone { get; set; } = string.Empty;
        public string OriginCountry { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;
        public int Experience { get; set; }
        public string Position { get; set; } = string.Empty;
        public string DisabilityStatus { get; set; } = string.Empty;

        public string IdNumber { get; set; }

        //Generate password hash

        public static string passwordHash(string plain)
        {

            // generate a 128-bit salt using a cryptographically strong random sequence of nonzero values
            byte[] salt = Encoding.UTF8.GetBytes("This is my salt/sugar");
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetNonZeroBytes(salt);
            }
            Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: plain,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashed;
        }
    }
}
