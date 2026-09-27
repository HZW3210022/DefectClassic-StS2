using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DefectClassic.DefectClassicCode.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;

namespace DefectClassic.DefectClassicCode.Patches;

internal static class OrbPatchUtil
{
    private static readonly MethodInfo? _evokeSfx = AccessTools.Method(typeof(OrbModel), "PlayEvokeSfx");

    public static void PlayEvokeSfx(OrbModel orb) => _evokeSfx?.Invoke(orb, null);

    /// <summary>一代 AbstractOrb.applyLockOn：目标有跟踪锁定时，充能球伤害 ×1.5。</summary>
    public static decimal ApplyLockOn(Creature target, decimal damage) =>
        target.HasPower<LockOnPower>() ? damage * LockOnPower.Multiplier : damage;
}

/// <summary>
/// 闪电球伤害补丁，对应一代的两处行为：
///   1. 电动力学 —— 目标由「单个随机敌人」改为「全体敌人」
///   2. 跟踪锁定 —— 目标受到的伤害 ×1.5
/// 一代里这两件事分别写在 Lightning orb 与 AbstractOrb.applyLockOn 中，二代没有对应 API，
/// 因此在这里接管 LightningOrb.ApplyLightningDamage，其余行为（特效、音效、激发事件）保持原样。
/// </summary>
[HarmonyPatch]
public static class LightningOrbPatch
{
    [HarmonyPrepare]
    public static bool Prepare()
    {
        bool ok = AccessTools.Method(typeof(LightningOrb), "ApplyLightningDamage") is not null;
        if (!ok) MainFile.Logger.Warn("[DefectClassic] 找不到 LightningOrb.ApplyLightningDamage，电动力学/跟踪锁定将退化为原版");
        return ok;
    }

    [HarmonyTargetMethod]
    public static MethodBase? TargetMethod() => AccessTools.Method(typeof(LightningOrb), "ApplyLightningDamage");

    [HarmonyPrefix]
    public static bool Prefix(LightningOrb __instance, decimal value, Creature? target,
        PlayerChoiceContext choiceContext, bool isEvoke, ref Task<IEnumerable<Creature>> __result)
    {
        Player? owner = __instance.Owner;
        if (owner is null) return true;

        List<Creature> enemies = __instance.CombatState.GetOpponentsOf(owner.Creature)
            .Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0)
        {
            __result = Task.FromResult<IEnumerable<Creature>>(Array.Empty<Creature>());
            return false;
        }

        // 一代：电动力学让闪电球打全体
        bool allEnemies = owner.Creature.HasPower<ElectrodynamicsPower>();
        List<Creature> targets = allEnemies
            ? enemies
            : [target ?? owner.RunState!.Rng.CombatTargets.NextItem(enemies)];

        __result = Apply(__instance, targets, value, choiceContext, isEvoke);
        return false;
    }

    private static async Task<IEnumerable<Creature>> Apply(LightningOrb orb, List<Creature> targets,
        decimal value, PlayerChoiceContext ctx, bool isEvoke)
    {
        if (isEvoke) orb.ActivateEvoke(targets.ToArray());
        foreach (Creature t in targets)
        {
            VfxCmd.PlayOnCreature(t, "vfx/vfx_attack_lightning");
        }
        OrbPatchUtil.PlayEvokeSfx(orb);
        foreach (Creature t in targets)
        {
            // 一代 applyLockOn 的位置
            await CreatureCmd.Damage(ctx, t, OrbPatchUtil.ApplyLockOn(t, value), ValueProp.Unpowered, orb.Owner.Creature);
        }
        return targets;
    }
}

/// <summary>
/// 暗球补丁：一代里暗球的激发伤害同样经过 AbstractOrb.applyLockOn。
/// 二代的 DarkOrb.Evoke 是公开方法，这里直接接管以施加跟踪锁定倍率。
/// </summary>
[HarmonyPatch(typeof(DarkOrb), nameof(DarkOrb.Evoke))]
public static class DarkOrbLockOnPatch
{
    [HarmonyPrefix]
    public static bool Prefix(DarkOrb __instance, PlayerChoiceContext playerChoiceContext,
        ref Task<IEnumerable<Creature>> __result)
    {
        Player? owner = __instance.Owner;
        if (owner is null) return true;

        List<Creature> enemies = __instance.CombatState.GetOpponentsOf(owner.Creature)
            .Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0)
        {
            __result = Task.FromResult<IEnumerable<Creature>>(Array.Empty<Creature>());
            return false;
        }

        __result = Apply(__instance, enemies, playerChoiceContext);
        return false;
    }

    private static async Task<IEnumerable<Creature>> Apply(DarkOrb orb, List<Creature> enemies,
        PlayerChoiceContext ctx)
    {
        OrbPatchUtil.PlayEvokeSfx(orb);
        Creature weakest = enemies.MinBy(c => c.CurrentHp)!;
        orb.ActivateEvoke([weakest]);
        await CreatureCmd.Damage(ctx, weakest, OrbPatchUtil.ApplyLockOn(weakest, orb.EvokeVal),
            ValueProp.Unpowered, orb.Owner.Creature);
        return [weakest];
    }
}
