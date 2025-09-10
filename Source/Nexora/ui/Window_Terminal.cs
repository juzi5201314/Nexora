using System.Text;
using Nexora.network;
using Nexora.ui.utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nexora.ui;

[StaticConstructorOnStartup]
public class Window_Terminal(IItemInterface itemInterface) : Window
{
    private Vector2 scrollPosition = Vector2.zero;
    private string search = "";
    private List<Thing> filteredItems = [];

    private QuickSearchFilter filter = new();

    public IItemInterface ItemInterface = itemInterface;
    private bool Initialized = false;

    private static Texture2D webIcon = ContentFinder<Texture2D>.Get("UI/WebIcon");

    public override Vector2 InitialSize => new(UI.screenWidth * 0.7f, UI.screenHeight * 0.7f);

    public override void DoWindowContents(Rect inRect)
    {
        if (!Initialized)
        {
            if (ItemInterface is null)
            {
                Log.Error("No Nexora storage found");
                Close();
                return;
            }

            RefreshItemList();
            Initialized = true;
        }

        var itemList = inRect.LeftPart(0.8f);
        var searchRect = itemList.TopPartPixels(Text.LineHeight * 1.5f);
        itemList.yMin += searchRect.height + 10f;
        Widgets.DrawLineHorizontal(itemList.x, itemList.y, itemList.width);
        itemList.yMin += 10f;
        DrawItemList(itemList);

        using var _ = Styles.TextAnchor(TextAnchor.MiddleCenter);
        var newSearch = Widgets.TextField(searchRect.ContractedBy(5f), search);
        if (newSearch != search)
        {
            search = newSearch;
            OnSearch();
        }
    }

    private void DrawItemList(Rect rect)
    {
        var listing = new Listing_Standard();
        var rowHeight = Text.LineHeight * 1.5f;
        var viewRect =
            new Rect(0f, 0f, rect.width - 16f, filteredItems.Count * rowHeight);
        var firstVisibleIndex = Mathf.FloorToInt(scrollPosition.y / rowHeight);
        var lastVisibleIndex = Mathf.CeilToInt((scrollPosition.y + rect.height) / rowHeight);
        using var anchor = Styles.TextAnchor(TextAnchor.MiddleLeft);

        firstVisibleIndex = Math.Max(0, firstVisibleIndex);
        lastVisibleIndex = Math.Min(filteredItems.Count - 1, lastVisibleIndex);
        Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
        listing.Begin(viewRect);

        var topPadding = firstVisibleIndex * rowHeight;
        listing.Gap(topPadding);
        for (var i = firstVisibleIndex; i <= lastVisibleIndex; i++)
        {
            var item = filteredItems[i];
            DrawItemRow(listing.GetRect(rowHeight), item.GetInnerIfMinified());
        }

        listing.End();
        Widgets.EndScrollView();
    }

    private void DrawItemRow(Rect rect, Thing item)
    {
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        var webIconRect = rect.GetLeft(rect.height).ContractedBy(3f);
        DrawNetworkInfoTooltip(webIconRect, item);

        TooltipHandler.TipRegion(rect,
            $"{GenLabel.ThingLabel(item, item.stackCount)}\n\n{item.def.description ?? ""}");

        var infoRect = rect.GetLeft(rect.height).ContractedBy(3f);
        rect.Gap(5f);
        var iconRect = rect.GetLeft(rect.height).ContractedBy(3f);
        rect.Gap();

        var size10 = Text.CalcSize("0123456789").x;
        var markerValueRect = rect.GetRight(size10);
        var massRect = rect.GetRight(size10);
        var label = $"<b>{item.stackCount}</b> * {GenLabel.ThingLabel(item, 1)}";
        var labelRect = rect.Remaining();
        using var anchor = Styles.TextAnchor(TextAnchor.MiddleLeft);

        Widgets.ButtonImage(webIconRect, webIcon, GUI.color);
        Widgets.InfoCardButton(infoRect, item.def);
        if (item.def.DrawMatSingle is var mat && mat && mat.mainTexture && item is not Corpse { InnerPawn: null })
            Widgets.ThingIcon(iconRect, item);
        DrawMarketValue(markerValueRect, item);
        DrawMass(massRect, item);
        Widgets.LabelEllipses(labelRect, label);
    }

    private void DrawNetworkInfoTooltip(Rect rect, Thing item)
    {
        var str = new StringBuilder();
        if (item.holdingOwner is ItemStorage storage)
        {
            var pos = storage.Owner.Position;
            str.AppendLine($"Stored in: ({pos.x}, {pos.y})");
        }
        else
        {
            str.AppendLine("error: item not in any storage");
        }

        TooltipHandler.TipRegion(rect, str.ToTaggedString());
    }

    private static void DrawMarketValue(Rect rect, Thing item)
    {
        var val = item.GetStatValue(StatDefOf.MarketValue);
        using (Styles.TextAnchor(TextAnchor.MiddleLeft))
        using (Styles.WordWrap(false))
            Widgets.LabelEllipses(rect, (val * item.stackCount).ToStringMoney());
    }

    private static void DrawMass(Rect rect, Thing item)
    {
        var mass = item.GetStatValue(StatDefOf.Mass);
        using (Styles.GUIColor(TransferableOneWayWidget.ItemMassColor))
        using (Styles.TextAnchor(TextAnchor.MiddleLeft))
        using (Styles.WordWrap(false))
            Widgets.Label(rect, (mass * item.stackCount).ToStringMass());
    }

    private void RefreshItemList()
    {
        filteredItems.Clear();
        var searchText = search.ToLower().Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var matchers = new List<Func<Thing, bool>>();

        foreach (var s in searchText)
        {
            if (s.StartsWith("#"))
            {
                var desc = s.Substring(1);
                matchers.Add(thing =>
                {
                    filter.Text = desc;
                    return filter.Matches(thing.def.description);
                });
            }
            else if (s.StartsWith("@"))
            {
                var modname = s.Substring(1);
                matchers.Add(thing =>
                {
                    filter.Text = modname;
                    return filter.Matches(thing.def.modContentPack?.Name ?? "core");
                });
            }
            else
            {
                matchers.Add(thing =>
                {
                    filter.Text = s;
                    return filter.Matches(thing.def.label ?? "");
                });
            }
        }

        foreach (var item in ItemInterface.GetVirtualItems())
        {
            if (matchers.All(matcher => matcher(item)))
            {
                filteredItems.Add(item);
            }
        }

        filteredItems.SortBy(thing => TransferableUIUtility.DefaultListOrderPriority(thing.def));
    }

    private void OnSearch()
    {
        RefreshItemList();
    }
}