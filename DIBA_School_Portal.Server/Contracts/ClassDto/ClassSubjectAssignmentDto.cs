using DIBA_School_Portal.Server.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class ClassSubjectDTO
    {
       
        public Guid Id { get; set; } 

        public Guid ClassLevelId { get; set; }
        public Guid SubjectId { get; set; }

       

    }
}
