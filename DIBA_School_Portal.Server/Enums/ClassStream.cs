using System.Text.Json.Serialization;

namespace DIBA_School_Portal.Server.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClassStream
    {
        Science,     // SS1-3 Science
        Arts,        // SS1-3 Arts / Humanities
        Commercial   // SS1-3 Commercial
    }
}
