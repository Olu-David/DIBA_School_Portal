using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class ClassSubjectResultDto
   
        {
            public Guid ClassId { get; set; }
            public string ClassName { get; set; } = "";
            public AcademicSection Section { get; set; }

            public Guid SubjectId { get; set; }
            public string SubjectName { get; set; } = "";
            public string SubjectCode { get; set; } = "";

            public Guid TermId { get; set; }
            public string TermName { get; set; } = "";
            public Guid AcademicSessionId { get; set; }
            public string AcademicSessionName { get; set; } = "";

            public string TeacherName { get; set; } = "";

            // "NotStarted" | "Draft" | "Submitted"
            public string SubmissionStatus { get; set; } = "NotStarted";

            public int StudentCount { get; set; }
            public int ScoredCount { get; set; }
            public decimal ClassAverage { get; set; }
            public decimal HighestScore { get; set; }
            public decimal LowestScore { get; set; }
            public int PassCount { get; set; }
            public int FailCount { get; set; }

            public List<ClassSubjectResultRowDto> Students { get; set; } = new();
        }

        public class ClassSubjectResultRowDto
        {
            public string StudentId { get; set; } = "";        // ApplicationUser.Id
            public string AdmissionNumber { get; set; } = "";
            public string StudentName { get; set; } = "";
            public string? ProfilePictureUrl { get; set; }

            public decimal Test1Score { get; set; }
            public decimal Test2Score { get; set; }
            public decimal ExamScore { get; set; }
            public decimal TotalScore { get; set; }
            public string Grade { get; set; } = "";
            public string Remark { get; set; } = "";
            public int SubjectPosition { get; set; }

            public bool IsDraft { get; set; }
        }
}

