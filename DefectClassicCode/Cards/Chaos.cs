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

/// <summary>一代故障机器人：混沌。</summary>
public sealed class Chaos : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Channeling)];
    public Chaos() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        for (int i = 0; i < DynamicVars["MagicNumber"].IntValue; i++)
        {
            switch (Owner.RunState!.Rng.CombatTargets.NextInt(4))
            {
                case 0: await OrbCmd.Channel<LightningOrb>(ctx, Owner); break;
                case 1: await OrbCmd.Channel<FrostOrb>(ctx, Owner); break;
                case 2: await OrbCmd.Channel<DarkOrb>(ctx, Owner); break;
                default: await OrbCmd.Channel<PlasmaOrb>(ctx, Owner); break;
            }
        }
    }
    protected override void OnUpgrade() => DynamicVars["MagicNumber"].UpgradeValueBy(1m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("混沌", "生成 {MagicNumber:diff()} 个随机充能球。"),
        _ => new CardLoc("Chaos", "Channel {MagicNumber:diff()} random Orb{MagicNumber:plural:|s}.")
    };
}
