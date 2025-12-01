namespace Helpers;

static internal class Tools
{
    public static string ToStringProperty<T>(T obj)
    {
        var properties = typeof(T).GetProperties();
        var result = new System.Text.StringBuilder();
        result.AppendLine($"{typeof(T).Name} Properties:");
        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj, null);
            result.AppendLine($"- {prop.Name}: {value}");
        }
        return result.ToString();
    }
}
