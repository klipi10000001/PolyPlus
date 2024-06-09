﻿using HarmonyLib;
using Polytopia.Data;
namespace PolyPlus {
    public class PolyPlusPatcher
    {
        public static void Load()
        {
            Console.WriteLine("Loading PolyPlus...");
            CreateEnumCaches();
            Harmony.CreateAndPatchAll(typeof(PolyPlusPatcher));
            Console.WriteLine("PolyPlus Loaded!");
        }

        private static void CreateEnumCaches()
        {
			EnumCache<PlayerAbility.Type>.AddMapping("waterembarking", (PlayerAbility.Type)750);
			EnumCache<PlayerAbility.Type>.AddMapping("waterembarking", (PlayerAbility.Type)750);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.IsTileAccessible))]
        private static void PathFinder_IsTileAccessible(ref bool __result, TileData tile, TileData origin, PathFinderSettings settings)
	    {
            if(PlayerExtensions.HasAbility(settings.playerState, (PlayerAbility.Type)750, settings.gameState) && tile.IsWater && (!origin.IsWater || settings.unit != null)){
                if(settings.allowedTerrain.Contains(tile.terrain) && tile.GetExplored(settings.playerState.Id)){
                    __result = true;
                }
                else{
                    __result = false;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MoveAction), nameof(MoveAction.ExecuteDefault))]
        private static bool MoveAction_ExecuteDefault(MoveAction __instance, GameState gameState)
	    {
            UnitState unitState;
            PlayerState playerState;
            UnitData unitData;
            if (gameState.TryGetUnit(__instance.UnitId, out unitState) && gameState.TryGetPlayer(__instance.PlayerId, out playerState) && gameState.GameLogicData.TryGetData(unitState.type, out unitData))
            {
                WorldCoordinates worldCoordinates = __instance.Path[0];
                WorldCoordinates worldCoordinates2 = __instance.Path[__instance.Path.Count - 1];
                TileData tile = gameState.Map.GetTile(worldCoordinates2);
                TileData tile2 = gameState.Map.GetTile(worldCoordinates);
                unitState.moved = unitState.moved || ((__instance.Reason != MoveAction.MoveReason.Attack || !unitState.HasAbility(UnitAbility.Type.Escape, null)) && __instance.Reason != MoveAction.MoveReason.Push);
                if (unitState.HasAbility(UnitAbility.Type.Skate, null) && gameState.Map.GetTile(worldCoordinates).terrain != Polytopia.Data.TerrainData.Type.Ice && __instance.Reason != MoveAction.MoveReason.Push)
                {
                    unitState.moved = true;
                    unitState.attacked = true;
                }
                tile.SetUnit(null);
                tile2.SetUnit(unitState);
                unitState.coordinates = worldCoordinates;
                if (__instance.Path.Count > 1)
                {
                    unitState.SetUnitDirection(__instance.Path[1], worldCoordinates);
                }
                ActionUtils.CheckStepOnPoison(tile2, unitState, gameState);
                if (__instance.Reason != MoveAction.MoveReason.Push)
                {
                    if (unitState.HasAbility(UnitAbility.Type.AutoFreeze, gameState))
                    {
                        gameState.ActionStack.Add(new FreezeAreaAction(__instance.PlayerId, tile2.coordinates, 1, true, false));
                    }
                    if (unitState.HasAbility(UnitAbility.Type.Stomp, gameState))
                    {
                        ActionUtils.StompAttack(gameState, unitState, tile2.coordinates);
                    }
                    if (unitState.HasAbility(UnitAbility.Type.AutoHeal, gameState))
                    {
                        gameState.ActionStack.Add(new HealOthersAction(__instance.PlayerId, tile2.coordinates));
                    }
                }
                int sightRange = unitData.GetSightRange();
                foreach (WorldCoordinates worldCoordinates3 in __instance.Path)
                {
                    TileData tile3 = gameState.Map.GetTile(worldCoordinates3);
                    ActionUtils.ExploreFromTile(gameState, playerState, tile3, sightRange, true);
                }
                if (unitState.type == UnitData.Type.Bunny && tile2.CanDestroy(gameState, playerState.Id))
                {
                    gameState.ActionStack.Add(new DestroyImprovementAction(tile2.owner, tile2.coordinates));
                }
                if (!unitData.IsAquatic() && !unitState.HasAbility(UnitAbility.Type.Fly, gameState) && tile2.IsWater && tile2.HasEmbarkImprovement(gameState))
                {
                    gameState.ActionStack.Add(new EmbarkAction(__instance.PlayerId, worldCoordinates));
                }
                if (!unitData.IsAquatic() && !unitState.HasAbility(UnitAbility.Type.Fly, gameState) && tile2.IsWater && PlayerExtensions.HasAbility(playerState, (PlayerAbility.Type)750, gameState))
                {
                    gameState.ActionStack.Add(new EmbarkAction(__instance.PlayerId, worldCoordinates));
                }
                else if (!tile2.IsWater && unitData.IsVehicle())
                {
                    gameState.ActionStack.Add(new DisembarkAction(__instance.PlayerId, worldCoordinates));
                }
                UnitState unitState2;
                if (unitState.HasFollower() && gameState.TryGetUnit(unitState.follower, out unitState2))
                {
                    Il2CppSystem.Collections.Generic.List<WorldCoordinates> coordList = new Il2CppSystem.Collections.Generic.List<WorldCoordinates>();
                    coordList.Add(worldCoordinates2);
                    coordList.Add(unitState2.coordinates);
                    if (MapDataExtensions.ChebyshevDistance(unitState2.coordinates, worldCoordinates) > 1)
                    {
                        gameState.ActionStack.Add(new MoveAction(__instance.PlayerId, unitState.follower, coordList, MoveAction.MoveReason.Command));
                    }
                    else{
                        gameState.ActionStack.Add(new MoveAction(__instance.PlayerId, unitState.follower, coordList, MoveAction.MoveReason.Command));
                    }
                }
                if (tile2.HasImprovement(ImprovementData.Type.City) && tile2.owner != 0 && tile2.owner != playerState.Id)
                {
                    playerState.SetLastAttack(tile2.owner, (int)gameState.CurrentTurn, gameState);
                }
            }
            return false;
        }
	}
}