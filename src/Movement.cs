using HarmonyLib;
using Polytopia.Data;

namespace PolyPlus
{
    public class Movement
    {
        //TODO: ADD A METHOD WHICH RETURN ALL IMPROVEMENTS WITH BRIDGE ABILITY
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.IsTileAccessible))] // PROPERLY TEST THE CHANGE AS IT MAY BREAK WHOLE MOVEMENT.
        private static void PathFinder_IsTileAccessible(ref bool __result, TileData tile, TileData origin, PathFinderSettings settings) // if it works correctly its gonna be perfect optimisation!
        {
            if(settings.unit != null)
            {
                if(settings.unit.HasAbility(EnumCache<UnitAbility.Type>.GetType("revolt")))
                {
                    if(tile.HasImprovement(ImprovementData.Type.City))
                    {
                        __result = false;
                        return;
                    }
                }
                // FIXME: Units on algae cannot move on water tiles to embark.
                if (PlayerExtensions.HasAbility(settings.playerState, EnumCache<PlayerAbility.Type>.GetType("waterembark"), settings.gameState)
                    && settings.allowedTerrain.Contains(tile.terrain) && tile.GetExplored(settings.playerState.Id))
                {
                    bool tileHasBridge = tile.HasEffect(TileData.EffectType.Algae)
                        || tile.HasImprovement(ImprovementData.Type.Bridge);

                    bool originHasBridge = origin.HasEffect(TileData.EffectType.Algae)
                        || origin.HasImprovement(ImprovementData.Type.Bridge);
                    if(tile.IsWater && !tileHasBridge &&
                        (!origin.IsWater || originHasBridge)) // I NEED TO CHECK BRIDGE ABIL INSTEAD
                    {
                        __result = true;
                        return;
                    }
                    if(origin.IsWater && !originHasBridge &&
                        !tile.IsWater && settings.unit.HasAbility(UnitAbility.Type.Land)) // I NEED TO CHECK BRIDGE ABIL INSTEAD
                    {
                        // wat?
                        __result = false;
                        return;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MoveAction), nameof(MoveAction.ExecuteDefault))]
        private static void MoveAction_ExecuteDefault(MoveAction __instance, GameState gameState)
        {
            UnitState unitState;
            PlayerState playerState;
            UnitData unitData;
            if (gameState.TryGetUnit(__instance.UnitId, out unitState)
                && gameState.TryGetPlayer(__instance.PlayerId, out playerState)
                && gameState.GameLogicData.TryGetData(unitState.type, out unitData))
            {
                WorldCoordinates worldCoordinates = __instance.Path[0];
                TileData tile2 = gameState.Map.GetTile(worldCoordinates);
                tile2.SetUnit(unitState);
                unitState.coordinates = worldCoordinates;
                bool hasNoBridge = true;
                // if (tile2.improvement != null)
                // {
                //     if (gameState.GameLogicData.TryGetData(tile2.improvement.type, out ImprovementData improvementData))
                //     {
                //         if (improvementData.HasAbility(ImprovementAbility.Type.Bridge))
                //             hasNoBridge = false;
                //     }
                // }
                if(tile2.HasImprovement(ImprovementData.Type.Bridge) || tile2.HasEffect(TileData.EffectType.Algae)) // I NEED TO CHECK BRIDGE ABIL INSTEAD
                {
                    hasNoBridge = false;
                }
                if (hasNoBridge && !unitData.IsAquatic() && !unitState.HasAbility(UnitAbility.Type.Fly, gameState) && tile2.IsWater
                    && PlayerExtensions.HasAbility(playerState, EnumCache<PlayerAbility.Type>.GetType("waterembark"),gameState))
                {
                    gameState.ActionStack.Add(new EmbarkAction(__instance.PlayerId, worldCoordinates));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EmbarkAction), nameof(EmbarkAction.Execute))]
        private static void EmbarkAction_ExecuteDefault_Postfix(EmbarkAction __instance, GameState gameState)
        {
            PlayerState playerState;
            if (gameState.TryGetPlayer(__instance.PlayerId, out playerState))
            {
                TileData tile = gameState.Map.GetTile(__instance.Coordinates);
                UnitState unitState = tile.unit;
                if (unitState != null && playerState.HasAbility(EnumCache<PlayerAbility.Type>.GetType("dashembark"), gameState))
                {
                    unitState.moved = false;
                    unitState.attacked = false;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TileData), nameof(TileData.GetMovementCost))]
        private static bool TileData_GetMovementCost(
            ref int __result,
            TileData __instance,
            MapData map,
            TileData fromTile,
            PathFinderSettings settings)
        {
            const int BLOCKED   = 1000;
            const int TILE_COST = 10;
            __result = TILE_COST;

            UnitState unit = settings.unit;
            UnitData unitData = settings.unitData;
            byte playerId = settings.playerState.Id;

            // No unit
            if (unit == null)
            {
                if(settings.shouldFollowTransportPaths && (__instance.HasRoad || __instance.hasRoute))
                    __result /= 2;
                // wait i just realized that some pathfinder shi never triggers cuz
                // sherlock this shit was never in actual game like dawg WHAT????
                // reeferring to shouldFollowTransportPaths
                return false;
            }

            // Water / embark
            if (settings.isRequiredToUsePortToGoIntoWater &&
                __instance.IsWater &&
                __instance.HasEmbarkImprovement(settings.gameState))
            {
                __result = BLOCKED;
                return false;
            }

            if (!__instance.IsWater &&
                unit.HasAbility(UnitAbility.Type.Carry, settings.gameState))
            {
                __result = BLOCKED;
                return false;
            }

            // ZOC
            if (!settings.shouldAllowOccupiedTiles &&
                !unitData.HasAbility(UnitAbility.Type.Sneak) &&
                !unitData.HasAbility(UnitAbility.Type.Hide))
            {
                foreach (TileData neighbor in map.GetTileNeighbors(__instance.coordinates))
                {
                    UnitState neighborUnit = neighbor.unit;
                    if (neighborUnit == null)
                        continue;

                    if (neighborUnit.owner == playerId)
                        continue;

                    if (settings.playerState.HasPeaceWith(neighborUnit.owner))
                        continue;

                    if (neighborUnit.HasEffect(UnitEffect.Invisible))
                        continue;

                    __result = BLOCKED;
                    return false;
                }
            }

            // Fly / Creep
            if (unitData.HasAbility(UnitAbility.Type.Fly) ||
                unitData.HasAbility(UnitAbility.Type.Creep) ||
                unit.HasEffect(UnitEffect.Boosted))
            {
                __result = TILE_COST;
                return false;
            }

            // From water to land
            if (fromTile.IsWater &&
                !fromTile.HasImprovement(ImprovementData.Type.Bridge) &&
                !__instance.IsWater &&
                !__instance.HasImprovement(ImprovementData.Type.City))
            {
                __result = BLOCKED;
                return false;
            }

            // Roads REEEEEEE FUCKING WRITE......é
            if (__instance.HasRoadTo(fromTile, settings.gameState, unit.owner) &&
                __instance.terrain != TerrainData.Type.Ice &&
                !unitData.HasAbility(UnitAbility.Type.Skate) &&
                !unitData.HasAbility(UnitAbility.Type.Swim))
            {
                __result /= 2;
                return false;
            }

            bool hasRoughTerrain = ( __instance.improvement != null && settings.gameState.GameLogicData.TryGetData(__instance.improvement.type, out ImprovementData improvementData) 
                && improvementData.HasAbility(ImprovementAbility.Type.Slow)) ||__instance.terrain == TerrainData.Type.Mountain;

            // Condition for tiles to cost 30
            if (hasRoughTerrain)
            {
                __result *= 3;
                return false;
            }

            if(__instance.terrain == TerrainData.Type.Forest)
            {
                __result *= 2;
                return false;
            }
            // Ice / skate / slide / polarism
            bool canSlide =
                settings.unitData.HasAbility(EnumCache<UnitAbility.Type>.GetType("slide")) ||
                settings.playerState.availableTech.Contains(TechData.Type.Polarism);

            if (canSlide && __instance.terrain == TerrainData.Type.Ice)
            {
                __result /= 2;
                return false;
            }
            else if (unitData.HasAbility(UnitAbility.Type.Skate))
            {
                __result *= 2;
                return false;
            }

            // Swim units on land (Kill me why does swim still exist why does swim still exist why does
            // swoim stikl exist i actually manually type it and dont cntrlc cntrlv why does swim stikll exist)
            if (!__instance.IsWater &&
                unitData.HasAbility(UnitAbility.Type.Swim))
            {
                __result *= 2;
                return false;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.CanExplode))]
        private static bool UnitDataExtensions_CanExplode(ref bool __result, UnitState unit, GameState gameState)
        {
            __result = unit.CanAttack() && unit.owner == gameState.CurrentPlayer && unit.HasAbility(UnitAbility.Type.Explode, gameState);
            return false;
        }

        // I removed logic which was checking whether unit has attacked before being pushed on water tile. Too complicated.
        // I will fix other stuff and then come up with better solution.
    }
}