namespace DIBA_School_Portal.Server.DropDown
{
    public class FeeCreationMetadataDto
    {
        public List<SessionDropdownItemDto> Sessions { get; set; } = new();
        public List<SchoolClassDropdownItemDto> SchoolClasses { get; set; } = new();
        public List<EnumLookupDto> Sections { get; set; } = new();
        public List<EnumLookupDto> Streams { get; set; } = new();
    }
}
