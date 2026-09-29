namespace DIBA_School_Portal.Server.DropDown
{
    public class TermDropdownItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCurrentTerm { get; set; }
    }
}
