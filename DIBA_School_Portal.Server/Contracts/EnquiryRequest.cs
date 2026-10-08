using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    /// <summary>Payload for POST /api/enquiries (admission form).</summary>
    public class EnquiryRequest
    {
        public string? ChildFirstName { get; set; }
        public string? ChildLastName { get; set; }
        public string? ChildDateOfBirth { get; set; }
        public string? ChildGender { get; set; }
        public string? Section { get; set; }
        public string? PreviousSchool { get; set; }
        public string? PreferredStartTerm { get; set; }

        [Required, MaxLength(150)]
        public string ParentName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string ParentEmail { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? ParentPhone { get; set; }

        [Required, MaxLength(4000)]
        public string Message { get; set; } = string.Empty;
    }


}