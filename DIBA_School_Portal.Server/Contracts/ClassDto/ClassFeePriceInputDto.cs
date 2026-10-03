namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class ClassFeePriceInputDto
    {
        public Guid SchoolClassId { get; set; }
        public decimal AmountForReturningStudents { get; set; }
        public decimal AmountForNewStudents { get; set; }
        public decimal MinimumAmountToPay { get; set; }
    }
}
