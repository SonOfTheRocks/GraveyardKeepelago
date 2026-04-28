using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;
using UnityEngine;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.GameModifications.Patches
{
    public static class IntroUtilities
    {
        private static readonly List<KeyCode> SkipIntroKeys = new()
        {
            KeyCode.Space,
            KeyCode.Return,
            KeyCode.Escape,
            KeyCode.Mouse0,
            KeyCode.Joystick1Button0, // XBox: A
            KeyCode.Joystick1Button1, // XBox: B
            KeyCode.Joystick1Button7, // XBox: Start
        };

        public static void RegisterSkipKeys()
        {
            foreach (var skipIntroKey in SkipIntroKeys)
                KeyDispatcher.Register(skipIntroKey, PlayerUtilities.SkipIntro); 
        }

        public static void UnregisterSkipKeys()
        {
            foreach (var skipIntroKey in SkipIntroKeys)
                KeyDispatcher.Unregister(skipIntroKey);
        }
    }

    //public void Update()
    [HarmonyPatch(typeof(Intro), nameof(Intro.ShowIntro))]
    public class IntroShowIntroPatch : BasePatch
    {
        private static bool Prefix(MethodBase __originalMethod)
        {
            LogBefore(__originalMethod);

            IntroUtilities.RegisterSkipKeys();
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }

    [HarmonyPatch(typeof(Intro), nameof(Intro.OnIntroAnimationFinished))]
    public class IntroOnIntroAnimationFinishedPatch : BasePatch
    {
        private static bool Prefix(MethodBase __originalMethod)
        {
            LogBefore(__originalMethod);

            IntroUtilities.UnregisterSkipKeys();

            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}