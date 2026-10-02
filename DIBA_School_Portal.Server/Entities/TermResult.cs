using DIBA_School_Portal.Server.Configurations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class TermResult
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey(nameof(StudentId))]
        public ApplicationUser Student { get; set; } = null!;

        [Required]
        public Guid ClassId { get; set; }

        [ForeignKey(nameof(ClassId))]
        public SchoolClass SchoolClass { get; set; } = null!;

        [Required]
        public Guid TermId { get; set; }

        [ForeignKey(nameof(TermId))]
        public SchoolTerm Term { get; set; } = null!;

        [Required]
        public Guid AcademicSessionId { get; set; }

        [ForeignKey(nameof(AcademicSessionId))]
        public AcademicSession AcademicSession { get; set; } = null!;

        [Column(TypeName = "decimal(6,2)")]
        public decimal TotalMarksObtained { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal AverageScore { get; set; }

        [MaxLength(10)]
        public string OverallGrade { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TeacherRemark { get; set; } = string.Empty;

        [MaxLength(500)]
        public string PrincipalRemark { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = false;

        public ICollection<TermSubjectResult> SubjectResults { get; set; } = new List<TermSubjectResult>();

        
        [Required]
        public Guid EnrollmentId { get; set; }

        [ForeignKey(nameof(EnrollmentId))]
        public virtual StudentEnrollments Enrollment { get; set; } = null!;

        public int Position { get; set; }
    }
}