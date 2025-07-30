using BepInEx;
using BepInEx.Bootstrap;
using System.Runtime.CompilerServices;
using FixedBankTabs;

namespace Marioalexsan.AutoSaver.SoftDependencies;

public static class MoreBankTabsFixed
{
    private const MethodImplOptions SoftDepend = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    // Bookkeeping

    public const string ModID = "MoreBankTabsFixed";
    public static readonly Version ExpectedVersion = new Version("1.0.0");

    public static bool IsAvailable
    {
        get
        {
            if (!_initialized)
            {
                _plugin = Chainloader.PluginInfos.TryGetValue(ModID, out PluginInfo info) ? info.Instance : null;
                _initialized = true;

                if (_plugin == null)
                {
                    Logging.LogWarning($"Soft dependency {ModID} was not found.");
                }
                else if (_plugin.Info.Metadata.Version != ExpectedVersion)
                {
                    Logging.LogWarning($"Soft dependency {ModID} has a different version than expected (have: {_plugin.Info.Metadata.Version}, expect: {ExpectedVersion}).");
                }
            }

            return _plugin != null;
        }
    }
    private static BaseUnityPlugin? _plugin;
    private static bool _initialized;

    // Implementation - all method calls must be marked with [MethodImpl(SoftDepend)] and must be guarded with a check to IsAvailable

    [MethodImpl(SoftDepend)]
    public static ItemStorage_Profile[] GetExtraProfiles() => BankPatches.newItemStorageProfiles;

    [MethodImpl(SoftDepend)]
    public static List<ItemData>[] GetItemDatas() => BankPatches.newItemDatas;
}
