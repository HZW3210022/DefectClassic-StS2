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

/// <summary>一代故障机器人：刮擦。</summary>
public sealed class Scrape : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new CardsVar(4)];
    public Scrape() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
        // 二代原生写法：抽到的牌里，本回合实际费用不为 0（且不是 X 费）的才丢弃
        var drawn = (await CardPileCmd.Draw(ctx, DynamicVars.Cards.IntValue, Owner))
            .Where(c => c.EnergyCost.GetWithModifiers(CostModifiers.All) != 0 || c.EnergyCost.CostsX);
        await CardCmd.Discard(ctx, drawn);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); DynamicVars.Cards.UpgradeValueBy(1m); }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("刮削", "造成 {Damage:diff()} 伤害。\n抽 {Cards:diff()} 张牌。\n丢弃抽到的牌中耗能不为0的牌。"),
        _ => new CardLoc("Scrape", "Deal {Damage:diff()} damage.\nDraw {Cards:diff()} cards.\nDiscard all cards drawn this way that do not cost 0.")
    };
}
