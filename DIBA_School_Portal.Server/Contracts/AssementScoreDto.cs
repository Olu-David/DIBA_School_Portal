using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class AssementScoreDto
    {
        [Required]
        public Guid AssessmentId { get; set; } // e.g., ID for "Test 1"

        [Range(0, 1000, ErrorMessage = "Score cannot be negative.")]
        public decimal ScoreObtained { get; set; } // e.g., 18.5
    }
}
