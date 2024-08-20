using app.vis;
using HarmonyLib;
using play.day.border;
using psp_papers_mod.Twitch;
using play.day;
using app;
using data;
using System.Threading.Tasks;
using psp_papers_mod.MonoBehaviour;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace psp_papers_mod.Patches;

//todo: raid end day, test day ending by itself, add alarm sounds to raid, fix gun locker  

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
        // invoke prevents crashes
        if (attackFunc != null) {
            UnityThreadInvoker.Invoke(() => {
                attackFunc();
            });
        }
            
        inProgress = true;
        EnableRifle();
        CheckEnemiesDead().SuccessWithUnityThread(() => { });
        DelayReadyToCall().SuccessWithUnityThread(() => { });
        
        Attackers.Clear();
        AliveAttackers.Clear();
        for (int i = 0; i < attackerCount; i++) {
            Chatter attacker = PapersPSP.Twitch.FrequentChatters.GetRandomChatter(true);
            Attackers.Enqueue(attacker);
            AliveAttackers.Enqueue(attacker);
        }
        AliveAttackers = Attackers;

        string isare = Attackers.Distinct().Count() == 1 ? " is" : " are";
        string message = AttackerNames(true) + isare + " attacking! MONKA";
        TwitchIntegration.Message(message);

    }

    public static void End() {
        inProgress = false;
        BorderPatch.Border.standDownRightGuards();
    }

    // todo: call after each day also reset other fields prolly 
    public static void Reset() {
        AllowAlarm = false;
        WallBlownUp = false;
        Attackers.Clear();
        AliveAttackers.Clear();

    }

    private static async Task DelayReadyToCall() {
        // calling this too early causes truck to not work
        await Task.Delay(2500);
        EnableRifle();
        BorderPatch.Border.stater.go("ready-to-call", true);
    }

    // todo: prevent attacks from affecting rifle in the first place
    public static void EnableRifle() {
        //gets reset internally by attacks for whatever reason
        BorderPatch.Border.set_snipingEnabled(true);
        BorderPatch.Border.killRifleButton.set_numBullets(999);
        BorderPatch.Border.tranqRifleButton.set_state(play.day.border.State.OFF);
    }

    public static void AttackerShot() {
        var attacker = AliveAttackers.Dequeue();
        PapersPSP.Log.LogWarning("SHOT ATTACKER: " + attacker.Username);
        attacker.Shot();
    }

    public static string AttackerNames(bool mention = false) {
        return string.Join(", ", Attackers.Distinct().Select(c => mention ? "@" + c.Username : c.Username));
    }

    
    private static async Task CheckEnemiesDead() {
        // enemies = ingame, attackers = chatters
        await Task.Delay(3000);
        while (true) {
            if (BorderPatch.Border.checkEnemiesDead()) {
                End();
                break;
            } 
            await Task.Delay(100);
        }
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

        UnityThreadInvoker.Invoke(() => {
            AllowPanicOnce = true;
            BorderPatch.Border.waitingLine.panic();
        });

        await Task.Delay(5000);

        UnityThreadInvoker.Invoke(() => {
            var person = BorderPatch.Border.addPerson("Man0", "", "");
            BorderPatch.Border.person_onEvent(person, "truck-blow-wall");
        });
    }

    //private static async Task LoopingAlarm() {
    //    while (inProgress) {
    //        PapersPSP.Log.LogWarning("juh");
    //        BorderPatch.Border.playAlarmSound();
    //        await Task.Delay(1000);
    //    }
    //}



    }



[HarmonyPatch(typeof(Border))]
public static class AttackBorderPatch {
    
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
public class AttackPersonPatch {

    [HarmonyPrefix]
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
public static class StaterPat {

    [HarmonyPrefix]
    [HarmonyPatch("go", typeof(string), typeof(Il2CppSystem.Object))]
    private static bool PreventBusy(string name) {
        // PapersPSP.Log.LogWarning(name);
        
        // Normally attacks only allow you to shoot and do nothing else
        if (name is "busyWithBorder" or "busy-with-border")
            return false;

        return true;
    }
}




