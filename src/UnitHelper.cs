using Polytopia.Data;

namespace PolyPlus;

public static class UnitHelper
{
    public static bool HasAbility(this UnitState unit, string ability)
    {
        return unit.UnitData.unitAbilities.Contains(EnumCache<UnitAbility.Type>.GetType(ability));
    }
    public static bool HasAbility(this UnitState unit, string ability, GameLogicData logicData)
    {
        if (!logicData.TryGetData(unit.type, out var data))
            return false;
        return data.unitAbilities.Contains(EnumCache<UnitAbility.Type>.GetType(ability));
    }
    public static bool HasAbility(this UnitData unit, string ability)
    {
        return unit.unitAbilities.Contains(EnumCache<UnitAbility.Type>.GetType(ability));
    }
    
    public static bool CanEmbark (this UnitState unit, GameState state)
    {
        if (unit.HasAbility(UnitAbility.Type.Fly)) return false;
        if (unit.UnitData.IsAquatic()) return false;
        if (unit.HasFollower() || unit.HasLeader()) return false;
        if (!state.TryGetPlayer(unit.owner, out var playerState)) return false;
        if (!ActionUtils.CanPlayerEmbark(state, playerState)) return false;
        var tile = state.Map.GetTile(unit.coordinates);
        if (tile.IsWetland())
        {
            return true;
        }
        if (tile.improvement != null && state.GameLogicData.TryGetData(tile.improvement.type, out var data) && data != null && data.HasAbility(ImprovementAbility.Type.Bridge))
        {
            return true;
        }
        return false;
    }

    public static bool CanAct(this UnitState unit)
    {
        if (unit.attacked)
        {
        	return false;
        }
        if (unit.HasEffect(UnitEffect.Frozen))
        {
        	return false;
        }

        return true;
    }
    
    public static bool CanActAndMove(this UnitState unit)
    {
        if (unit.attacked || unit.moved)
        {
            return false;
        }
        if (unit.HasEffect(UnitEffect.Frozen))
        {
            return false;
        }

        return true;
    }
    
    public static bool CanActOrMove(this UnitState unit)
    {
        if (unit.attacked && unit.moved)
        {
            return false;
        }
        if (unit.HasEffect(UnitEffect.Frozen))
        {
            return false;
        }

        return true;
    }

    public static Il2CppSystem.Collections.Generic.List<CommandBase> AddUnitActions(GameState gameState, PlayerState player, TileData tile, bool includeUnavailable = false)
    {
        var list = new Il2CppSystem.Collections.Generic.List<CommandBase>();
        var unit = tile.unit;
        if (unit == null || unit.owner != player.Id)
        {
            return list;
        }

        gameState.GameLogicData.TryGetData(unit.type, out var data);
        if (unit.CanCapture(gameState, tile))
        {
            CommandUtils.AddCommand(gameState, list, new CaptureCommand(player.Id, unit.id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanExamineRuins(gameState, tile))
        {
            CommandUtils.AddCommand(gameState, list, new ExamineRuinsCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanRecover(gameState))
        {
            CommandUtils.AddCommand(gameState, list, new RecoverCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if ((unit.CanHealOthers(gameState) || unit.CanRecover(gameState)) && tile.GetHealOptions(unit.owner, gameState, includeCenter: true).Count > 0)
        {
            CommandUtils.AddCommand(gameState, list, new HealOthersCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanDisembark(gameState))
        {
            CommandUtils.AddCommand(gameState, list, new DisembarkCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanEmbark(gameState))
        {
            CommandUtils.AddCommand(gameState, list, EmbarkCommand.CreateCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanBePromoted(gameState))
        {
            CommandUtils.AddCommand(gameState, list, new PromoteCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanFreezeArea(gameState))
        {
            CommandUtils.AddCommand(gameState, list, new FreezeAreaCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanBreakIce(gameState))
        {
            var area = gameState.Map.GetArea(unit.coordinates, 1, allowDiagonal: true);
            if (area.Any(tileData => tileData.HasBreakableIce(gameState, player.Id)))
            {
                CommandUtils.AddCommand(gameState, list, new BreakIceCommand(player.Id, unit.coordinates), includeUnavailable);
            }
        }
        if (!unit.HasAbility(UnitAbility.Type.Grow))
        {
            foreach (var item in gameState.GameLogicData.GetUnlockedUpgradesForUnit(player, gameState, data))
            {
                CommandUtils.AddCommand(gameState, list, new UpgradeCommand(player.Id, item.type, unit.coordinates), includeUnavailable);
            }
        }
        if (player.Id == gameState.CurrentPlayer)
        {
            if (unit.CanActAndMove() && unit.CanDisband(gameState, player))
            {
                CommandUtils.AddCommand(gameState, list, new DisbandCommand(player.Id, unit.coordinates),
                    includeUnavailable);
            }

            if (unit.CanActOrMove() && unit.HasAbility(UnitAbility.Type.Boost))
            {
                CommandUtils.AddCommand(gameState, list, new BoostCommand(player.Id, unit.coordinates),
                    includeUnavailable);
            }

            if (unit.CanActOrMove() && unit.HasAbility(UnitAbility.Type.Swarm))
            {
                CommandUtils.AddCommand(gameState, list, new SwarmCommand(player.Id, unit.coordinates),
                    includeUnavailable);
            }
        }
        if (unit.CanExplode(gameState))
        {
            CommandUtils.AddCommand(gameState, list, new ExplodeCommand(player.Id, unit.coordinates), includeUnavailable);
        }
        if (unit.CanBuild())
        {
            foreach (var unlockedImprovement in gameState.GameLogicData.GetUnlockedImprovements(player))
            {
                if (unlockedImprovement.HasAbility(ImprovementAbility.Type.Manual) && gameState.GameLogicData.CanBuild(gameState, tile, player, unlockedImprovement))
                {
                    CommandUtils.AddCommand(gameState, list, new BuildCommand(player.Id, unlockedImprovement.type, tile.coordinates), includeUnavailable);
                }
            }
        }
        if (unit.CanActOrMove() && !tile.HasEffect(TileData.EffectType.Flooded) && player.HasAbility("canal", gameState))
        {
            CommandUtils.AddCommand(gameState, list, new FloodCommand(player.Id, unit.coordinates), includeUnavailable);
        }

        if (unit.CanActOrMove() && tile.HasEffect(TileData.EffectType.Algae) && (unit.HasAbility(UnitAbility.Type.Amphibious) || unit.HasAbility(UnitAbility.Type.Swim) || unit.HasAbility(UnitAbility.Type.Fly)))
        {
            CommandUtils.AddCommand(gameState, list, new ClearTileEffectCommand(player.Id, unit.coordinates, TileData.EffectType.Algae), includeUnavailable);
        }
        if (unit.CanActOrMove() && tile.HasEffect(TileData.EffectType.Flooded) && (unit.HasAbility(UnitAbility.Type.Amphibious) || !unit.HasAbility(UnitAbility.Type.Swim)))
        {
            CommandUtils.AddCommand(gameState, list, new ClearTileEffectCommand(player.Id, unit.coordinates, TileData.EffectType.Flooded), includeUnavailable);
        }
        return list;
    }
}