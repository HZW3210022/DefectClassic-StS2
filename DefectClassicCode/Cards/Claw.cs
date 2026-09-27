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

/// <summary>一代故障机器人：爪击。</summary>
public sealed class Claw : DefectClassicCard
{
    private decimal _extra;
    private decimal Extra { get => _extra; set { AssertMutable(); _extra = value; } }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move), new DynamicVar("MagicNumber", 2m)];
    public Claw() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        ArgumentNullException.ThrowIfNull(play.Target, "play.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
        decimal inc = DynamicVars["MagicNumber"].BaseValue;
        foreach (Claw c in Owner.PlayerCombatState!.AllCards.OfType<Claw>()) c.Buff(inc);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
    protected override void AfterDowngraded() { base.AfterDowngraded(); DynamicVars.Damage.BaseValue += Extra; }
    private void Buff(decimal inc) { DynamicVars.Damage.BaseValue += inc; Extra += inc; }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("爪击", "造成 {Damage:diff()} 点伤害。\n在本场战斗中所有 爪击 牌的伤害增加 {MagicNumber:diff()} 。"),
        _ => new CardLoc("Claw", "Deal {Damage:diff()} damage.\nIncrease the damage of ALL Claw cards by {MagicNumber:diff()} this combat.")
    };
}
