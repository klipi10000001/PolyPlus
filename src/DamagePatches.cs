using Polytopia.Data;

namespace PolyPlus;

public static class DamagePatches
{
    public static void ExecuteAttack(GameState gameState, byte playerId, WorldCoordinates origin, WorldCoordinates target, int damage)
    {
        var originTile = gameState.Map.GetTile(origin);
        var targetTile = gameState.Map.GetTile(target);
        var attackingUnit = originTile?.unit;
        var targetUnit = targetTile.unit;
        attackingUnit?.SetUnitDirection(origin, target);
        attackingUnit?.RemoveEffect(UnitEffect.Boosted);
        byte attacker;
        if (attackingUnit == null || origin == target)
            attacker = playerId;
        else
            attacker = attackingUnit.owner;
        gameState.TryGetPlayer(attacker, out var playerState);
        if (targetUnit != null)
        {
            targetUnit.SetUnitDirection(target, origin);
            targetUnit.RemoveEffect(UnitEffect.Swift);
            ActionUtils.RemoveBubble(targetUnit, gameState);
            targetUnit.health -= (ushort)Math.Min(damage, targetUnit.health);
            if (targetUnit.health == 0)
            {
                if (targetUnit.owner != 255)
                {
                    playerState.kills++;
                    if (origin != WorldCoordinates.NULL_COORDINATES && attackingUnit != null && !attackingUnit.UnitData.IsVehicle() && !attackingUnit.UnitData.hidden && !attackingUnit.HasAbility(UnitAbility.Type.Static))
                    {
                        attackingUnit.xp++;
                    }
                    ActionUtils.EnableTask(gameState, playerState, TaskData.Type.Killer);
                    if (gameState.TryGetTask(playerState, TaskData.Type.Killer, out var task) && task.Bump(gameState))
                    {
                        gameState.CheckTask(playerState, task);
                    }
                }
                gameState.TryGetPlayer(targetUnit.owner, out var playerState2);
                playerState2.casualities++;
                gameState.ActionStack.Add(new KillUnitAction(attacker, target));
            }
            if (attackingUnit != null && attackingUnit.HasAbility(UnitAbility.Type.Poison))
            {
                gameState.ActionStack.Add(new PoisonUnitAction(attacker, origin, target));
            }
        }
        if (attackingUnit != null && attackingUnit.HasAbility(UnitAbility.Type.Drench) && targetTile.isFloodable())
        {
            gameState.ActionStack.Add(new FloodTileAction(attacker, target));
        }
    }
}