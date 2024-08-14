using HarmonyLib;
using play.day;
using play.day.booth;
using psp_papers_mod.Patches;
using psp_papers_mod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace psp_papers_mod.src.Patches;


[HarmonyPatch(typeof(Booth))]
internal class AnimationPatch {


    static float fps = 0.5f;

    static float dT = 0.016f;
    static float interval = 1 / fps;        
    static float anim_time = 0;


    [HarmonyPostfix]
    [HarmonyPatch("draw")]
    private static void draw() {

        //anim_time += dT;
        ////PapersPSP.Log.LogWarning(anim_time + " " + frametime);

        //if (anim_time > interval) {
        //    anim_time = 0;

        //    var paper = BorderPatch.Border.booth.autoFindPaper("Coin");
        //    if (paper != null) {
        //        debug.dump(paper.deskItem.visuals[0]);
        //        int index = (paper.get_page() + 1) % 8;
        //        paper.set_page(index);
        //    }
        //}

    }

}
