using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using DefectClassic.DefectClassicCode.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace DefectClassic.DefectClassicCode.Powers;

/// <summary>
/// 一代「跟踪锁定」：该敌人从充能球受到的伤害提高 50%，持续 N 回合（每回合递减 1）。
/// 倍率本身由 <see cref="OrbLockOnPatch"/> 施加 —— 对应一代 AbstractOrb.applyLockOn 的位置。
/// </summary>
public sealed class LockOnPower : DefectClassicPower
{
    /// <summary>一代 LockOnPower.MULTIPLIER = 1.5f。</summary>
    public const decimal Multiplier = 1.5m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromOrb<LightningOrb>()];

    /// <summary>回合结束时递减，归零即移除（一代的 atEndOfRound）。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }

    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    {
        "zhs" => new PowerLoc("跟踪锁定", "从充能球受到的伤害增加 50%。", "从充能球受到的伤害增加 50%。"),
        _ => new PowerLoc("Lock-On", "Takes 50% more damage from Orbs.", "Takes 50% more damage from Orbs.")
    };
}
