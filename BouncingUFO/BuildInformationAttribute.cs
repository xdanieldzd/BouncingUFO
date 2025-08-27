using System.Globalization;

namespace BouncingUFO
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BuildInformationAttribute(string dateTimeString, string userNameString, string machineNameString) : Attribute
    {
        public readonly DateTime DateTime = DateTime.ParseExact(dateTimeString, "o", CultureInfo.InvariantCulture, DateTimeStyles.None);
        public readonly string UserName = userNameString;
        public readonly string MachineName = machineNameString;
    }
}
