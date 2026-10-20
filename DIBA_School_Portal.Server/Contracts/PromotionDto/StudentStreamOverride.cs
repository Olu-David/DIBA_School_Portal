using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.PromotionDto
{
    public class StudentStreamOverride
    {
        public Guid StudentId { get; set; }
        public ClassStream Stream { get; set; }
    }
}
