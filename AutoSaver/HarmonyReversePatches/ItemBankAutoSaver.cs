using BepInEx.Bootstrap;
using HarmonyLib;
using Marioalexsan.AutoSaver.SoftDependencies;
using System;
using System.Reflection.Emit;
using UnityEngine;

namespace Marioalexsan.AutoSaver.HarmonyReversePatches;

[HarmonyPatch(typeof(ProfileDataManager), nameof(ProfileDataManager.Save_ItemStorageData))]
static class ItemBankAutoSaver
{
    public static void TrySaveCurrentProfileToLocation(string location)
    {
        if (SaveDone)
        {
            AutoSaver.Plugin.Logger.LogInfo("Triggering item bank save process...");
        }

        SaveLocationOverride = location;
        BanksDone = 0;
        SaveDone = false;
        SaveProfileData(ProfileDataManager._current);
    }

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
    private static string? SaveLocationOverride; // Directory
    private static string? TempContents;

    private static int BanksDone = 0;
    private static int BanksMax = 0;
    internal static bool SaveDone { get; private set; } = true;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

    [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
    [HarmonyPriority(Priority.Last)]
    private static void SaveProfileData(ProfileDataManager __instance)
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> data)
        {
            var matcher = new CodeMatcher(data);

            // Strategy: everywhere the save is about to be saved, drop the input path and use our own

            int patchLocations = 0;

            while (true)
            {
                matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => File.WriteAllText(null, null)))
                    );

                if (matcher.IsInvalid)
                    break;

                patchLocations++;

                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(ItemBankAutoSaver), nameof(TempContents))),
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ItemBankAutoSaver), nameof(SaveLocationOverride))),
                    new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => MarkSaveDone(null!))),
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ItemBankAutoSaver), nameof(TempContents)))
                    );

                matcher.Advance(1);
            }

            const int expectedLocations = 3;

            if (patchLocations != expectedLocations)
            {
                AutoSaver.Plugin.Logger.LogWarning($"WARNING: ItemBankAutoSaver expected {expectedLocations} patch locations, got {patchLocations}.");
                AutoSaver.Plugin.Logger.LogWarning($"Either the vanilla code changed, or mods added extra stuff. This may or may not cause issues.");
            }

            BanksMax = expectedLocations;

            return matcher.InstructionEnumeration();
        }

        _ = Transpiler(null!);
        throw new NotImplementedException("Stub method");
    }

    private static string MarkSaveDone(string location)
    {
        BanksDone++;

        if (BanksDone >= BanksMax)
        {
            SaveDone = true;
        }

        return Path.Combine(location, $"itembank_{BanksDone - 1}");
    }

    public static void SaveExtraBankTabsToLocation(string location)
    {
        try
        {
            AutoSaver.Plugin.Logger.LogInfo($"Attempting to save MoreBankTabsFixed data...");

            var extraProfiles = MoreBankTabsFixed.GetExtraProfiles();
            var itemDatas = MoreBankTabsFixed.GetItemDatas();

            for (int i = 0; i < extraProfiles.Length; i++)
            {
                if (extraProfiles[i] == null)
                    continue;

                extraProfiles[i]._heldItemStorage = [.. itemDatas[i]];
                var serialized = JsonUtility.ToJson(extraProfiles[i], true);
                File.WriteAllText(Path.Combine(location, $"MoreBankTabsFixed_itemBank_{(i + 3)}"), serialized);
            }

            AutoSaver.Plugin.Logger.LogInfo($"MoreBankTabsFixed slots saved ({extraProfiles.Length} total).");
        }
        catch (Exception e)
        {
            AutoSaver.Plugin.Logger.LogError("Failed to save MoreBankTabsFixed info.");
            AutoSaver.Plugin.Logger.LogError($"Exception info: {e}");
        }
    }
}
