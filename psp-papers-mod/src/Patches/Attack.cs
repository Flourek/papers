using app.vis;
using HarmonyLib;
using play.day.border;
using psp_papers_mod.Twitch;
using play.day;
using app;
using data;
using System.Threading.Tasks;
using psp_papers_mod.MonoBehaviour;
using psp_papers_mod.Utils;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace psp_papers_mod.Patches;


public class Attack {
    static bool inProgress = false;
    public static bool AllowPanicOnce = false;
    public static bool AllowAlarm = false;
    public static bool WallBlownUp = false;
    public static Queue<Chatter> Attackers = new();
    public static Queue<Chatter> AliveAttackers = new();
    private static int AttackersKilled = 0;
    private delegate void Delegate();

    private static void Start(Delegate attackFunc = null, int attackerCount = 0) {
        inProgress = true;

        // invoke prevents crashes
        if (attackFunc != null) {
            UnityThreadInvoker.Invoke(() => {
                attackFunc();
            });
        }
        
        PaperUtils.Delay(2500).SuccessWithUnityThread(() => {
            // calling this too early makes Truck not work.
            BorderPatch.Border.stater.go("ready-to-call", true);
            
            // only truck disables shooting
            if (attackFunc is { Method.Name: "sendTruck" }) {
                PapersPSP.Log.LogWarning("wah");
                EnableRifle();
            }
        });
        
        Attackers.Clear();
        AliveAttackers.Clear();
        for (int i = 0; i < attackerCount; i++) {
            Chatter attacker = PapersPSP.Twitch.FrequentChatters.GetRandomChatter(true);
            Attackers.Enqueue(attacker);
            AliveAttackers.Enqueue(attacker);
        }
        AliveAttackers = Attackers;

        string isAre = Attackers.Distinct().Count() == 1 ? " is" : " are";
        string message = AttackerNames(true) + isAre + " attacking! MONKA";
        TwitchIntegration.Message(message);

    }

    public static void End() {
        inProgress = false;
        BorderPatch.Border.standDownRightGuards();
    }

    // todo: call after each day also reset other fields prolly 
    public static void Reset() {
        inProgress = false;
        AllowAlarm = false;
        WallBlownUp = false;
        Attackers.Clear();
        AliveAttackers.Clear();
    }

    // todo: prevent attacks from affecting rifle in the first place
    public static void EnableRifle() {
        //gets reset internally by attacks for whatever reason
        BorderPatch.Border.set_snipingEnabled(true);
        BorderPatch.Border.killRifleButton.set_numBullets(999);
        BorderPatch.Border.tranqRifleButton.set_state(play.day.border.State.OFF);
    }

    public static void FadeToNight() {
        End();
        AttackBorderPatch.ThrewGrenade = true;
        BorderPatch.Border.stater.go("waiting-to-fade-to-night", true);
    }

    public static void AttackerShot() {
        var attacker = AliveAttackers.Dequeue();
        PapersPSP.Log.LogWarning("SHOT ATTACKER: " + attacker.Username);
        attacker.Shot();
        
        if (BorderPatch.Border.checkEnemiesDead()) End();
    }

    public static string AttackerNames(bool mention = false) {
        return string.Join(", ", Attackers.Distinct().Select(c => mention ? "@" + c.Username : c.Username));
    }
    
    public static void Bike() {
        Start(BorderPatch.Border.sendBikeAttack, 1);
    }
    
    public static void BikeRunner() {
        Start(BorderPatch.Border.sendBikeRunner, 3);
    }
    public static void Runner() {
        Start(BorderPatch.Border.sendRunner, 1);
    }
    
    public static void Truck() {
        Start(BorderPatch.Border.sendTruck, 2);
  
    }
    
    public static void Raid() {
        RushWallAsync().SuccessWithUnityThread(() => { });
        Start();
        AllowAlarm = true;
        // todo: add a trigger for this to end game also maybe add alarm sounds
    }

    private static async Task RushWallAsync() {
        inProgress = true;
        LoopingAlarm().SuccessWithUnityThread(() => {});

        UnityThreadInvoker.Invoke(() => {
            AllowPanicOnce = true;
            BorderPatch.Border.waitingLine.panic();
        });

        await Task.Delay(5000);

        
        UnityThreadInvoker.Invoke(() => {
            var person = BorderPatch.Border.addPerson("Man0", "", "");
            BorderPatch.Border.person_onEvent(person, "truck-blow-wall");
        });
        
        await Task.Delay(30000);
        
        FadeToNight();
    }

    private static async Task LoopingAlarm() {
        while (inProgress) {
            PapersPSP.Log.LogWarning("juh");
            PaperUtils.PlaySound("border-alarm");
            await Task.Delay(2000);
        }
    }



    }



[HarmonyPatch(typeof(Border))]
internal static class AttackBorderPatch {
    
    public static bool ThrewGrenade;

    [HarmonyPostfix]
    [HarmonyPatch("checkSniped", typeof(Person), typeof(PointData), typeof(string))]
    private static void SnipedPostfix(Person person, PointData pos, string shotAnim, ref bool __result) {
        if (!__result) return;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("person_onEvent", typeof(Person), typeof(string))]
    private static void OnWallBlowUp(Person person, string @event) {
        if (@event == "truck-blow-wall") 
            Attack.WallBlownUp = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("panic")]
    private static bool AvoidPanic() {
        bool result = ThrewGrenade || Attack.AllowPanicOnce;
        Attack.AllowPanicOnce = false;
        return result;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("panicLeavingPersonRight")]
    private static bool AvoidSinglePanic() {
        bool result = ThrewGrenade || Attack.AllowPanicOnce;
        Attack.AllowPanicOnce = false;
        return result;
    }

    [HarmonyPrefix]
    [HarmonyPatch("playAlarmSound")]
    private static bool AvoidAlarm() {
        return Attack.AllowAlarm;
    }

    [HarmonyPrefix]
    [HarmonyPatch("throwGrenade")]
    public static void ThrowGrenade() {
        ThrewGrenade = true;
        BorderPatch.Border.day.setAttackResultWithPriority(AttackResult.FAILED_DIDNOTHING);
    }

}

[HarmonyPatch(typeof(Person))]
internal class AttackPersonPatch {

    [HarmonyPostfix]
    [HarmonyPatch("setAnim", typeof(Anim), typeof(Object))]
    private static void PersonSetAnim(Anim anim, Object movingHorizontal, Person __instance) {
        if (!anim.death) return;
        
        if (__instance.id == "waiting" && Attack.WallBlownUp) {
            Chatter chatter = PapersPSP.Twitch.FrequentChatters.GetRandomChatter(false);
            chatter.Shot(); 
        }
        
        if (__instance.id == "runner" || __instance.id.StartsWith("enemy") || __instance.id.StartsWith("bike")) {
            Attack.AttackerShot();
        }
    }
}

[HarmonyPatch(typeof(Stater))]
internal static class StaterPat {

    [HarmonyPrefix]
    [HarmonyPatch("go", typeof(string), typeof(Il2CppSystem.Object))]
    private static bool PreventBusy(string name) {
        PapersPSP.Log.LogWarning(name);
        
        // Normally attacks only allow you to shoot and do nothing else
        if (name is "busyWithBorder" or "busy-with-border")
            return false;

        return true;
    }
}







