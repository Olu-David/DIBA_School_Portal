namespace DIBA_School_Portal.Server.Repository.Services
{
    public class BookItemBreakdownDto
    {
        public Guid ClassBookId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal  Price { get; set; }
        public bool IsPurchased { get; set; }
    }
}