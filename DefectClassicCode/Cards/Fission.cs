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

/// <summary>一代故障机器人：裂变。移除所有充能球，每移除一个回 1 能量并抽牌（升级改为先激发）。</summary>
public sealed class Fission : DefectClassicCard
{

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Fission() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var queue = Owner.PlayerCombatState!.OrbQueue;
        int orbCount = queue.Orbs.Count;
        if (orbCount == 0) return;

        if (IsUpgraded)
        {
            // 一代升级版：激发所有充能球（按快照逐个激发，避免边遍历边改集合）
            for (int i = 0; i < orbCount; i++)
            {
                if (queue.Orbs.Count == 0) break;
                await OrbCmd.EvokeNext(ctx, Owner);
            }
        }
        else
        {
            // 一代基础版：直接移除，不激发
            queue.Clear();
        }

        await PlayerCmd.GainEnergy(orbCount, Owner);
        await CardPileCmd.Draw(ctx, orbCount, Owner);
    }

    /// <summary>一代升级只把「移除」改成「激发」，数值不变。</summary>
    protected override void OnUpgrade() { }
    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("裂变", "{IfUpgraded:show:激发|移除} 所有 充能球 ，每{IfUpgraded:show:激发|移除}一个充能球获得 {energyPrefix:energyIcons(1)} 并抽 1 张牌 。"),
        _ => new CardLoc("Fission", "{IfUpgraded:show:Evoke|Remove} all your Orbs. Gain {energyPrefix:energyIcons(1)} and draw 1 card for each Orb {IfUpgraded:show:Evoked|removed}.")
    };
}
