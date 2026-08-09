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
}