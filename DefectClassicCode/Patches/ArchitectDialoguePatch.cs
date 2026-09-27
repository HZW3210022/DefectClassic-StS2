using System;
using System.Collections.Generic;
using DefectClassic.DefectClassicCode.Character;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace DefectClassic.DefectClassicCode.Patches;

/// <summary>
/// 把「建筑师」与经典故障机器人的对话注入 "ancients" 本地化表。
///
/// <para>
/// <b>为什么不放在 <c>localization/ancients.json</c>：</b>
/// 本 mod 的 <c>.json</c> **进不了导出的 <c>.pck</c>**。Godot 只导出"资源"，
/// 而没有 <c>.import</c> 的 <c>.json</c> 既不算资源、<c>include_filter</c> 也捞不动它
/// —— <c>export_filter</c> 的 <c>all_resources</c> / <c>all_files</c>、
/// <c>include_filter</c> 的 <c>*.json</c> / 明确目录 / <c>res://</c> 前缀，
/// 全都实测过，导出的 pck 大小与条目数分毫未变。而游戏只经 <c>res://</c> 读本地化表，
/// 所以那些 json 在运行时形同废纸。
/// </para>
/// <para>
/// （<c>localization/*.json</c> 仍然保留：ModAnalyzers 的 <c>STS001</c> 会在**编译期**检查它们存在，
/// 删掉就编不过。它们同时也是 Godot 编辑器导出时的正确来源。）
/// </para>
/// <para>
/// 这里沿用 BaseLib 自己在 <c>ModelLocPatch</c> 里的手法：取出 <c>LocTable</c> 的私有
/// <c>_translations</c> 字典后直接写键，绕过文件加载。
/// 键前缀由 <c>BaseLib.Utils.AncientDialogueUtil.BaseLocKey</c> 决定：<c>{古代者ID}.talk.{角色ID}.</c>
/// </para>
/// </summary>
[HarmonyPatch(typeof(ModelDb), "Init")]
internal static class ArchitectDialoguePatch
{
    private const string CharacterEntry = "DEFECTCLASSIC-DEFECT_CLASSIC";

    private static readonly string Prefix = "THE_ARCHITECT.talk." + CharacterEntry + ".";

    /// <summary>后缀按 BaseLib 的 <c>GetDialoguesForKey</c> 探测顺序排列。</summary>
    private static readonly string[] Suffixes =
    [
        "0-0r.char",
        "0-0r.next",
        "0-1r.ancient",
        "0-attack",
    ];

    private static readonly string[] Chinese =
    [
        "我没有问题。只有还没被解答过的问题。",
        "继续",
        "那你就继续往上爬，继续找不到答案。",
        "这不是我能计算的问题。",
    ];

    private static readonly string[] English =
    [
        "I have no questions. Only questions that were never answered.",
        "Continue",
        "Then you will keep climbing, and keep finding no answer.",
        "This is not a question I am able to compute.",
    ];

    [HarmonyPostfix]
    private static void InjectArchitectDialogue()
    {
        // 这段失败只意味着建筑师对话缺文本，绝不能连累 mod 其它部分，所以整体兜住。
        try
        {
            LocTable? table = LocManager.Instance.GetTable("ancients");
            if (table is null)
            {
                MainFile.Logger.Warn($"[{MainFile.ModId}] 取不到 ancients 本地化表，建筑师对话将缺失。");
                return;
            }

            if (AccessTools.Field(typeof(LocTable), "_translations")?.GetValue(table)
                is not Dictionary<string, string> translations)
            {
                MainFile.Logger.Warn($"[{MainFile.ModId}] 取不到 ancients 表的字典，建筑师对话将缺失。");
                return;
            }

            bool zh = LocManager.Instance.Language == "zhs";
            string[] texts = zh ? Chinese : English;
            for (int i = 0; i < Suffixes.Length; i++)
            {
                translations[Prefix + Suffixes[i]] = texts[i];
            }

            MainFile.Logger.Info($"[{MainFile.ModId}] 建筑师对话已注入（{Suffixes.Length} 条，语言={LocManager.Instance.Language}）。");
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] 建筑师对话注入失败，将缺失：{e}");
        }
    }
}
