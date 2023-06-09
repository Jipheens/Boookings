using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookingApinetcore.Models
{
    public class MBooking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BookingId { get; set; }


        public string ExternalSchemeAdmin { get; set; } = string.Empty;

        [Required]
        public string CourseDate { get; set; } = string.Empty;

        [Required]
        public string BookingType { get; set; } = string.Empty;

        public string RetirementSchemeName { get; set; } = string.Empty;
        public string SchemePosition { get; set; } = string.Empty;
        public string TrainingVenue { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
        public string AdditionalRequirements { get; set; } = string.Empty;
        public long UserId { get; set; }
    }
}
