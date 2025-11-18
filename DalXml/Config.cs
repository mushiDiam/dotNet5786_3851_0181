using System;
using System.Globalization;
using System.Xml.Linq;

namespace Dal;
internal static class Config
{
    internal const string s_data_config_xml = "data-config.xml";
    internal const string s_couriers_xml = "couriers.xml";
    internal const string s_orders_xml = "orders.xml";
    internal const string s_deliveries_xml = "deliverys.xml";

    internal static int NextOrderId{
        //get => XMLTools.GetAndIncreaseConfigIntVal(s_data_config_xml, "NextOrderId");
        get => XMLTools.GetAndIncreaseConfigIntVal(s_data_config_xml, "NextOrderId");
        private set => XMLTools.SetConfigIntVal(s_data_config_xml, "NextOrderId", value);
    }
    internal static int NextDeliveryId{
        get => XMLTools.GetAndIncreaseConfigIntVal(s_data_config_xml, "NextDeliveryId");
        private set => XMLTools.SetConfigIntVal(s_data_config_xml, "NextDeliveryId", value);
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
    internal static DateTime Clock{
        get{
            var s = GetConfigStringVal(s_data_config_xml, "Clock");
            if (!string.IsNullOrWhiteSpace(s) &&
                DateTime.TryParseExact(s, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return DateTime.Now;
        }
        set => SetConfigStringVal(s_data_config_xml, "Clock", value.ToString("o"));
    }

    internal static int ManagerId{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToIntNullable("ManagerId") ?? 0;
        private set => SetConfigStringVal(s_data_config_xml, "ManagerId", value.ToString());
    }

    internal static string ManagerPassword{
        get{
            var s = GetConfigStringVal(s_data_config_xml, "ManagerPassword");
            return s ?? string.Empty;
        }
        private set => SetConfigStringVal(s_data_config_xml, "ManagerPassword", value ?? string.Empty);
    }

    internal static string? CompanyAddress{
        get => GetConfigStringVal(s_data_config_xml, "CompanyAddress");
        private set => SetConfigStringVal(s_data_config_xml, "CompanyAddress", value);
    }

    internal static double? CompanyLatitude{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("CompanyLatitude");
        private set => SetConfigStringVal(s_data_config_xml, "CompanyLatitude", value?.ToString(CultureInfo.InvariantCulture));
    }

    internal static double? CompanyLongitude{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("CompanyLongitude");
        private set => SetConfigStringVal(s_data_config_xml, "CompanyLongitude", value?.ToString(CultureInfo.InvariantCulture));
    }

    internal static double? MaxDeliveryDistance{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("MaxDeliveryDistance");
        private set => SetConfigStringVal(s_data_config_xml, "MaxDeliveryDistance", value?.ToString(CultureInfo.InvariantCulture));
    }

    internal static double AverageCarSpeed{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageCarSpeed") ?? 0.0;
        private set => SetConfigStringVal(s_data_config_xml, "AverageCarSpeed", value.ToString(CultureInfo.InvariantCulture));
    }

    internal static double AverageMotorcycleSpeed{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageMotorcycleSpeed") ?? 0.0;
        private set => SetConfigStringVal(s_data_config_xml, "AverageMotorcycleSpeed", value.ToString(CultureInfo.InvariantCulture));
    }

    internal static double AverageBikeSpeed{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageBikeSpeed") ?? 0.0;
        private set => SetConfigStringVal(s_data_config_xml, "AverageBikeSpeed", value.ToString(CultureInfo.InvariantCulture));
    }

    internal static double AverageWalkingSpeed{
        get => XMLTools.LoadListFromXMLElement(s_data_config_xml).ToDoubleNullable("AverageWalkingSpeed") ?? 0.0;
        private set => SetConfigStringVal(s_data_config_xml, "AverageWalkingSpeed", value.ToString(CultureInfo.InvariantCulture));
    }

    internal static TimeSpan MaxDeliveryTime{
        get{
            var s = GetConfigStringVal(s_data_config_xml, "MaxDeliveryTime");
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
                return ts;
            return TimeSpan.Zero;
        }
        private set => SetConfigStringVal(s_data_config_xml, "MaxDeliveryTime", value.ToString());
    }

    internal static TimeSpan RiskRange{
        get{
            var s = GetConfigStringVal(s_data_config_xml, "RiskRange");
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
                return ts;
            return TimeSpan.Zero;
        }
        private set => SetConfigStringVal(s_data_config_xml, "RiskRange", value.ToString());
    }

    internal static TimeSpan InactiveTime{
        get{
            var s = GetConfigStringVal(s_data_config_xml, "InactiveTime");
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
                return ts;
            return TimeSpan.Zero;
        }
        private set => SetConfigStringVal(s_data_config_xml, "InactiveTime", value.ToString());
    }
    internal static void Reset(){
        //NextDeliveryId = 1;
        // NextOrderId = 1;
        SetConfigStringVal(s_data_config_xml, "NextDeliveryId", "1");   //changed back to 1
        SetConfigStringVal(s_data_config_xml, "NextOrderId", "1");  //changed back to 1
        Clock = DateTime.Now;
        ManagerPassword = string.Empty;
    }
}
