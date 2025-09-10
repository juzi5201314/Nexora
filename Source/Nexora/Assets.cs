using UnityEngine;
using Verse;

namespace Nexora;

[StaticConstructorOnStartup]
public static class Assets
{
    public static readonly Texture2D Priority;
    public static readonly Texture2D Terminal;
    public static readonly Texture2D MoveItem;
    public static readonly Texture2D Input;
    public static readonly Texture2D Output;

    static Assets()
    {
        var holder = LoadedModManager.GetMod<Nexora>().Content.GetContentHolder<Texture2D>();
        Priority = holder.Get("UI/Priority");
        Terminal = holder.Get("UI/Terminal");
        MoveItem = holder.Get("UI/MoveItem");
        Input = holder.Get("UI/Input");
        Output = holder.Get("UI/Output");
    }
}