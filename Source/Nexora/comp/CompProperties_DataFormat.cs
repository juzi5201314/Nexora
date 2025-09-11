using RimWorld;
using Verse;

namespace Nexora.comp;

public class CompProperties_DataFormat : CompProperties
{
    public string value;

    public CompProperties_DataFormat()
    {
    }

    private decimal? ValueInt;

    public decimal Value
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
                var multiplier = 1;
                if (str.EndsWith("k"))
                {
                    multiplier = 1000;
                    str = str.Substring(0, str.Length - 1);
                }
                else if (str.EndsWith("m"))
                {
                    multiplier = 1000000;
                    str = str.Substring(0, str.Length - 1);
                }
                else if (str.EndsWith("b"))
                {
                    multiplier = 1000000000;
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
}