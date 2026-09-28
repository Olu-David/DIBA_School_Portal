using System.Text.Json.Serialization;

namespace DIBA_School_Portal.Server.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClassStream
    {
        General,     // Used for Nursery, Primary, and JSS (no split yet)
        Science,     // SS1-3 Science
        Arts,        // SS1-3 Arts / Humanities
        Commercial   // SS1-3 Commercial
    }
}
