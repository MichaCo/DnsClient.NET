using System;
using System.Text;

namespace DnsClient
{
    /// <summary>
    /// Extension method for <see cref="DateTime"/>
    /// </summary>
    public static class MyDateTimeExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToRrsigDateString(this DateTime dateTime)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(dateTime.Year);
            stringBuilder.Append(dateTime.Month);
            stringBuilder.Append(dateTime.Day);
            stringBuilder.Append(dateTime.Hour);
            stringBuilder.Append(dateTime.Minute);
            stringBuilder.Append(dateTime.Second);

            return stringBuilder.ToString();
        }
    }
}
