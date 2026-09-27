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

/// <summary>一代故障机器人：基因算法。</summary>
public sealed class GeneticAlgorithm : DefectClassicCard
{
    private decimal _extra;
    private decimal Extra { get => _extra; set { AssertMutable(); _extra = value; } }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(1m, ValueProp.Move), new DynamicVar("MagicNumber", 2m)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public GeneticAlgorithm() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        decimal inc = DynamicVars["MagicNumber"].BaseValue;
        foreach (GeneticAlgorithm c in Owner.PlayerCombatState!.AllCards.OfType<GeneticAlgorithm>().ToList())
            c.Grow(inc);
    }
    protected override void OnUpgrade() => DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Block.BaseValue += Extra; }
    private void Grow(decimal inc) { DynamicVars.Block.BaseValue += inc; Extra += inc; }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("遗传算法", "获得 {Block:diff()} 点 格挡 。\n每打出一次，这张牌在本局游戏中的格挡值永久性增加 {MagicNumber:diff()} 。"),
        _ => new CardLoc("Genetic Algorithm", "Gain {Block:diff()} Block.\nPermanently increase this card's Block by {MagicNumber:diff()}.")
    };
}
