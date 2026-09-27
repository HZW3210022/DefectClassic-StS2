using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using DefectClassic.DefectClassicCode.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DefectClassic.DefectClassicCode.Cards;

/// <summary>一代故障机器人：流线。</summary>
public sealed class Streamline : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15m, ValueProp.Move)];

    public Streamline() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    /// <summary>该张牌在本场战斗中被打出的次数（按实例统计，不波及同类牌）。</summary>
    private static int TimesPlayedFor(CardModel card) =>
        CombatManager.Instance.History.CardPlaysFinished
            .Count(e => ReferenceEquals(e.CardPlay.Card, card));

    /// <summary>本场战斗中每打出一次，费用降低 1。</summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not Streamline) return false;
        decimal reduced = Math.Max(0m, originalCost - TimesPlayedFor(card));
        if (reduced == originalCost) return false;
        modifiedCost = reduced;
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("精简改良", "造成 {Damage:diff()} 点伤害。\n这张牌每被打出一次，它在本场战斗中的耗能减少 1 。"),
        _ => new CardLoc("Streamline", "Deal {Damage:diff()} damage.\nReduce this card's cost by 1 this combat.")
    };
}
