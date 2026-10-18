using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class ClassSubjectAssignmentDto
    {
       
        public Guid Id { get; set; }

        public Guid SubjectId { get; set; }
        public ClassStream? Stream { get; set; }



    }
}
