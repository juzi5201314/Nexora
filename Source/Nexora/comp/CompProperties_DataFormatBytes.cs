using RimWorld;
using Verse;

namespace Nexora.comp;

public class CompProperties_DataFormatBytes : CompProperties_DataFormat
{
    public string? value;

    public CompProperties_DataFormatBytes()
    {
    }

    private decimal? ValueInt;

    public override decimal Value
    {
        get
        {
            if (ValueInt is null)
            {
                if (value.NullOrEmpty())
                {
                    ValueInt = 0;
                    return 0;
                }

                var str = value.Trim().ToLowerInvariant();
                var multiplier = 1m;
                if (str.EndsWith("tb"))
                {
                    multiplier = 1099511627776m; // 10^12
                    str = str.Substring(0, str.Length - 2);
                }
                else if (str.EndsWith("gb"))
                {
                    multiplier = 1073741824m; // 10^9
                    str = str.Substring(0, str.Length - 2);
                }
                else if (str.EndsWith("mb"))
                {
                    multiplier = 1048576m; // 10^6
                    str = str.Substring(0, str.Length - 2);
                }
                else if (str.EndsWith("kb"))
                {
                    multiplier = 1024m; // 10^3
                    str = str.Substring(0, str.Length - 2);
                }
                else if (str.EndsWith("b"))
                {
                    multiplier = 1m;
                    str = str.Substring(0, str.Length - 1);
                }

                if (decimal.TryParse(str, out var numericValue))
                {
                    ValueInt = numericValue * multiplier;
                }
                else
                {
                    Log.Error($"Invalid data format numeric value: {value}.");
                    ValueInt = 0;
                }
            }

            return (decimal)ValueInt!;
        }
    }

    public string ToString(decimal bytes)
    {
        var units = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        var unitIndex = 0;
        var val = bytes;

        while (val >= 1024 && unitIndex < units.Length - 1)
        {
            val /= 1024;
            unitIndex++;
        }

        return $"{val:0.##}{units[unitIndex]}";
    }
}