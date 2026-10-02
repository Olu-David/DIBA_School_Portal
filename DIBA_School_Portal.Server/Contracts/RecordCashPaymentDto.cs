namespace DIBA_School_Portal.Server.Contracts
{
    public class RecordCashPaymentDto
    {
        public Guid StudentProfileId { get; set; }
        public Guid AcademicSessionId { get; set; }
        public Guid TermId { get; set; }

        public decimal AmountPaid { get; set; }

        /// <summary>
        /// Optional external reference (teller slip, receipt book number).
        /// If empty, backend generates one.
        /// </summary>
        public string? ExternalReference { get; set; }

        /// <summary>
        /// Optional note e.g. "Father paid in person at bursary".
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Optional allocation — which fee(s) this cash is for.
        /// If empty, allocates to the tuition/school fee first.
        /// </summary>
        public List<CashPaymentItemDto>? Items { get; set; }

    }
}
