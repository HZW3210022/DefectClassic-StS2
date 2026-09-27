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

/// <summary>一代故障机器人：力场。</summary>
public sealed class ForceField : DefectClassicCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12m, ValueProp.Move)];

    public ForceField() : base(4, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    private int PowersPlayedThisCombat =>
        CombatManager.Instance.History.CardPlaysFinished
            .Count(e => e.CardPlay.Player == Owner && e.CardPlay.Card.Type == CardType.Power);

    /// <summary>本场战斗中每打出一张能力牌，费用降低 1。</summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card is not ForceField) return false;
        decimal reduced = Math.Max(0m, originalCost - PowersPlayedThisCombat);
        if (reduced == originalCost) return false;
        modifiedCost = reduced;
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("力场", "你在本场战斗中每打出一张能力牌，耗能就减少 1 {energyPrefix:energyIcons(1)} 。\n获得 {Block:diff()} 点 格挡 。"),
        _ => new CardLoc("Force Field", "Costs 1 {energyPrefix:energyIcons(1)} less for each Power card played this combat.\nGain {Block:diff()} Block.")
    };
}
