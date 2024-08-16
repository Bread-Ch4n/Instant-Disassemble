﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using IAmFuture.Gameplay.Buildings;
using IAmFuture.MiniGames.DisassembleObjectMode;
using IAmFuture.ObjectPools;
using IAmFuture.UserInterface.MiniGames.DisassembleObject;
using UnityEngine;

namespace Instant_Disassemble;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static ManualLogSource _logger;

    [HarmonyPatch(typeof(DisassembleObjectGameMode))]
    class AutoDisassemblePatch
    {
        [HarmonyPatch(typeof(DisassembleObjectGameMode), "Begin")]
        static bool Prefix(DisassembleObjectGameMode __instance, DisassembleObject disassembleObject, GameObject actor)
        {
            AccessTools.Field(typeof(DisassembleObjectGameMode), "disassembleObject")
                .SetValue(__instance, disassembleObject);
            disassembleObject.Initialize();

            DisassembleObjectProgress currentProgression = disassembleObject.ActiveObjectProgress;
            AccessTools.Field(typeof(DisassembleObjectGameMode), "currentProgression")
                .SetValue(__instance, disassembleObject.ActiveObjectProgress);

            DisassembleRewardsTransfer transfer = (DisassembleRewardsTransfer)AccessTools.Field(typeof(DisassembleObjectGameMode), "transfer").GetValue(__instance);
            transfer.Initialize(disassembleObject, actor);
            transfer.DisablePossibilityToTransfer();

            Transform disassemblePoint = (Transform)AccessTools.Field(typeof(DisassembleObjectGameMode), "disassemblePoint").GetValue(__instance);

            DisassembleElementList elementList = ((IObjectPool)AccessTools.Field(typeof(DisassembleObjectGameMode), "objectPool").GetValue(__instance)).GetObject(currentProgression.GetDisassembleObjectPrefab(), disassemblePoint)
                .GetComponent<DisassembleElementList>();
            AccessTools.Property(typeof(DisassembleObjectGameMode), "ListInProcess")
                .SetValue(__instance, elementList);
            elementList.RestoreState(currentProgression.SavedProgress);

            AccessTools.Method(typeof(DisassembleObjectGameMode), "Subscribe").Invoke(__instance, null);
            ((GUI_ObjectRotationController)AccessTools.Field(typeof(DisassembleObjectGameMode), "rotationController")
                .GetValue(__instance)).TargetTransform = disassemblePoint;
            __instance.OnBegin.Invoke();

            foreach (DisassembleElementBase element in elementList.Elements)
            {
                AccessTools.Method(typeof(DisassembleElementList), "RecycleElement")
                    .Invoke(elementList, new object[] { element });
                element.Initialize(1f);
            }

            transfer.EnablePossibilityToTransfer();

            __instance.OnUpdated.Invoke();
            return false;
        }
    }


    private void Awake()
    {
        _logger = Logger;
        _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        var h = new Harmony("auto_disassemble");
        h.PatchAll();
    }
}