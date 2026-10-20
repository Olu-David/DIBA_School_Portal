using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Entities
{
    public class PromotionRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StudentId { get; set; }
        public virtual Student? Student { get; set; }

        public Guid FromClassId { get; set; }
        public virtual SchoolClass? FromClass { get; set; }

        /// <summary>Null when the student graduated.</summary>
        public Guid? ToClassId { get; set; }
        public virtual SchoolClass? ToClass { get; set; }

        public Guid AcademicSessionId { get; set; }
        public virtual AcademicSession? AcademicSession { get; set; }

        /// <summary>Stream at the time of promotion (relevant for JSS3 → SS1).</summary>
        public ClassStream? Stream { get; set; }

        public DateTime PromotedAt { get; set; }
        public string? PerformedByUserId { get; set; }

        /// <summary>Groups all records from a single promotion run.</summary>
        public Guid BatchId { get; set; }

        public string? Notes { get; set; }

    }
}
