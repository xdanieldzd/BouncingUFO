using System.Globalization;

namespace BouncingUFO
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildDateAttribute(string value) : Attribute
    {
        public readonly DateTime DateTime = DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}
