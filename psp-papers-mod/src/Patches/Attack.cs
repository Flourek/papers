using app.vis;
using System;
using HarmonyLib;
using play.day.border;
using psp_papers_mod.Twitch;
using play.day.booth;
using play.day;
using play.ui;
using app;
using Il2CppSystem;
using Cpp2IL.Core.Extensions;
using System.Threading.Tasks;
using psp_papers_mod.MonoBehaviour;

namespace psp_papers_mod.Patches;


public class Attack {
    static bool inProgress = false;
    public static bool allowPanicOnce = false;
    public static bool allowAlarm = false;
    public static bool wallBlownUp = false;
    public static int chattersToTimeout = 0;
    private delegate void Delegate();

    private static void Start(Delegate attackFunc = null) {
        // invoke prevents crashes
        if (attackFunc != null) {
            UnityThreadInvoker.Invoke(() => {
                attackFunc();
            });
        }

        inProgress = true;
        enableRifle();
        CheckEnemiesDead().SuccessWithUnityThread(() => { });
        DelayReadyToCall().SuccessWithUnityThread(() => { });
    }

    public static void End() {
        inProgress = false;
        BorderPatch.Border.standDownRightGuards();
    }

    // todo: call after each day also reset other fields prolly 
    public static void Reset() {
        allowAlarm = false;
    }

    private static async Task DelayReadyToCall() {
        // enable calling next traveller
        // calling this too early causes truck to not work
        await Task.Delay(1000);
        BorderPatch.Border.stater.go("ready-to-call", true);
        enableRifle();
    }

    // todo: prevent attacks from affecting rifle in the first place (prevent state if attack.inProgress)
    public static void enableRifle() {
        //gets reset internally by attacks for whatever reason
        BorderPatch.Border.set_snipingEnabled(true);
        BorderPatch.Border.killRifleButton.set_numBullets(999);
        BorderPatch.Border.tranqRifleButton.set_state(play.day.border.State.OFF);
    }

    private static async Task CheckEnemiesDead() {
        await Task.Delay(3000);  // wait for all enemies to be on screen

        while (true) {
            
            int deadEnemies = 0;

            for (int i = 0; i < BorderPatch.Border.enemies.length; i++) {
                Person enemy = BorderPatch.Border.enemies[i].TryCast<Person>();
                if (enemy.isDead)
                    deadEnemies++;
            }

            if (deadEnemies >= BorderPatch.Border.enemies.length) {
                chattersToTimeout = deadEnemies;
                End();
                break;
            }
            await Task.Delay(500);
        }
    }

  

    public static void Bike() {
        Start(BorderPatch.Border.sendBikeAttack);
    }
    
    public static void BikeRunner() {
        Start(BorderPatch.Border.sendBikeRunner);
    }
    public static void Runner() {
        Start(BorderPatch.Border.sendRunner);
    }
    
    public static void Truck() {
        Start(BorderPatch.Border.sendTruck);
    }
    
    public static void Raid() {
        RushWallAsync().SuccessWithUnityThread(() => { });
        Start();
        allowAlarm = true;
        // todo: add a trigger for this to end game also maybe add alarm sounds
    }

    private static async Task RushWallAsync() {

        UnityThreadInvoker.Invoke(() => {
            allowPanicOnce = true;
            BorderPatch.Border.waitingLine.panic();
        });

        await Task.Delay(5000);

        UnityThreadInvoker.Invoke(() => {
            wallBlownUp = true;
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

//[HarmonyPatch(typeof(Booth))]
//public static class BoothEPatch {

//    [HarmonyPrefix]
//    [HarmonyPatch("shutterSwitch_onClick", typeof(Button))]
//    private static void egge(Button b) {
//        Attack.Truck();
//    }
//}



[HarmonyPatch(typeof(Border))]
public static class AttackBorderPatch {
    
    public static bool ThrewGrenade;

    [HarmonyPostfix]
    [HarmonyPatch("checkSniped", typeof(Person), typeof(PointData), typeof(string))]
    private static void SnipedPostfix(Person person, PointData pos, string shotAnim, ref bool __result) {
        if (!__result) return;
    }

    [HarmonyPrefix]
    [HarmonyPatch("panic")]
    private static bool AvoidPanic() {
        bool result = ThrewGrenade || Attack.allowPanicOnce;
        Attack.allowPanicOnce = false;
        return result;

    }

    [HarmonyPrefix]
    [HarmonyPatch("panicLeavingPersonRight")]
    private static bool AvoidSinglePanic() {
        bool result = ThrewGrenade || Attack.allowPanicOnce;
        Attack.allowPanicOnce = false;
        return result;
    }

    [HarmonyPrefix]
    [HarmonyPatch("playAlarmSound")]
    private static bool AvoidAlarm() {
        return Attack.allowAlarm;
    }

    [HarmonyPrefix]
    [HarmonyPatch("throwGrenade")]
    public static void ThrowGrenade() {
        ThrewGrenade = true;
        BorderPatch.Border.day.setAttackResultWithPriority(AttackResult.FAILED_DIDNOTHING);
    }


    [HarmonyPrefix]
    [HarmonyPatch("checkEnemiesDead")]
    private static bool egegemies(ref bool __result) {
        __result = true;
        return false;
    }

}




[HarmonyPatch(typeof(Stater))]
public static class StaterPat {

    // Normally attacks only allow you to shoot and do nothing else
    [HarmonyPrefix]
    [HarmonyPatch("go", typeof(string), typeof(Il2CppSystem.Object))]
    private static bool preventBusy(string name) {
        PapersPSP.Log.LogWarning(name);

        if (name == "busyWithBorder" || name == "busy-with-border")
            return false;

        return true;
    }
}




