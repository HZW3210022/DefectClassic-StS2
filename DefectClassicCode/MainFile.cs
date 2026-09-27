using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace DefectClassic.DefectClassicCode;

// 建议把全部代码放在这个命名空间下，全部素材放在 DefectClassic 文件夹下。
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "DefectClassic"; // 用于拼资源路径
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 若要让你自己定义的 Godot 场景使用本 mod 的脚本，取消下一行注释。
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);

        Harmony harmony = new(ModId);

        // 打了补丁就不能因为单个补丁失败而整个 mod 起不来，
        // 所以这里兜住异常：失败的补丁会退化成原版行为，mod 本体照常加载。
        try
        {
            harmony.PatchAll(assembly);
            Logger.Info($"[{ModId}] Harmony 补丁已应用。");
        }
        catch (Exception e)
        {
            Logger.Warn($"[{ModId}] 部分 Harmony 补丁未能应用，相关效果将退化为原版行为：{e}");
        }
    }
}
