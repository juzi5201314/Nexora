using RimWorld;
using UnityEngine;
using Verse;

namespace Nexora.ui;

public class ThingFilterITab(ThingFilter filter) : ITab
{
    private ThingFilterUI.UIState thingFilterState = new();
    private static readonly Vector2 WinSize = new(300f, 480f);

    private ThingFilter filter = filter;

    protected override void FillTab()
    {
        var rect = new Rect(0.0f, 0.0f, WinSize.x, WinSize.y).ContractedBy(10f);
        Widgets.BeginGroup(rect);
        ThingFilterUI.DoThingFilterConfigWindow(rect, thingFilterState, filter, null, 8);
        Widgets.EndGroup();
    }
}