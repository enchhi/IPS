using System.Globalization;

namespace IPS.Processing;

public static class PayloadParser
{
    // parsira "key:value,key:value" u dictionary
    public static Dictionary<string, string> Parse(string payload)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in payload.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = segment.IndexOf(':');
            if (colon <= 0 || colon >= segment.Length - 1)
                throw new FormatException("Bad payload segment: " + segment);

            var key = segment.Substring(0, colon).Trim();
            var value = segment.Substring(colon + 1).Trim();
            if (key.Length == 0 || value.Length == 0)
                throw new FormatException("Bad payload segment: " + segment);

            result[key] = value;
        }
        return result;
    }

    public static int ReadInt(Dictionary<string, string> fields, string key)
    {
        if (!fields.TryGetValue(key, out var raw))
            throw new FormatException("Missing key: " + key);

        var clean = raw.Replace("_", "").Replace(" ", "");
        return int.Parse(clean, CultureInfo.InvariantCulture);
    }
}
