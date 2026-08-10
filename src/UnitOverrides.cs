using HarmonyLib;
using Polytopia.Data;

namespace PolyPlus;

public class UnitOverrides
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetDefenceBonus))]
    public static bool UnitDataExtensions_GetDefenceBonus(ref int __result, UnitState unit, GameState gameState)
    {
        __result = DefenceHelper.ComputeDefenceBonus(unit, gameState);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HealAction), nameof(HealAction.ExecuteDefault))]
    public static bool HealActionEx(HealAction __instance, GameState gameState)
    {
        var tile = gameState.Map.GetTile(__instance.Coordinates);
        var unit = tile?.unit;
        if (unit == null) return false;
        unit.RemoveEffect(UnitEffect.Poisoned);
        unit.RemoveEffect(UnitEffect.Charmed);
        unit.passengerUnit?.RemoveEffect(UnitEffect.Poisoned);
        unit.passengerUnit?.RemoveEffect(UnitEffect.Charmed);
        var val = (ushort)unit.GetMaxHealth(gameState);
        if (__instance.FixedHealAmount == ushort.MaxValue)
            __instance.FixedHealAmount = 0;
        else if (__instance.FixedHealAmount > 0)
            __instance.HealAmount = __instance.FixedHealAmount;
        else
            __instance.HealAmount = ActionUtils.GetHealAmount(gameState, tile);
        var val2 = (ushort)(unit.health + __instance.HealAmount);
        unit.health = Math.Min(val2, val);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HealOthersAction), nameof(HealOthersAction.Execute))]
    public static bool HealAreaEx(HealOthersAction __instance, GameState gameState)
    {
        foreach (var healOption in gameState.Map.GetTile(__instance.Coordinates).GetHealOptions(__instance.PlayerId, gameState, true))
        {
            gameState.ActionStack.Add(new HealAction(__instance.PlayerId, healOption.coordinates, 40));
        }
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.CanExplode))]
    public static bool CanExplode(ref bool __result, UnitState unit, GameState gameState)
    {
        __result = CanUnitExplode(unit, gameState);
        return false;
    }
    
    public static bool CanUnitExplode(UnitState unit, GameState gameState)
    {
        if (!unit.HasAbility(UnitAbility.Type.Explode))
        {
            return false;
        }

        if (unit.CanAct())
            return false;
        if (unit.HasEffect(UnitEffect.Frozen))
        {
            return false;
        }

        var result = unit.GetAge(gameState) >= 1 || unit.leader != 0;
        return result;
    }

    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.PerformAttack))]
    public static bool PerformAttack(GameState gameState, byte playerId, WorldCoordinates origin,
        WorldCoordinates target, int damage)
    {
        DamagePatches.ExecuteAttack(gameState, playerId, origin, target, damage);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CommandUtils), nameof(CommandUtils.GetUnitActions))]
    public static bool GetUnitActions(
        ref Il2CppSystem.Collections.Generic.List<CommandBase> __result,
        GameState gameState,
        PlayerState player,
        TileData tile,
        bool includeUnavailable)
    {
        __result = UnitHelper.AddUnitActions(gameState, player, tile, includeUnavailable);
        return false;
    }
}