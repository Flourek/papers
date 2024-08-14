using app.ent;
using app.vis;
using data;
using HarmonyLib;
using Il2CppSystem;
using play.day;
using play.day.booth;

namespace psp_papers_mod.Patches;

[HarmonyPatch(typeof(Paper))]
public class PaperPatch {

    [HarmonyPostfix]
    [HarmonyPatch("__hx_ctor_play_day_booth_Paper", typeof(Paper), typeof(Ent), typeof(Filer), typeof(PaperDef), typeof(BoothEnv), typeof(int), typeof(TouchGlows), typeof(Object), typeof(Carousel))]
    static void CtorPostfix(PaperDef def_, int multiPaperIndex_) {
        Console.Out.WriteLine("PAPER!!! " + def_.id + " : " + def_.outerImageName  + " : " + def_.mountImageName  + " : " + multiPaperIndex_);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("playSoundTurnPage")]
    static void eeee(Paper __instance) {

        if (__instance.idWithIndex == "Case") {
            //PapersPSP.Log.LogWarning("buh " + __instance.idWithIndex);
            //BoothEnginePatch.GivePaperNow("emoteBlank");
            //Paper paper = BorderPatch.Border.booth.autoFindPaper("emoteBlank");
            //paper.def.reveal = new Reveal_ONDESK(120, 2);
            //string newId = "droplule";
            //paper.idWithIndex = newId;
            //paper.deskItem.id = newId;
            //paper.deskItem.idWithIndex = newId;
            //var point = new app.vis.PointData(0, 0);
            //paper.deskItem.startRevealAnim(point);
        }

    }

    [HarmonyPostfix]
    [HarmonyPatch("onClick", typeof(Visual))]
    static void coinFlip(Paper __instance, Visual hitVisual) {

        if (__instance.idWithIndex == "Coin") {
            PapersPSP.Log.LogWarning("omajga");
        }

    }

}