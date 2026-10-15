using System.Text.Json.Serialization;

namespace DIBA_School_Portal.Server.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcademicSection
    {
        Nursery,
        Primary,
        JuniorSecondary,
        SeniorSecondary,
        PreNursery
    }
}
