namespace DIBA_School_Portal.Server.DropDown
{
    public class SessionDropdownItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCurrentSession { get; set; }
        public List<TermDropdownItemDto> Terms { get; set; } = new();
    }
}
