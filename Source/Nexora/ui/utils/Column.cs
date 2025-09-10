using UnityEngine;
using Verse;

namespace Nexora.ui.utils;

public static class Column
{
    //public Rect InnerRect = rect;

    public static Rect GetLeft(ref this Rect rect, float width)
    {
        var r = rect with
        {
            width = width
        };
        rect.xMin += width;
        if (rect.width < 0)
        {
            Log.Warning("Column width underflow (left)");
        }

        return r;
    }

    public static Rect GetRight(ref this Rect rect, float width)
    {
        var r = rect with
        {
            x = rect.xMax - width,
            width = width
        };
        rect.xMax -= width;
        if (rect.width < 0)
        {
            Log.Warning("Column width underflow (right)");
        }

        return r;
    }

    public static Rect Remaining(ref this Rect rect)
    {
        var r = new Rect(rect.x, rect.y, rect.width, rect.height);
        rect = Rect.zero;
        return r;
    }

    public static void Gap(ref this Rect rect, float width = 10f)
    {
        rect.xMin += width;
    }
}