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

    private static ushort _founded;
    private static ushort _changedFounded;
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BuildAction), nameof(BuildAction.ExecuteDefault))]
    public static bool BuildAction_ExecuteDefaultPre(BuildAction __instance, GameState gameState)
    {
        if (!gameState.GameLogicData.TryGetData(__instance.Type, out var improvementData))
        {
            return true;
        }
        var tile = gameState.Map.GetTile(__instance.Coordinates);
        if (tile == null) return true;
        if (ImprovementHelper.OverridableImprovement(tile, improvementData, gameState.GameLogicData) && improvementData.HasAbility(ImprovementAbility.Type.Patina))
        {
            var tileImprovement = tile.improvement;
            var tileImprovementData = gameState.GameLogicData.GetImprovementData(tileImprovement.type);
            if (tileImprovement.level >= tileImprovementData.maxLevel)
            {
                _founded = (ushort)(gameState.CurrentTurn - tileImprovementData.maxLevel * tileImprovementData.growthRate);
            }
            else _founded = tileImprovement.founded;
            _changedFounded = 1;
        }
        
        if (improvementData.HasAbility("mycrogrove"))
        {
            __instance.AddSubAction(new DestroyImprovementAction(__instance.PlayerId, __instance.Coordinates));
            if (__instance.DeductCost && gameState.TryGetPlayer(__instance.PlayerId, out var playerState))
            {
                playerState.Currency -= improvementData.GetCurrencyCost();
            }
            __instance.AddSubAction(
                new BuildAction(__instance.PlayerId,
                    EnumCache<ImprovementData.Type>.GetType("mycrogrove"),
                    __instance.Coordinates,
                    false));
            __instance.AddSubAction(UpdateImprovementAction.CreateUpgradeImprovementAction(__instance.PlayerId, tile));
            __instance.CommitSubActionsToStack(gameState.ActionStack);
            return false;
        }
        return true;
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildAction), nameof(BuildAction.ExecuteDefault))]
    public static void BuildAction_ExecuteDefaultPost(BuildAction __instance, GameState gameState)
    {
        var tile = gameState.Map.GetTile(__instance.Coordinates);
        if (_changedFounded == 1) _changedFounded++;
        else if (_changedFounded == 2)
        {
            var improvement = tile.improvement;
            improvement.founded = _founded;
            _changedFounded = 0;
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.MeetsRequirement))]
    public static bool MeetsRequirementsPatch(ref bool __result, GameLogicData __instance, TileData tile, ImprovementData improvement, PlayerState playerState, GameState gameState)
    {
        __result = ImprovementHelper.MeetsRequirement(__instance, tile, improvement, playerState, gameState);
        return false;
    }
}
