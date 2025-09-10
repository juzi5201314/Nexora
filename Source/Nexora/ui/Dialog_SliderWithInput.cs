using HarmonyLib;
using Nexora.ui.utils;
using UnityEngine;
using Verse;

namespace Nexora.ui;

public class Dialog_SliderWithInput(
    Func<int, string> textGetter,
    int from,
    int to,
    Action<int> confirmAction,
    int startingValue = -2147483648,
    float roundTo = 1)
    : Dialog_Slider(textGetter, from, to, confirmAction, startingValue,
        roundTo)
{
    public override Vector2 InitialSize => new(300f, 160f);

    public override void DoWindowContents(Rect inRect)
    {
        base.DoWindowContents(inRect);
        var curValue = Traverse.Create(this).Field("curValue");
        var curValueInt = curValue.GetValue<int>();
        var curValueString = curValue.GetValue<int>().ToString();
        var size = Text.CalcSize(curValueString);
        var rect = new Rect(inRect.x + inRect.width / 2f - (size.x + 20f), inRect.yMax - 65f, size.x + 20f, size.y);
        using (Styles.TextAnchor(TextAnchor.MiddleCenter))
        {
            Widgets.TextFieldNumeric(rect, ref curValueInt, ref curValueString, int.MinValue, int.MaxValue);
        }

        curValue.SetValue(curValueInt);
    }
}