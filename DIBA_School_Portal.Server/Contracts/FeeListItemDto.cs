namespace DIBA_School_Portal.Server.Contracts
{
    public class FeeListItemDto
    {
        public Guid Id { get; set; }
        public string FeeName { get; set; } = "";
        public string? Description { get; set; }
        public Guid AcademicSessionId { get; set; }
        public Guid TermId { get; set; }
        public bool IsDeleted { get; set; }
        public List<FeeClassPriceDto> ClassPrices { get; set; } = new();
    }
}