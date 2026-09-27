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

/// <summary>一代故障机器人：静电释放：受击时生成闪电球。</summary>
public sealed class StaticDischarge : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StaticDischargePower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StaticDischargePower>(), HoverTipFactory.FromOrb<LightningOrb>()];
    public StaticDischarge() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<StaticDischargePower>(ctx, Owner.Creature, DynamicVars["StaticDischargePower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["StaticDischargePower"].UpgradeValueBy(1m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("静电释放", "每当你受到未被格挡的攻击伤害时， 生成 {StaticDischargePower:diff()} 个 闪电 充能球。"),
        _ => new CardLoc("Static Discharge", "Whenever you receive unblocked attack damage, Channel {StaticDischargePower:diff()} Lightning.")
    };
}
