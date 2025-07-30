using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace Marioalexsan.AutoSaver.HarmonyPatches;

[HarmonyPatch]
static class OptionsMenuCell_SaveFile_Routine
{
    // This is a state machine, mind the hidden implementation
    static MethodInfo TargetMethod() => typeof(OptionsMenuCell).Assembly.DefinedTypes.First(x => x.Name.Contains("SaveFile_Routine")).DeclaredMethods.First(x => x.Name == "MoveNext");

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
    {
        var matcher = new CodeMatcher(code);

        matcher.MatchForward(false, new CodeMatch((ins) => ins.Calls(AccessTools.Method(typeof(ProfileDataManager), nameof(ProfileDataManager.Save_ProfileData)))));

        if (matcher.IsInvalid)
        {
            AutoSaver.Plugin.Logger.LogWarning($"WARNING: OptionsMenuCell_SaveFile_Routine couldn't find call to Save_ProfileData!");
            AutoSaver.Plugin.Logger.LogWarning($"Either the vanilla code changed, or mods added extra stuff. Backing up on manual saves will not work due to this.");
            return code;
        }

        matcher.Advance(1);

        var labels = matcher.Instruction.ExtractLabels(); // Move labels just in case

        matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OptionsMenuCell_SaveFile_Routine), nameof(ManualSaveTriggered))).WithLabels(labels));

        return matcher.InstructionEnumeration();
    }

    static void ManualSaveTriggered()
    {
        AutoSaver.Plugin.ManualSaveTriggered();
    }
}
