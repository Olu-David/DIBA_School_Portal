using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.DropDown
{
    public class SchoolClassDropdownItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AcademicSection Section { get; set; }
    }
}
