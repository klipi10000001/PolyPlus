using HarmonyLib;
using Polytopia.Data;

namespace PolyPlus;
public static class Main
{

    public static void Load()
    {
        PolyMod.Loader.AddPatchDataType("tileEffect", typeof(TileData.EffectType));
        EnumCache<CommandType>.AddMapping("embarkcommand", (CommandType)1000);
        Harmony.CreateAndPatchAll(typeof(Main));
        Harmony.CreateAndPatchAll(typeof(UnitOverrides));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameState), nameof(GameState.GetCommand))]
    public static bool BaseCommand(ref CommandBase __result, CommandType type) 
    {
        if (type == EnumCache<CommandType>.GetType("embarkcommand"))
        {
	        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<EmbarkCommand>()) 
		        ClassInjector.RegisterTypeInIl2Cpp<EmbarkCommand>();            			
            __result = new EmbarkCommand();
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.CanPlayerEmbark))]
    public static bool CanEmbark(ref bool __result,GameState gamestate, PlayerState player) {
        __result = player.HasAbility("waterembark", gamestate);
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.WillUnitEmbark))]
    public static bool WillUnitEmbark(ref ActionUtils.EmbarkStatus __result, UnitState unit, TileData targetTile, GameState gameState)
    {
        __result = MovementHelper.WillUnitEmbark(unit, targetTile, gameState);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.IsTileAccessible))]
    public static bool IsTileAccessible(ref bool __result, TileData tile, TileData origin, PathFinderSettings settings)
    {
        __result = MovementHelper.IsTileAccessible(origin, tile, settings);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TileData), nameof(TileData.GetMovementCost))]
    public static bool TileData_GetMovementCost(
        ref int __result,
        TileData __instance,
        MapData map,
        TileData fromTile,
        PathFinderSettings settings)
    {
        __result = MovementHelper.ComputeMovementCost(map, fromTile, __instance, settings);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateInternal))]
    private static void MapGenerator_GenerateInternal(int seed, GameState gameState, MapGeneratorSettings settings)
    {
        WorldCoordinates[] corners = ExploreLightHouseTask.GetCorners(gameState);
        for (int i = 0; i < corners.Length; i++)
        {
            TileData tile = gameState.Map.GetTile(corners[i]);
            if(tile.HasImprovement(ImprovementData.Type.LightHouse))
            {
                tile.improvement = null; // Yes, I know about AddLightHouseImprovements. It is simply not longer called. idk why
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.OnRevealLighthouseTile))]
    public static bool OnRevealLighthouseTile(GameState gameState, PlayerState playerState, WorldCoordinates tile)
    {
        gameState.CheckTask(playerState, TaskData.Type.ExploreLighthouses);
        ActionUtils.EnableTask(gameState, playerState, TaskData.Type.ExploreLighthouses);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.AddPostTerrainCities))]
    public static bool AddPostTerrainCities(MapGenerator __instance, MapData map, int maxCityCount)
    {
        MapHelper.PostTerrainVillages(__instance, map, maxCityCount);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.SetTileAsCapital))]
    private static void MapGenerator_SetTileAsCapital(GameState gameState, PlayerState playerState, TileData tile)
    {
        if (tile == null || !gameState.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData)
            || !tribeData.HasAbility(EnumCache<TribeAbility.Type>.GetType("citypark")))
        {
            return;
        }

        tile.improvement.production = 2;
        tile.improvement.baseScore += 50;
        tile.improvement.AddReward(CityReward.Park);
    }
}
