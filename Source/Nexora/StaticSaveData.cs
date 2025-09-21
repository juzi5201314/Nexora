using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Nexora;

public class StaticSaveData(World world) : WorldComponent(world)
{
    public static Pawn Worker;

    public static Job workJob;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref Worker, "Worker");
        Scribe_Deep.Look(ref workJob, "workJob");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Worker ??= PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
                canGeneratePawnRelations: false,
                allowAddictions: false, allowGay: false, allowFood: false, forceGenerateNewPawn: true,
                fixedBirthName: "",
                fixedLastName: ""));
            Worker.Name = new NameSingle("Nexora Worker");
            workJob ??= new Job(JobDefOf.DoBill);
        }
    }
}