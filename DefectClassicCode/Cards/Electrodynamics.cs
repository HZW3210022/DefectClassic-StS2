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

/// <summary>一代故障机器人：电动力学：闪电球命中全体。</summary>
public sealed class Electrodynamics : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ElectrodynamicsPower>(1m), new DynamicVar("MagicNumber", 2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ElectrodynamicsPower>(), HoverTipFactory.FromOrb<LightningOrb>()];
    public Electrodynamics() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ElectrodynamicsPower>(ctx, Owner.Creature, DynamicVars["ElectrodynamicsPower"].BaseValue, Owner.Creature, this);
        for (int i = 0; i < DynamicVars["MagicNumber"].IntValue; i++)
            await OrbCmd.Channel<LightningOrb>(ctx, Owner);
    }
    protected override void OnUpgrade() => DynamicVars["MagicNumber"].UpgradeValueBy(1m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("电动力学", "闪电 充能球现在会击中所有敌人。\n生成 {MagicNumber:diff()} 个 闪电 充能球。"),
        _ => new CardLoc("Electrodynamics", "Lightning now hits ALL enemies.\nChannel {MagicNumber:diff()} Lightning.")
    };
}
