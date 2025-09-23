using System.Text;
using Nexora.buildings;
using Nexora.comp;
using Nexora.network;
using Nexora.ui.utils;
using Nexora.utils.pooled;
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

    private Action<List<Thing>> sortBy = l =>
        l.SortBy(thing => thing.LabelCap);

    private List<Thing> filteredItems = [];

    private QuickSearchFilter filter = new();

    public readonly IItemInterface ItemInterface = itemInterface;
    private bool Initialized = false;

    private static Texture2D webIcon = ContentFinder<Texture2D>.Get("UI/WebIcon");

    private static Vector2 Size = new(UI.screenWidth * 0.7f, UI.screenHeight * 0.7f);
    public override Vector2 InitialSize => Size;

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
            resizeable = true;
            ItemInterface.OnItemChanged += RefreshItemList;
        }

        using var fontSize = Styles.FontSize(GameFont.Small);
        var netPanel = inRect.RightPart(0.2f);
        var itemList = inRect.LeftPart(0.8f);
        var searchRect = itemList.TopPartPixels(Text.LineHeight * 1.5f);
        itemList.yMin += searchRect.height + 10f;
        Widgets.DrawLineHorizontal(itemList.x, itemList.y, itemList.width);
        itemList.yMin += 10f;
        DrawItemList(itemList);
        DrawNetPanel(netPanel.RightPartPixels(netPanel.width - 10f));

        using var _ = Styles.TextAnchor(TextAnchor.MiddleCenter);
        var sortButton = searchRect.RightPartPixels(Text.CalcSize("1").x * 10).ContractedBy(5f);
        searchRect.xMax -= sortButton.width + 10f;

        DrawSortButton(sortButton);
        var newSearch = Widgets.TextField(searchRect.ContractedBy(5f), search);
        if (newSearch != search)
        {
            search = newSearch;
            OnSearch();
        }

        TooltipHandler.TipRegion(searchRect,
            "Direct Search: Search by item name\nPrefix @: Search by mod name\nPrefix #: Search within item description");
    }

    private void DrawSortButton(Rect rect)
    {
        if (Widgets.ButtonText(rect, "SortBy"))
        {
            var options = new List<FloatMenuOption>
            {
                new("A-Z", () => { sortBy = list => list.SortBy(t => t.LabelCap); }),
                new("Mass",
                    () => { sortBy = list => list.SortBy(t => t.GetStatValue(StatDefOf.Mass) * t.stackCount); }),
                new("Unit Mass",
                    () => { sortBy = list => list.SortBy(t => t.GetStatValue(StatDefOf.Mass)); }),
                new("Value",
                    () => { sortBy = list => list.SortBy(t => t.GetStatValue(StatDefOf.MarketValue) * t.stackCount); }),
                new("Unit Value", () => { sortBy = list => list.SortBy(t => t.GetStatValue(StatDefOf.MarketValue)); }),
                new("Reverse", () =>
                {
                    var old = sortBy;
                    sortBy = list =>
                    {
                        old(list);
                        list.Reverse();
                    };
                }),
            };
            Find.WindowStack.Add(new FloatMenu(options)
            {
                onCloseCallback = RefreshItemList
            });
        }
    }

    private void DrawNetPanel(Rect rect)
    {
        using var textAnchor = Styles.TextAnchor(TextAnchor.MiddleCenter);
        var network = ItemInterface.Network();
        var listing = new Listing_Standard();
        listing.Begin(rect);
        listing.Gap(20f);

        if (listing.ButtonText("Auto organize"))
        {
            network.AutoOrganize();
        }

        listing.Gap(20f);

        DrawTextLine($"workrate: {network.UsedWorkrate} / {network.TotalWorkrate} ops");
        DrawTextLine($"devices: {network.DynWorkRates.Count} / {network.MaxDevices}");

        using var comps = network.Storages.OfType<ItemStorage>().Select(s => s.Owner.CompDataFormat)
            .OfType<CompDataFormatMassFormat>()
            .ToPooledList();
        DrawTextLine(
            $"mass: {comps.Select(c => c.Mass(((Building_LocalStorage)c.parent).Storage!)).Sum():F2} / {comps.Select(c => c.Props.Value).Sum()} kg",
            "This data only counts storage devices that use mass format.");

        listing.End();
        return;

        void DrawTextLine(string text, string? tooltip = null)
        {
            var inRect = listing.GetRect(Text.CalcHeight(text, listing.ColumnWidth));
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(inRect, tooltip);
            }

            Widgets.TextArea(inRect, text, true);
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
            if (item.Destroyed || item.stackCount == 0 || (item.holdingOwner is not ItemStorage or EmptyThingOwner &&
                                                           !ItemInterface.Contains(item)))
            {
                filteredItems.Remove(item);
                i -= 1;
                lastVisibleIndex -= 1;
                continue;
            }

            DrawItemRow(listing.GetRect(rowHeight), item);
        }

        listing.End();
        Widgets.EndScrollView();
    }

    private void DrawItemRow(Rect rect, Thing item2)
    {
        var item = item2.GetInnerIfMinified();
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        var webIconRect = rect.GetLeft(rect.height).ContractedBy(3f);
        DrawNetworkInfoTooltip(webIconRect, item2);

        TooltipHandler.TipRegion(rect,
            $"{GenLabel.ThingLabel(item, item.stackCount)}\n\n{item.def.description ?? ""}");

        var infoRect = rect.GetLeft(rect.height).ContractedBy(3f);
        rect.Gap(5f);
        var iconRect = rect.GetLeft(rect.height).ContractedBy(3f);
        rect.Gap();

        var size10 = Text.CalcSize("0123456789").x;
        var popRect = rect.GetRight(rect.height).ContractedBy(3f);
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
        DrawPopButton(popRect, item);
        Widgets.LabelEllipses(labelRect, label);
        if (Widgets.ButtonInvisible(labelRect))
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(item2);
            Find.CameraDriver.JumpToCurrentMapLoc(item2.PositionHeld);
        }
    }

    private void DrawNetworkInfoTooltip(Rect rect, Thing item)
    {
        var str = new StringBuilder();
        Thing? holder = null;
        if (item.holdingOwner.Owner is Thing thing)
        {
            str.AppendLine($"Stored in {thing.LabelCap}");
            holder = thing;
        }
        else if (item.holdingOwner is EmptyThingOwner owner)
        {
            str.AppendLine($"Stored in {owner.Storage.LabelCap}");
            holder = owner.Storage;
        }
        else
        {
            str.AppendLine("Stored in unknown");
        }

        if (holder != null && Widgets.ButtonInvisible(rect))
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(holder);
            Find.CameraDriver.JumpToCurrentMapLoc(holder.PositionHeld);
        }

        TooltipHandler.TipRegion(rect, str.ToTaggedString());
    }

    private void DrawPopButton(Rect rect, Thing item)
    {
        if (Widgets.ButtonImage(rect, Assets.Pop))
        {
            var inter = ItemInterface.Network().GetClosestAccessInterface(item.PositionHeld);
            if (inter == null)
            {
                Messages.Message("No reachable access interface found", MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                GenDrop.TryDropSpawn(item.SplitOff(item.stackCount), inter.Position, inter.Map, ThingPlaceMode.Near,
                    out _,
                    (thing, _) =>
                    {
                        Close();
                        CameraJumper.TryJump(thing);
                        Find.Selector.ClearSelection();
                        Find.Selector.Select(thing);
                    });
            }
        }

        TooltipHandler.TipRegion(rect, "Pop the item to the nearest access interface");
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

        foreach (var item in ItemInterface!.GetAllItems())
        {
            if (matchers.All(matcher => matcher(item)))
            {
                filteredItems.Add(item);
            }
        }

        sortBy(filteredItems);
    }

    private void OnSearch()
    {
        RefreshItemList();
    }

    public override void PostClose()
    {
        ItemInterface.OnItemChanged -= RefreshItemList;
        base.PostClose();
    }
}