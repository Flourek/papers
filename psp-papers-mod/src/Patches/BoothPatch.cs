using app.vis;
using System;
using HarmonyLib;
using play.day.border;
using psp_papers_mod.Twitch;
using play.day.booth;
using play.day;
using play.ui;
using data;
using Newtonsoft.Json;
using haxe;

namespace psp_papers_mod.Patches;

[HarmonyPatch(typeof(Booth))]
public static class BoothPatch {


    [HarmonyPostfix]
    [HarmonyPatch("deskItem_onMounted", typeof(DeskItem), typeof(bool))]
    private static void onMounted(DeskItem deskItem, bool mounted) {
        // if mounted the paper doesn't get deleted after traveler leaves

        PapersPSP.Log.LogInfo("mounted: " + deskItem.id);
        Paper paper = BorderPatch.Border.booth.autoFindPaper(deskItem.id);

        if (mounted) {
            paper.def.stay = Stay.DAY;
        } else {
            paper.def.stay = Stay.NONE;

        }

    }


}