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

/// <summary>一代故障机器人：风暴：打出能力牌生成闪电球。</summary>
public sealed class Storm : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StormPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StormPower>(), HoverTipFactory.FromOrb<LightningOrb>()];
    public Storm() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<StormPower>(ctx, Owner.Creature, DynamicVars["StormPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("雷暴", "你每打出一张能力牌， 生成 1 个 闪电 充能球。"),
        _ => new CardLoc("Storm", "Whenever you play a Power card, Channel 1 Lightning.")
    };
}
