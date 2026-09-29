using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolClassDTO
    {

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "JSS 1"
        public int DisplayOrder { get; set; }
        public AcademicSection AcademicSection { get; set; }
    }
}
