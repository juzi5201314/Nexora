using Nexora.network;
using RimWorld;
using Verse;

namespace Nexora.ui;

public class MainButtonWorker_OpenGlobal : MainButtonWorker
{
    public override void Activate()
    {
        if (Find.WindowStack.IsOpen<Window_Terminal>())
        {
            Find.WindowStack.TryRemove(typeof(Window_Terminal));
        }
        else
        {
            Find.WindowStack.Add(new Window_Terminal(Find.CurrentMap.GetComponent<LocalNetwork>()));
        }
    }
}