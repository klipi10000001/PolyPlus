using HarmonyLib;
using Polytopia.Data;

namespace PolyPlus;

public static class ImprovementHelperExtensions
{
    public static bool HasAbility(this ImprovementData data, string ability)
    {
        return data.improvementAbilities.Contains(EnumCache<ImprovementAbility.Type>.GetType(ability));
    }
}

public static class ImprovementHelper
{
    public static bool OverridableImprovement(TileData tile, ImprovementData improvement, GameLogicData gameLogic)
    {
	    if (tile.improvement == null) return false;
	    var improvementData = gameLogic.GetImprovementData(tile.improvement.type);
	    if (improvementData.type == ImprovementData.Type.Fungi && improvement.HasAbility("fungiup"))
	    {
		    return true;
	    }
		return false;
	}
    
    public static bool MeetsRequirement(GameLogicData logicData, TileData tile, ImprovementData improvement, PlayerState playerState, GameState gameState)
	{
		if (improvement.HasAbility("mycrogrove"))
		{
			return false;
		}
		if (improvement.terrainRequirements == null || improvement.terrainRequirements.Count <= 0)
		{
			return true;
		}

		var requirements = improvement.GetTerrainRequirementData();
		var terrainRequirements = requirements[TileType.Terrain];
		var meetsTerrainRequirement = terrainRequirements.Count == 0;
		meetsTerrainRequirement = meetsTerrainRequirement || terrainRequirements.Any(it => 
			it.terrain.type == tile.terrain ||
			(it.terrain.type == TerrainData.Type.Wetland &&
				tile.terrain != TerrainData.Type.Forest &&
				tile.IsWetland()) ||
			(it.terrain.type == TerrainData.Type.Mangrove &&
				tile.terrain == TerrainData.Type.Forest &&
				tile.HasEffect(TileData.EffectType.Flooded)) ||
			(it.terrain.type == TerrainData.Type.Field &&
				!tile.IsLand &&
				!logicData.nonPontoonImprovements.Contains(improvement.type) &&
				!improvement.HasAbility("strictreqs") &&
				playerState.HasAbility(PlayerAbility.Type.Pontoon, gameState)) ||
			(it.terrain.type == TerrainData.Type.Field &&
				tile.terrain == TerrainData.Type.Forest &&
				!logicData.nonPontoonImprovements.Contains(improvement.type) &&
				!improvement.HasAbility("strictreqs") &&
				playerState.HasAbility(PlayerAbility.Type.Treehouse, gameState)));

		var resourceRequirements = requirements[TileType.Resource];
		var meetsResourceRequirement = resourceRequirements.Count <= 0 ||
			resourceRequirements.Any(it => it.resource.type == tile.resource?.type);

		return meetsTerrainRequirement && meetsResourceRequirement;
	}
}