using System;
using System.Globalization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace TomorrowDAOServer.Common;

public static class TimeHelper
{
    public const string DefaultPattern = "yyyy-MM-dd HH:mm:ss";
    public const string DatePattern = "yyyy-MM-dd";
    public const string UtcPattern = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    public static DateTime GetDateTimeFromTimeStamp(long timeStamp)
    {
        DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return start.AddMilliseconds(timeStamp).ToUniversalTime();
    }

    public static long GetTimeStampInMilliseconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

    public static long GetTimeStampInSeconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
    }

    public static long GetTimeStampFromDateTime(DateTime dateTime)
    {
        TimeSpan timeSpan = dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)timeSpan.TotalMilliseconds;
    }

    public static long GetTimeStampFromDateTimeInSeconds(DateTime dateTime)
    {
        TimeSpan timeSpan = dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)timeSpan.TotalSeconds;
    }

    public static string ToUtcString(this DateTime dateTime, string pattern = null)
    {
        return dateTime.ToString(pattern ?? "o");
    }

    public static string ToUtc8String(this DateTime dateTime, string pattern = DefaultPattern)
    {
        return dateTime.ToZoneString(8, pattern);
    }

    public static DateTime WithSeconds(this DateTime dateTime, int seconds)
    {
        return dateTime.AddSeconds(-dateTime.Second).AddSeconds(seconds);
    }

    public static DateTime WithMilliSeconds(this DateTime dateTime, int millisecond)
    {
        return dateTime.AddMilliseconds(-dateTime.Millisecond).AddSeconds(millisecond);
    }

    public static DateTime WithMicroSeconds(this DateTime dateTime, int microSecond)
    {
        return dateTime.AddMicroseconds(-dateTime.Microsecond).AddSeconds(microSecond);
    }

    public static string ToZoneString(this DateTime dateTime, int zoneNo, string pattern = DefaultPattern)
    {
        var zone = zoneNo >= 0 ? "UTC+" + zoneNo : "UTC" + zoneNo;
        var utcZone = TimeZoneInfo.CreateCustomTimeZone(zone, TimeSpan.FromHours(zoneNo), zone, zone);
        var utcZoneTime = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Utc, utcZone);
        return utcZoneTime.ToString(pattern, CultureInfo.InvariantCulture);
    }

    public static DateTime? ParseFromUtc8(string dateTimeString, string pattern = DefaultPattern,
        DateTime? defaultDateTime = null)
    {
        try
        {
            return ParseFromZone(dateTimeString, 8, pattern);
        }
        catch (Exception)
        {
            return defaultDateTime;
        }
    }

    public static bool IsEmpty(this DateTime dateTime)
    {
        return dateTime == DateTime.MinValue;
    }

    public static DateTime DefaultIfEmpty(this DateTime dateTime, DateTime defaultValue)
    {
        return dateTime.IsEmpty() ? defaultValue : dateTime;
    }

    public static DateTime ParseFromZone(string dateTimeString, int utcOffset, string pattern = DefaultPattern)
    {
        var dateTime = DateTime.ParseExact(dateTimeString, pattern, CultureInfo.InvariantCulture);
        var customTimeZone = TimeZoneInfo.CreateCustomTimeZone($"UTC{utcOffset:+00;-00}", TimeSpan.FromHours(utcOffset),
            $"UTC{utcOffset:+00;-00}", $"UTC{utcOffset:+00;-00}");
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, customTimeZone);
        return utcDateTime;
    }

    public static DateTime ParseFromUtcString(string utcTimeString)
    {
        var match = Regex.Match(utcTimeString, @"(\.\d+)?Z$");
        if (!match.Success)
        {
            throw new FormatException("Invalid date time format.");
        }

        var fraction = match.Groups[1].Value;
        var pattern = "yyyy-MM-ddTHH:mm:ss" + fraction + "Z";

        return DateTime.ParseExact(utcTimeString, pattern, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal);
    }

    public static long ToUtcMilliSeconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    public static long ToUtcSeconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }
    
    public static string ConvertStrTimeToDate(string strTimeStamp)
    {
        if (string.IsNullOrWhiteSpace(strTimeStamp))
        {
            return string.Empty;
        }

        try
        {
            long timestamp = long.Parse(strTimeStamp);
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            return dateTime.ToString(DatePattern);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

}