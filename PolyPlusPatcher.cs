using HarmonyLib;
using Il2CppSystem.IO;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using UnityEngine;

namespace PolyPlus {
    public class PolyPlusPatcher
    {
        private static string version = "0.0.9";
        private static string branch = "waterembark";
        private static int _polyplusAutoidx = 480;
        private static Dictionary<string, int> _polyplusDict = new Dictionary<string, int>();
        public static void Load()
        {
            Console.WriteLine("Loading PolyPlus Polyscript of version " + version + " of branch " + branch + "...");
            CreateEnumCaches();
            Harmony.CreateAndPatchAll(typeof(PolyPlusPatcher));
            Console.WriteLine("PolyPlus Polyscript Loaded!");
        }

        internal static void CreateEnumCaches()
        {
            EnumCache<PlayerAbility.Type>.AddMapping("waterembark", (PlayerAbility.Type)750);
			EnumCache<PlayerAbility.Type>.AddMapping("waterembark", (PlayerAbility.Type)750);
            EnumCache<UnitAbility.Type>.AddMapping("polyplusstatic", (UnitAbility.Type)_polyplusAutoidx);
			EnumCache<UnitAbility.Type>.AddMapping("polyplusstatic", (UnitAbility.Type)_polyplusAutoidx);
            _polyplusDict.Add("polyplusstatic", _polyplusAutoidx);
            _polyplusAutoidx++;
        }

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UnitData), nameof(UnitData.getPromotionLimit))]
        private static void UnitData_getPromotionLimit(ref int __result, UnitData __instance, PlayerState player, GameState gameState)
		{
            if(__instance.unitAbilities.Contains((UnitAbility.Type)_polyplusDict["polyplusstatic"])){
                __result = 0;
            }
		}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnitPopup), nameof(UnitPopup.UnitData), MethodType.Setter)]
        private static void UnitPopup_UnitData_Set(UnitPopup __instance)
        {
            Vector2 anchoredPosition = __instance.iconContainer.anchoredPosition;
            string improvementName = string.Empty;
            if (__instance.Unit != null && GameManager.GameState != null && GameManager.GameState.Map != null)
            {
                TileData tile = GameManager.GameState.Map.GetTile(__instance.Unit.UnitState.home);
                if (tile != null && tile.HasImprovement(ImprovementData.Type.City))
                {
                    improvementName = tile.improvement.name;
                }
            }
            string unitDescription = string.IsNullOrEmpty(improvementName) ? string.Empty : string.Format("{0}\n", Localization.Get("world.unit.info.from", new Il2CppSystem.Object[] { improvementName }));
            string unitProgressText;
            int killCount = (int)(__instance.Unit ? __instance.Unit.UnitState.xp : 0);
            if (UIManager.Instance.CurrentScreen != UIConstants.Screens.TechTree && __instance.unit != null && __instance.unit.unitData.HasAbility((UnitAbility.Type)_polyplusDict["polyplusstatic"]))
            {
                unitProgressText = Localization.Get("polyplus.unit.veteran.static.progress", new Il2CppSystem.Object[]
                {
                    killCount.ToString(),
                });
                __instance.Description = string.Format("{0}{1}", unitDescription, unitProgressText);
                anchoredPosition.x = 48f;
            }
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MoveAction), nameof(MoveAction.ExecuteDefault))]
        private static void MoveAction_ExecuteDefault(MoveAction __instance, GameState gameState)
	    {
            UnitState unitState;
            PlayerState playerState;
            UnitData unitData;
            if (gameState.TryGetUnit(__instance.UnitId, out unitState) && gameState.TryGetPlayer(__instance.PlayerId, out playerState) && gameState.GameLogicData.TryGetData(unitState.type, out unitData))
            {
                WorldCoordinates worldCoordinates = __instance.Path[0];
                TileData tile2 = gameState.Map.GetTile(worldCoordinates);
                tile2.SetUnit(unitState);
                unitState.coordinates = worldCoordinates;
                if (!unitData.IsAquatic() && !unitState.HasAbility(UnitAbility.Type.Fly, gameState) && tile2.IsWater && PlayerExtensions.HasAbility(playerState, (PlayerAbility.Type)750, gameState))
                {
                    gameState.ActionStack.Add(new EmbarkAction(__instance.PlayerId, worldCoordinates));
                }
            }
        }

        internal static string GetJTokenName(JToken token, int n = 1)
		{
			return token.Path.Split('.')[^n];
		}
    }
}