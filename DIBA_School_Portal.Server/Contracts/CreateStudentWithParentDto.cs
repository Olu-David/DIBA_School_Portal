namespace DIBA_School_Portal.Server.Contracts
{
    public class CreateStudentWithParentDto
    {
        // Student
        public string StudentFirstName { get; set; } = string.Empty;
        public string StudentLastName { get; set; } = string.Empty;
        public Guid SchoolClassId { get; set; }

        // Parent
        public string ParentFirstName { get; set; } = string.Empty;
        public string ParentLastName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public string ParentRelationship { get; set; } = "Guardian";
        public bool ParentIsPrimaryContact { get; set; } = true;
    }
}
