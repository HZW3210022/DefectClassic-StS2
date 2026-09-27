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

/// <summary>一代故障机器人：回收。</summary>
public sealed class Recycle : DefectClassicCard
{
    public Recycle() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        CardModel? picked = (await CardSelectCmd.FromHand(ctx, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), null, this)).FirstOrDefault();
        if (picked is null) return;
        int gain = Math.Max(0, picked.EnergyCost.Canonical);
        await CardCmd.Exhaust(ctx, picked);
        if (gain > 0) await PlayerCmd.GainEnergy(gain, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("回收", "消耗 一张牌。\n获得与其耗能相等的 {energyPrefix:energyIcons(1)} 。"),
        _ => new CardLoc("Recycle", "Exhaust a card.\nGain {energyPrefix:energyIcons(1)} equal to its cost.")
    };
}
