using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Dal;
internal static class Config
{
    internal const string s_data_config_xml = "data-config.xml";
    internal const string s_couriers_xml = "couriers.xml";
    internal const string s_orders_xml = "orders.xml";
    internal const string s_deliveries_xml = "deliverys.xml";

    // --- Validation Helpers ---
    private static bool IsValidId(int id)
    {
        // ID must be between 100000000 and 999999999 (exactly 9 digits)
        return id >= 100000000 && id <= 999999999;
    }

    internal static int NextOrderId{
        //get => XMLTools.GetAndIncreaseConfigIntVal(s_data_config_xml, "NextOrderId");
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.GetAndIncreaseConfigIntVal(s_data_config_xml, "NextOrderId");
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => XMLTools.SetConfigIntVal(s_data_config_xml, "NextOrderId", value);
    }
    internal static int NextDeliveryId{
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.GetAndIncreaseConfigIntVal(s_data_config_xml, "NextDeliveryId");
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => XMLTools.SetConfigIntVal(s_data_config_xml, "NextDeliveryId", value);
    }

    // --- small helpers that mirror the missing Get/Set string helpers from the old XMLTools ---
    static string? GetConfigStringVal(string xmlFileName, string elemName){
        var root = XMLTools.LoadListFromXMLElement(xmlFileName);
        return (string?)root.Element(elemName);
    }

    static void SetConfigStringVal(string xmlFileName, string elemName, string? elemVal){
        var root = XMLTools.LoadListFromXMLElement(xmlFileName);
        var el = root.Element(elemName);
        if (el is null)
            root.Add(new XElement(elemName, elemVal ?? string.Empty));
        else
            el.SetValue(elemVal ?? string.Empty);
        XMLTools.SaveListToXMLElement(root, xmlFileName);
    }

    // Clock stored as ISO 8601 round-trip string ("o")
    internal static DateTime Clock
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            var s = GetConfigStringVal(s_data_config_xml, "Clock");
            if (!string.IsNullOrWhiteSpace(s) &&
                DateTime.TryParseExact(s, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return DateTime.Now;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => SetConfigStringVal(s_data_config_xml, "Clock", value.ToString("o"));
    }

    internal static int ManagerId
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToIntNullable("ManagerId") ?? 0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (!IsValidId(value))
                throw new DO.DalInvalidValueException("Manager ID must be exactly 9 digits");
            SetConfigStringVal(s_data_config_xml, "ManagerId", value.ToString());
        }
    }

    internal static string ManagerPassword
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            var s = GetConfigStringVal(s_data_config_xml, "ManagerPassword");
            return s ?? string.Empty;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DO.DalInvalidValueException("Manager password cannot be empty");
            SetConfigStringVal(s_data_config_xml, "ManagerPassword", value);
        }
    }

    internal static string? CompanyAddress
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => GetConfigStringVal(s_data_config_xml, "CompanyAddress");
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => SetConfigStringVal(s_data_config_xml, "CompanyAddress", value);
    }

    internal static double? CompanyLatitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("CompanyLatitude");
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => SetConfigStringVal(s_data_config_xml, "CompanyLatitude", value?.ToString(CultureInfo.InvariantCulture));
    }

    internal static double? CompanyLongitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("CompanyLongitude");
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => SetConfigStringVal(s_data_config_xml, "CompanyLongitude", value?.ToString(CultureInfo.InvariantCulture));
    }

    internal static double? MaxDeliveryDistance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("MaxDeliveryDistance");
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value.HasValue && value.Value < 0)
                throw new DO.DalInvalidValueException("Max delivery distance cannot be negative");
            SetConfigStringVal(s_data_config_xml, "MaxDeliveryDistance", value?.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static double AverageCarSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageCarSpeed") ?? 0.0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average car speed cannot be negative");
            SetConfigStringVal(s_data_config_xml, "AverageCarSpeed", value.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static double AverageMotorcycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageMotorcycleSpeed") ?? 0.0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average motorcycle speed cannot be negative");
            SetConfigStringVal(s_data_config_xml, "AverageMotorcycleSpeed", value.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static double AverageBikeSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageBikeSpeed") ?? 0.0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average bike speed cannot be negative");
            SetConfigStringVal(s_data_config_xml, "AverageBikeSpeed", value.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static double AverageWalkingSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageWalkingSpeed") ?? 0.0;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < 0)
                throw new DO.DalInvalidValueException("Average walking speed cannot be negative");
            SetConfigStringVal(s_data_config_xml, "AverageWalkingSpeed", value.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static TimeSpan MaxDeliveryTime
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            var s = GetConfigStringVal(s_data_config_xml, "MaxDeliveryTime");
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
                return ts;
            return TimeSpan.Zero;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < TimeSpan.Zero)
                throw new DO.DalInvalidValueException("Max delivery time cannot be negative");
            SetConfigStringVal(s_data_config_xml, "MaxDeliveryTime", value.ToString());
        }
    }

    internal static TimeSpan RiskRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            var s = GetConfigStringVal(s_data_config_xml, "RiskRange");
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
                return ts;
            return TimeSpan.Zero;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < TimeSpan.Zero)
                throw new DO.DalInvalidValueException("Risk range cannot be negative");
            SetConfigStringVal(s_data_config_xml, "RiskRange", value.ToString());
        }
    }

    internal static TimeSpan InactiveTime
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            var s = GetConfigStringVal(s_data_config_xml, "InactiveTime");
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
                return ts;
            return TimeSpan.Zero;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (value < TimeSpan.Zero)
                throw new DO.DalInvalidValueException("Inactive time cannot be negative");
            SetConfigStringVal(s_data_config_xml, "InactiveTime", value.ToString());
        }
    }
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void Reset(){
        SetConfigStringVal(s_data_config_xml, "NextDeliveryId", "1");
        SetConfigStringVal(s_data_config_xml, "NextOrderId", "1");
        Clock = DateTime.Now;
        SetConfigStringVal(s_data_config_xml, "ManagerPassword", "10"); // Reset to default password
    }
}
