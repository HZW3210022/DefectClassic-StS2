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

/// <summary>一代故障机器人：重启：洗回全部手牌并重抽。</summary>
public sealed class Reboot : DefectClassicCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public Reboot() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        // 一代 ShuffleAllAction：手牌与弃牌堆的所有牌都洗回抽牌堆
        foreach (CardModel c in PileType.Hand.GetPile(Owner).Cards.ToList())
            await CardPileCmd.Add(c, PileType.Draw);
        foreach (CardModel c in PileType.Discard.GetPile(Owner).Cards.ToList())
            await CardPileCmd.Add(c, PileType.Draw);
        await CardPileCmd.Shuffle(ctx, Owner);
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.IntValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new CardLoc("重启", "将你所有未消耗的牌重新洗牌放入抽牌堆。\n抽 {Cards:diff()} 张牌。"),
        _ => new CardLoc("Reboot", "Shuffle ALL your cards into your draw pile.\nDraw {Cards:diff()} cards.")
    };
}
