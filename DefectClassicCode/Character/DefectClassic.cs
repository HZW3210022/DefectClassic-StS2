using BaseLib.Abstracts;
using DefectClassic.DefectClassicCode.Cards;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace DefectClassic.DefectClassicCode.Character;

/// <summary>
/// 一代故障机器人：75 点生命、3 点能量，初始遗物「破裂核心」。
/// 立绘 / 能量表盘 / 篝火 / 商店 / 音效全部复用二代 Defect 的原版资源（PlaceholderID）。
/// </summary>
public class DefectClassic : PlaceholderCharacterModel
{
    public const string CharacterId = "DefectClassic";

    public static readonly Color Color = new("3EB3ED"); // 二代 Defect 的主题蓝

    public override Color NameColor => Color;

    public override CharacterGender Gender => CharacterGender.Neutral;

    /// <summary>一代故障机器人初始生命：75。</summary>
    public override int StartingHp => 75;

    /// <summary>一代故障机器人初始充能球槽位：3 个。</summary>
    public override int BaseOrbSlotCount => 3;

    /// <summary>复用二代 Defect 的全套视觉与音效资源，省掉自制美术。</summary>
    public override string PlaceholderID => "defect";

    /// <summary>一代初始牌组：4×打击、4×防御、电球、双重施法。</summary>
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Zap>(),
        ModelDb.Card<Dualcast>()
    ];

    /// <summary>一代初始遗物：破裂核心（每场战斗开始时生成 1 个闪电球）。</summary>
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<CrackedCore>()];

    public override CardPoolModel CardPool => ModelDb.CardPool<DefectClassicCardPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<DefectClassicRelicPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<DefectClassicPotionPool>();

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CharacterLoc(
            Title: "经典故障机器人",
            TitleObject: "经典故障机器人",
            Description: "一代的故障机器人，原样归来。\n一个依靠充能球生存的自律机械。",
            PronounObject: "它",
            PronounSubject: "它",
            PronounPossessive: "它的",
            PossessiveAdjective: "它的",
            AromaPrinciple: "[sine][blue]……检测到……熟悉的气息……[/blue][/sine]",
            EndTurnPingAlive: "……",
            EndTurnPingDead: "……系统……离线……",
            EventDeathPrevention: "我……还能被修复。",
            GoldMonologue: "金币……不是必要组件。",
            CardsModifierTitle: "经典故障机器人卡牌",
            CardsModifierDescription: "经典故障机器人的卡牌现在会出现在奖励与商店中。"),

        _ => new CharacterLoc(
            Title: "Defect Classic",
            TitleObject: "Defect Classic",
            Description: "The original Defect, restored.\nAn automaton that channels Orbs to survive.",
            PronounObject: "it",
            PronounSubject: "it",
            PronounPossessive: "its",
            PossessiveAdjective: "its",
            AromaPrinciple: "[sine][blue]...familiar signature detected...[/blue][/sine]",
            EndTurnPingAlive: "...",
            EndTurnPingDead: "...systems... offline...",
            EventDeathPrevention: "I can still be repaired.",
            GoldMonologue: "Gold... not a required component.",
            CardsModifierTitle: "Defect Classic Cards",
            CardsModifierDescription: "Defect Classic cards can now appear in rewards and shops.")
    };
}
