using Polytopia.Data;

namespace PolyPlus;
public static class MovementHelper
{
    internal static int ComputeMovementCost(MapData map,
        TileData fromTile,
        TileData toTile,
        PathFinderSettings settings)
    {
        if (settings.unit == null)
            return NoUnitCost(toTile, settings);

        if (IsBlockedByZoneOfControl(toTile, map, settings))
            return MovementCost.Max;

        if (ActionUtils.WillUnitEmbark(settings.unit, toTile, settings.gameState) != ActionUtils.EmbarkStatus.None)
            return MovementCost.Max;
        
        if (settings.unitData.HasAbility(UnitAbility.Type.Fly))
        {
            return MovementCost.Normal;
        }

        if (settings.unit.HasAbility(UnitAbility.Type.Creep) && toTile.terrain != TerrainData.Type.Mountain)
            return MovementCost.Normal;
        
        if (IsSlowed(toTile, settings))
        {
            return MovementCost.Max;
        }
        if (toTile.terrain != TerrainData.Type.Ice &&
            toTile.HasRoadTo(fromTile, settings.gameState,settings.unit.owner) &&
            !settings.unit.IgnoresRoads())
            return MovementCost.Road;
        
        if (toTile.terrain == TerrainData.Type.Ice &&
            (settings.playerState.HasAbility(PlayerAbility.Type.Glide, settings.gameState) ||
            settings.unit.HasAbility("slide")))
            return MovementCost.GlideIce;

        return GetLandCost(toTile);
    }

    private static int NoUnitCost(TileData tile, PathFinderSettings settings)
    {
        if (settings.shouldFollowTransportPaths && (tile.HasRoad || tile.hasRoute))
            return MovementCost.Road;

        return MovementCost.Normal;
    }

    private static bool IsBlockedByZoneOfControl(TileData tile, MapData map, PathFinderSettings settings)
    {
        if (settings.shouldAllowOccupiedTiles) return false;
        if (settings.unitData.HasAbility(UnitAbility.Type.Sneak)) return false;
        if (settings.unitData.HasAbility(UnitAbility.Type.Hide)) return false;
        if (settings.unit.HasEffect(UnitEffect.Invisible)) return false;

        foreach (var neighbor in map.GetTileNeighbors(tile.coordinates))
        {
            if(neighbor.unit == null) continue;
            if (neighbor.unit.owner == settings.playerState.Id) continue;
            if (neighbor.unit.leader != 0) continue;
            if (neighbor.unit.HasEffect(UnitEffect.Invisible)) continue;
            if (settings.playerState.HasPeaceWith(neighbor.unit.owner)) continue;

            return true;
        }

        return false;
    }

    private static bool IsSlowed(TileData tile, PathFinderSettings settings)
    {
        var unit = settings.unit;
        if (unit.HasAbility(UnitAbility.Type.Creep))
            return false;

        if (unit.HasAbility(UnitAbility.Type.Skate) && tile.terrain != TerrainData.Type.Ice)
            return true;
        if (tile.HasEffect(TileData.EffectType.Flooded) && settings.unit.UnitData.IsWaterBound())
            return true;
        
        var isAmphibious = settings.unitData.HasAbility(UnitAbility.Type.Amphibious) // All my homies hate UnitAbility.Type.Swim
            || settings.unitData.HasAbility(UnitAbility.Type.Water);
        if (isAmphibious && !tile.IsWater && !tile.HasEffect(TileData.EffectType.Flooded))
            return true;

        ImprovementData? data;
        if (tile.improvement != null)
        {
            settings.gameState.GameLogicData.TryGetData(tile.improvement.type, out data);
        }
        else data = null;
        if (tile.improvement != null && data != null && data.HasAbility(ImprovementAbility.Type.Slow))
        {
            return true;
        }

        return false;
    }
    
    private static bool IgnoresRoads(this UnitState unit, TileData? fromTile = null, TileData? toTile = null) 
    {
        if (unit.HasAbility(UnitAbility.Type.Creep))
        {
            return true;
        }

        if (unit.HasAbility(UnitAbility.Type.Swim))
        {
            return true;
        }

        if (unit.HasAbility(UnitAbility.Type.Amphibious))
        {
            return true;
        }

        return false;
    }

    private static int GetLandCost(TileData tile)
    {
        return tile.terrain switch
        {
            TerrainData.Type.Forest => 20,
            TerrainData.Type.Mountain => MovementCost.Max,
            _ => MovementCost.Normal
        };
    }

    private static class MovementCost
    {
        public const int Road = 5;
        public const int GlideIce = 9;
        public const int Normal = 10;
        public const int Forest = 20;
        public const int Max = 1000;
    }
}