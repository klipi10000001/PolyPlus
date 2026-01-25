using HarmonyLib;
using Newtonsoft.Json.Linq;
using PolyPlus.Utils;
using Polytopia.Data;
using UnityEngine;

namespace PolyPlus
{
    public static class Main
    {
        private static Color32 bloomColor = new Color32(255, 105, 225, 255);

        public static void Load()
        {
            PolyMod.Loader.AddPatchDataType("tileEffect", typeof(TileData.EffectType));
            Harmony.CreateAndPatchAll(typeof(Main));
            Harmony.CreateAndPatchAll(typeof(ApiHandler));
            Harmony.CreateAndPatchAll(typeof(Diplomacy));
            Harmony.CreateAndPatchAll(typeof(Generation));
            Harmony.CreateAndPatchAll(typeof(Movement));
            Harmony.CreateAndPatchAll(typeof(Routes));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
        private static void GameLogicData_AddGameLogicPlaceholders(GameLogicData __instance, JObject rootObject)
        {
            Parser.Parse(rootObject);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitData), nameof(UnitData.getPromotionLimit))]
        private static void UnitData_getPromotionLimit(ref int __result, UnitData __instance, PlayerState player, GameState gameState)
        {
            if (__instance.unitAbilities.Contains(EnumCache<UnitAbility.Type>.GetType("staticplus")))
                __result = 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitPopup), nameof(UnitPopup.UnitData), MethodType.Setter)]
        private static void UnitPopup_UnitData_Set(UnitPopup __instance)
        {
            if (
                UIManager.Instance.CurrentScreen != UIConstants.Screens.TechTree
                && __instance.unit != null
                && __instance.unit.unitData.HasAbility(
                    EnumCache<UnitAbility.Type>.GetType("staticplus")
                )
            )
            {
                int killCount = (int)(__instance.Unit ? __instance.Unit!.UnitState.xp : 0);
                string oldProgressText = Localization.Get(
                    "world.unit.veteran.progress",
                    new Il2CppSystem.Object[] { killCount.ToString(), 0 }
                );
                string unitProgressText = Localization.Get(
                    "polyplus.unit.veteran.static.progress",
                    new Il2CppSystem.Object[] { killCount.ToString() }
                );
                Console.Write(oldProgressText);
                __instance.Description = __instance.Description.Replace(oldProgressText, unitProgressText);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExamineRuinsAction), nameof(ExamineRuinsAction.ExecuteDefault))]
        private static bool ExamineRuinsAction_ExecuteDefault(ExamineRuinsAction __instance, GameState gameState)
        {
            if (__instance.Reward == RuinsReward.City)
            {
                RuinsReward[] excludedValues = new RuinsReward[]
                {
                    RuinsReward.None,
                    RuinsReward.City,
                    RuinsReward.SuperUnit,
                    RuinsReward.Battleship,
                    RuinsReward.Seamonster,
                };
                Array values = Enum.GetValues(typeof(RuinsReward));

                var filteredValues = values
                    .Cast<RuinsReward>()
                    .Where(v => !excludedValues.Contains(v))
                    .ToArray();
                System.Random random = new System.Random(gameState.Seed);
                __instance.Reward = filteredValues[random.Next(filteredValues.Length)];
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.CalculateImprovementLevel))]
        private static void ActionUtils_CalculateImprovementLevel(ref int __result, GameState gameState, TileData tile)
        {
            if (tile.improvement == null)
                return;
            if (!gameState.GameLogicData.TryGetData(tile.improvement.type, out ImprovementData improvementData))
                return;
            if (improvementData.HasAbility(EnumCache<ImprovementAbility.Type>.GetType("halved")))
                __result /= 2;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetDefenceBonus))]
        private static void UnitDataExtensions_GetDefenceBonus(ref int __result, UnitState unit, GameState gameState)
        {
            if (__result == 15 && !unit.HasAbility(UnitAbility.Type.Fortify))
            {
                __result = 10;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EndTurnCommand), nameof(EndTurnCommand.ExecuteDefault))]
        public static void EndTurnCommand_ExecuteDefault(EndTurnCommand __instance, GameState state)
        {
            List<TileData> healOptions = new();
            for (int i = 0; i < state.Map.Tiles.Length; i++)
            {
                TileData tileData = state.Map.Tiles[i];
                if (tileData.owner == __instance.PlayerId && tileData.improvement != null)
                {
                    ImprovementData improvementData;
                    state.GameLogicData.TryGetData(tileData.improvement.type, out improvementData);
                    if (improvementData.HasAbility(EnumCache<ImprovementAbility.Type>.GetType("healplus")))
                    {
                        healOptions.AddRange(tileData.GetHealOptions(__instance.PlayerId, state, true).ToArray().ToList());
                    }
                }
            }

            healOptions = healOptions
                .GroupBy(t => t.coordinates)
                .Select(g => g.First())
                .ToList();

            foreach (TileData option in healOptions)
            {
                state.ActionStack.Add(new HealAction(__instance.PlayerId, option.coordinates, 40));
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
        private static void GameLogicData_CanBuild(ref bool __result, GameLogicData __instance, GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement)
        {
            if(improvement.HasAbility(EnumCache<ImprovementAbility.Type>.GetType("overcapper")) && tile.improvement != null)
            {
                if(Parser.improvementTerrainReq.ContainsKey(improvement.type))
                {
                    List<Data.TerrainRequirementsPlus> list = Parser.improvementTerrainReq[improvement.type];
                    foreach (Data.TerrainRequirementsPlus req in list)
                    {
                        if(req.improvement != ImprovementData.Type.None && tile.HasImprovement(req.improvement) && __instance.TryGetData(req.improvement, out ImprovementData requirementData))
                        {
                            if(tile.improvement.level == requirementData.MaxLevel(playerState, gameState) &&
                                !tile.HasEffect(EnumCache<TileData.EffectType>.GetType("overcap")))
                            {
                                __result = true;
                            }
                            else
                            {
                                __result = false;
                            }
                        }
                    }
                }
            }

            if (tile.unit == null)
                return;

            if (!__instance.TryGetData(tile.unit.type, out UnitData tileUnit))
                return;

            var embarkActionType = EnumCache<ImprovementAbility.Type>.GetType("embarkmanual");
            if (!improvement.HasAbility(embarkActionType))
                return;

            bool isLandBound = tileUnit.IsLandBound();
            bool canWaterEmbark = playerState.HasAbility(EnumCache<PlayerAbility.Type>.GetType("waterembark"), gameState);

            if (__result)
            {
                if (!isLandBound || !canWaterEmbark)
                {
                    __result = false;
                }
            }
            else if (tile.improvement != null && __instance.TryGetData(tile.improvement.type, out ImprovementData tileImprovement))
            {
                bool hasBridge = tileImprovement.HasAbility(ImprovementAbility.Type.Bridge);
                bool isFlooded = tile.HasEffect(TileData.EffectType.Flooded);

                if (isLandBound && canWaterEmbark && (hasBridge || isFlooded))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildAction), nameof(BuildAction.Execute))]
        private static void BuildAction_Execute(BuildAction __instance, GameState gameState)
        {
            if (gameState.GameLogicData.TryGetData(__instance.Type, out ImprovementData improvementData))
            {
                if (improvementData.HasAbility(EnumCache<ImprovementAbility.Type>.GetType("embarkmanual")))
                {
                    gameState.ActionStack.Add(new EmbarkAction(__instance.PlayerId, __instance.Coordinates));
                }

                if(improvementData.creates != null && improvementData.creates.Count > 0)
                {
                    foreach (var item in improvementData.creates)
                    {
                        if(item.effect == EnumCache<TileData.EffectType>.GetType("blooming"))
                        {
                            Tile tile = MapRenderer.instance.GetTileInstance(__instance.Coordinates);
                            tile.Render();
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandUtils), nameof(CommandUtils.GetUnitActions))]
        private static void CommandUtils_GetUnitActions(ref Il2CppSystem.Collections.Generic.List<CommandBase> __result, GameState gameState, PlayerState player, TileData tile, bool includeUnavailable)
        {
            UnitState unit = tile.unit;
            if (unit == null)
            {
                return;
            }
            if (unit.owner != player.Id)
            {
                return;
            }
            UnitData unitData;
            if (!gameState.GameLogicData.TryGetData(unit.type, out unitData))
            {
                return;
            }
            foreach (ImprovementData improvementData in gameState.GameLogicData.GetUnlockedImprovements(player))
            {
                if (improvementData.HasAbility(ImprovementAbility.Type.Manual)
                    && !unit.CanBuild()
                    && !unit.CanDisembark(gameState)
                    && gameState.GameLogicData.CanBuild(gameState, tile, player, improvementData)
                    && improvementData.HasAbility(EnumCache<ImprovementAbility.Type>.GetType("embarkmanual")))
                {
                    CommandBase? disembarkCommand = Utils.CommandsUtils.GetCommandOnCoordinate(gameState.CommandStack, CommandType.Disembark, tile.coordinates, Utils.CommandsUtils.CheckType.Turn);
                    if(disembarkCommand == null)
                    {
                        CommandUtils.AddCommand(gameState, __result, new BuildCommand(player.Id, improvementData.type, tile.coordinates), includeUnavailable);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.CanDisembark))]
        private static void UnitDataExtensions_CanDisembark(ref bool __result, UnitState unitState, GameState state)
        {
            if (!__result) return;

            CommandBase? command = Utils.CommandsUtils.GetCommandOnCoordinate(state.CommandStack, CommandType.Build, unitState.coordinates, Utils.CommandsUtils.CheckType.Turn);
            if(command != null)
            {
                BuildCommand buildCommand = command.Cast<BuildCommand>();
                if (state.GameLogicData.TryGetData(buildCommand.Type, out var improvementData) &&
                    improvementData.HasAbility(EnumCache<ImprovementAbility.Type>.GetType("embarkmanual")))
                {
                    __result = false;
                }
            }
        }

        private static void RemovePop(GameState gameState, TileData tile, byte playerId, int population)
        {
            tile.RemoveEffect(EnumCache<TileData.EffectType>.GetType("blooming"));
            if(tile.owner == 0)
                return;

            TileData cityTile = gameState.Map.GetTile(tile.rulingCityCoordinates);
            for (int i = 0; i < population; i++)
            {
                if(cityTile.HasImprovement(ImprovementData.Type.City))
                    gameState.ActionStack.Add(new DecreasePopulationAction(playerId, cityTile.coordinates, 200));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClearTileEffectAction), nameof(ClearTileEffectAction.Execute))]
        private static void ClearTileEffectAction_Execute(ClearTileEffectAction __instance, GameState gameState)
        {
            TileData tile = gameState.Map.GetTile(__instance.Target);
            if(tile == null) return;

            bool hasAlgae = __instance.Effect == TileData.EffectType.Algae;
            bool hasOvercap = __instance.Effect == EnumCache<TileData.EffectType>.GetType("overcap");
            if(hasAlgae)
                tile.RemoveEffect(EnumCache<TileData.EffectType>.GetType("blooming"));
            if(hasAlgae || hasOvercap)
                RemovePop(gameState, tile, __instance.PlayerId, 1);

            // if(__instance.Effect == EnumCache<TileData.EffectType>.GetType("overcap")) // I tried to create more generic solution
            // {
                // Normally I would want to make so all overcap improvements get their pop reward from orig impr level up reward.
                // if(tile.owner != 0) //  && tile.improvement != null && gameState.GameLogicData.TryGetData(tile.improvement.type, out ImprovementData improvementData)
                // {
                    // TileData city = gameState.Map.GetTile(tile.rulingCityCoordinates);
                    // if(city.HasImprovement(ImprovementData.Type.City))
                        // gameState.ActionStack.Add(new DecreasePopulationAction(__instance.PlayerId, city.coordinates, 200));
                        // int popReward = (int)improvementData.GetPopulationReward();
                        // foreach (var item in improvementData.growthRewards)
                        // {
                        //     popReward += item.population;
                        // }
                        // for (int i = 0; i < popReward; i++)
                        // {
                        //     gameState.ActionStack.Add(new DecreasePopulationAction(__instance.PlayerId, city.coordinates, 200));
                        // }
                // }
            // }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tile), nameof(Tile.Render), typeof(MapRenderContext))]
        private static void Tile_Render(Tile __instance, MapRenderContext mapRenderContext )
        {
            TileRender(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tile), nameof(Tile.Render), typeof(MapRenderContext), typeof(SkinVisualsTransientData))]
        private static void Tile_Render(Tile __instance, MapRenderContext ctx, SkinVisualsTransientData transientSkinningData)
        {
            TileRender(__instance);
        }

        private static void TileRender(Tile tile)
        {
            TileData tileData = tile.data;
            if(tileData.HasEffect(EnumCache<TileData.EffectType>.GetType("blooming")))
            {
                if(tile.algaeRenderer != null)
                {
                    tile.algaeRenderer.color = bloomColor;

                    if(tile.algaeRenderer.spriteRenderer != null)
                    {
                        tile.algaeRenderer.spriteRenderer.color = bloomColor;
                    }
                }
            }

            if(tileData.improvement != null && tileData.HasEffect(EnumCache<TileData.EffectType>.GetType("overcap")))
            {
                int newLevel =  tileData.improvement.level + 2; // Visual level != Logic level. Aka, level 0 in Logic is 1 in Visual. So instead of increment i have to add 2.
                Console.Write(newLevel);
                Sprite? sprite = PolyMod.Registry.GetSprite(EnumCache<ImprovementData.Type>.GetName(tileData.improvement.type), level: newLevel);
                if (sprite != null)
                {
                    tile.improvement.Sprite = sprite;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DestroyImprovementAction), nameof(DestroyImprovementAction.Execute))]
        private static bool DestroyImprovementAction_Execute(DestroyImprovementAction __instance, GameState state)
        {
            TileData tile = state.Map.GetTile(__instance.Coordinates);
            TileData.EffectType overcapEffect = EnumCache<TileData.EffectType>.GetType("overcap");
            if(tile.HasEffect(overcapEffect))
            {
                __instance.AddSubAction(new ClearTileEffectAction(
                    __instance.PlayerId,
                    __instance.Coordinates,
                    overcapEffect,
                    false
                ));
            }
            return true;
        }
    }
}
