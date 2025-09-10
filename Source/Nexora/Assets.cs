using UnityEngine;
using Verse;

namespace Nexora;

[StaticConstructorOnStartup]
public static class Assets
{
    public static readonly Texture2D Priority;
    public static readonly Texture2D Terminal;
    public static readonly Texture2D MoveItem;

    static Assets()
    {
        var holder = LoadedModManager.GetMod<Nexora>().Content.GetContentHolder<Texture2D>();
        Priority = holder.Get("UI/Priority");
        Terminal = holder.Get("UI/Terminal");
        MoveItem = holder.Get("UI/MoveItem");
    }
}