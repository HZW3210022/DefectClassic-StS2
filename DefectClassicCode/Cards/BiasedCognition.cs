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

/// <summary>一代故障机器人：偏差认知：大量集中，但每回合流失。</summary>
public sealed class BiasedCognition : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FocusPower>(4m), new PowerVar<BiasedCognitionPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FocusPower>(), HoverTipFactory.FromPower<BiasedCognitionPower>()];
    public BiasedCognition() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<FocusPower>(ctx, Owner.Creature, DynamicVars["FocusPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<BiasedCognitionPower>(ctx, Owner.Creature, DynamicVars["BiasedCognitionPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["FocusPower"].UpgradeValueBy(1m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("偏差认知", "获得 {FocusPower:diff()} 点 集中。\n在每回合开始时，失去 1 点 集中 。"),
        _ => new CardLoc("Biased Cognition", "Gain {FocusPower:diff()} Focus.\nAt the start of your turn, lose 1 Focus.")
    };
}
