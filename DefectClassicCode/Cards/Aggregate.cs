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

/// <summary>一代故障机器人：汇集。</summary>
public sealed class Aggregate : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MagicNumber", 4m)];
    public Aggregate() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        int n = PileType.Draw.GetPile(Owner).Cards.Count / DynamicVars["MagicNumber"].IntValue;
        if (n > 0) await PlayerCmd.GainEnergy(n, Owner);
    }
    protected override void OnUpgrade() => DynamicVars["MagicNumber"].UpgradeValueBy(-1m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("汇集", "你的抽牌堆中每有 {MagicNumber:diff()} 张牌，获得一点 {energyPrefix:energyIcons(1)} 。"),
        _ => new CardLoc("Aggregate", "Gain {energyPrefix:energyIcons(1)} for every {MagicNumber:diff()} cards in your draw pile.")
    };
}
