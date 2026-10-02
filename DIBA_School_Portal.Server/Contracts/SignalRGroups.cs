namespace DIBA_School_Portal.Server.Contracts
{
    public static class SignalRGroups
    {
        // Individual roles (Pluralized strings to match SignalR group conventions)
        public const string SuperAdmins = "SuperAdmins";
        public const string Principals = "Principals";
        public const string SectionHeads = "SectionHeads";
        public const string VicePrincipals = "VicePrincipals";
        public const string SchoolAdmins = "SchoolAdmins";
        public const string Finance = "Finance";             // Finance/Accountant is usually a department/group name
        public const string Registrars = "Registrars";
        public const string Teachers = "Teachers";
        public const string FormTeachers = "FormTeachers";

        // Composite groups
        public const string Leadership = "Leadership";
        public const string AllAdmins = "AllAdmins";
        public const string AllStaff = "AllStaff";

        // Personal
        public static string User(string userId) => $"user-{userId}";
    }
}