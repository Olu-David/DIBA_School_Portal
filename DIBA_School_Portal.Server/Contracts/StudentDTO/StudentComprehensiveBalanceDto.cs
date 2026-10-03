namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentComprehensiveBalanceDto
    {
        public Guid StudentProfileId { get; set; }
        public Guid CurrentTermId { get; set; }
        public decimal PreviousArrears { get; set; }
        public decimal CurrentTermFee { get; set; }
        public decimal CurrentTermPaid { get; set; }
        public decimal CurrentTermBalance { get; set; }
        public decimal TotalBalanceDue { get; set; }
    }
}