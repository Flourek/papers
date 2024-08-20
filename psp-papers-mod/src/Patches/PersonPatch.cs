using data;
using HarmonyLib;
using play.day.border;
using psp_papers_mod.Twitch;
using System;
using System.Linq;
using Object = Il2CppSystem.Object;

namespace psp_papers_mod.Patches;

[HarmonyPatch(typeof(Person))]
public class PersonPatch {

    private static readonly string[] ExitPathIds = ["entrant-detainstart", "exit-right", "exit-left"];
    
    [HarmonyPostfix]
    [HarmonyPatch("setPath", typeof(string), typeof(Object), typeof(Object))]
    private static void PersonSetPath(string pathId, Object pathNumStops, Object delay, Person __result) {
        if (!ExitPathIds.Contains(pathId)) return;
        Chatter chatter = TwitchIntegration.ActiveChatter;
        if (chatter == null) return;

        TwitchIntegration.ChattersPerPerson.Add(__result.Pointer.ToInt64(), chatter);
        // Only path that doesnt clear activechatter
        if (pathId == "exit-right") TwitchIntegration.ActiveChatter = null;
    }
}