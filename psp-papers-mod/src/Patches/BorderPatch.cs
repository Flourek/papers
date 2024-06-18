using app.vis;
using System;
using HarmonyLib;
using play.day.border;
using psp_papers_mod.Twitch;

namespace psp_papers_mod.Patches;

[HarmonyPatch(typeof(Border))]
public static class BorderPatch {

    public static Border Border;

    [HarmonyPrefix]
    [HarmonyPatch("panic")]
    private static bool AvoidPanic() {
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("panicLeavingPersonRight")]
    private static bool AvoidSinglePanic() {
        return false;
    }

    public static void SendChatterRunner() {
        Chatter chatter = PapersPSP.Twitch.FrequentChatters.GetRandomChatter(true);
        TwitchIntegration.SetActiveAttacker(chatter);
        Border.sendRunner();
    }

    [HarmonyPrefix]
    [HarmonyPatch("checkSniped", typeof(Person), typeof(PointData), typeof(string))]
    private static bool SnipedPrefix(Person person, PointData pos, string shotAnim, ref bool __result) {
        if (person.id != "waiting" && person.id != "guard0" && person.id != "guard1") return true;

        // Killing the people in queue breaks the game
        // So does killing the two left guards during detains
        __result = false;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch("checkSniped", typeof(Person), typeof(PointData), typeof(string))]
    private static void SnipedPostfix(Person person, PointData pos, string shotAnim, ref bool __result) {
        if (!__result) return;

        Console.Out.WriteLine("SNIPED " + person.id + " SHOTANIM: " + shotAnim);
    }
    
}