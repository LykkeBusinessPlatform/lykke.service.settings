using System;

namespace AzureRepositories.Extensions
{
    internal static class DateTimeExtensions
    {
        internal static string StorageString(this DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
