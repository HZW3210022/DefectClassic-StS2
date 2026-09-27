using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace DefectClassic.DefectClassicCode.Powers;

/// <summary>
/// 一代「增幅」：本回合打出的下一张能力牌会打出两次，回合结束即失效。
///
/// 二代自带的 SignalBoostPower 逻辑一模一样，但没有回合结束清除，
/// 于是复制一份并补上回合结束移除，才能与一代对齐。
/// </summary>
public sealed class AmplifyPower : DefectClassicPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != Owner) return playCount;
        if (card.Type != CardType.Power) return playCount;
        return playCount + 1;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        await PowerCmd.Decrement(this);
    }

    /// <summary>一代 AmplifyPower.atEndOfTurn：玩家回合结束时移除。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
        {
            await PowerCmd.Remove(this);
        }
    }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("增幅", "本回合打出的下一张能力牌会打出两次。", "本回合打出的下一张能力牌会打出两次。"),
        _ => new PowerLoc("Amplify", "Your next Power card this turn is played twice.", "Your next Power card this turn is played twice.")
    };
}
