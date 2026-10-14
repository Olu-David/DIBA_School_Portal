using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Database_Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class TermSubjectResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TermResultId { get; set; }

        [ForeignKey(nameof(TermResultId))]
        public TermResult TermResult { get; set; } = null!;

        [Required]
        public Guid SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject Subject { get; set; } = null!;

        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [ForeignKey(nameof(TeacherId))]
        public ApplicationUser Teacher { get; set; } = null!;

        // Assessment columns entered by the teacher
        [Column(TypeName = "decimal(5,2)")]
        public decimal Test1Score { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Test2Score { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ExamScore { get; set; }

        // Automatically computed fields
        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalScore { get; set; }

        public int SubjectPosition { get; set; }

        [MaxLength(5)]
        public string Grade { get; set; } = string.Empty;

        public bool IsDraft { get; set; } = true;

        [MaxLength(100)]
        public string Remark { get; set; } = string.Empty;
        public DateTime CreatedAt { get;  set; }
    }
}