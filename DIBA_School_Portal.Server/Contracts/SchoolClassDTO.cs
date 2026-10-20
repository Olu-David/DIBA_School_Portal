using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolClassDTO
    {

        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public AcademicSection AcademicSection { get; set; }
        public int DisplayOrder { get; set; }
        public int StudentCount { get; set; }
        public Guid? NextClassId { get;  set; }
        public string? NextClassName { get;  set; }
    }
}
