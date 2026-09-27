using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace DefectClassic.DefectClassicCode.Powers;

/// <summary>一代「散热片」：每打出一张能力牌，抽 N 张牌。</summary>
public sealed class HeatsinksPower : DefectClassicPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card.Type != CardType.Power) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player!);
    }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("散热片", "每打出一张能力牌，抽 {Amount} 张牌。", "每打出一张能力牌，抽 {Amount} 张牌。"),
        _ => new PowerLoc("Heatsinks", "Whenever you play a Power card, draw {Amount} card(s).", "Whenever you play a Power card, draw {Amount} card(s).")
    };
}
