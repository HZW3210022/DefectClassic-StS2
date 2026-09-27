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

/// <summary>一代故障机器人：万物一心：收回所有零费牌。</summary>
public sealed class AllForOne : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];
    public AllForOne() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3").Execute(ctx);
        // 二代原生 Filter：本回合实际费用为 0、且不是 X 费的可打出牌
        var zeros = PileType.Discard.GetPile(Owner).Cards
            .Where(c => c.EnergyCost.GetWithModifiers(CostModifiers.All) == 0
                        && !c.EnergyCost.CostsX
                        && c.Type is CardType.Attack or CardType.Skill or CardType.Power)
            .ToList();
        foreach (CardModel c in zeros) await CardPileCmd.Add(c, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("万物一心", "造成 {Damage:diff()} 点伤害。\n将弃牌堆中所有0耗能的牌放入你的手牌。"),
        _ => new CardLoc("All for One", "Deal {Damage:diff()} damage.\nPut all cost 0 cards from your discard pile into your hand.")
    };
}
