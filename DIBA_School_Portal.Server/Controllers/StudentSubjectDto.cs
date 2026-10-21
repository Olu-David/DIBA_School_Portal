namespace DIBA_School_Portal.Server.Controllers
{
    internal class StudentSubjectDto
    {
        public Guid subjectId { get; set; }
        public string subjectName { get; set; }
        public string subjectCode { get; set; }
        public string teacherName { get; set; }
    }
}