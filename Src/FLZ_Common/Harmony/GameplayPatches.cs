using HarmonyLib;
using Il2CppFG.Common;
using Il2CppFG.Common.Character;
using Il2CppFG.Common.Messages;
using Il2CppFGClient;
using NOTFGT.FLZ_Common.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppFG.Common.LODs.LodController;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;

namespace NOTFGT.FLZ_Common.Harmony
{
    internal class GameplayPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CaptureToolsManager), nameof(CaptureToolsManager.CanUseCaptureTools), MethodType.Getter)]
        public static void CanUseCaptureTools(ref bool __result)
        {
            __result = Instance.InGameManager.UseCaptureTools;
        }

        [HarmonyPatch(typeof(PlayerInfoHUDBase), nameof(PlayerInfoHUDBase.SpawnPlayerTag)), HarmonyPostfix]
        static void SpawnPlayerTag(PlayerInfoHUDBase __instance, SpawnPlayerTagEvent spawnEvent)
        {
            FLZ_ToolsManager.Instance.InGameManager.RegisterTag(__instance._spawnedInfoObjects[^1].playerInfo, spawnEvent);
        }

        [HarmonyPatch(typeof(ClientGameManager), nameof(ClientGameManager.SetReady)), HarmonyPostfix]
        static void SetReady(ClientGameManager __instance, PlayerReadinessState readinessState, string sceneName, string levelHash)
        {
            if (!IsInRoundLoader)
                return;

            switch (readinessState)
            {
                case PlayerReadinessState.LevelLoaded:
                    var gameLoading = RoundLoaderService.GameLoading;
                    RoundLoaderService.CGM = gameLoading._clientGameManager;
                    RoundLoaderService.PTM = gameLoading._clientGameManager._playerTeamManager;

                    gameLoading._clientGameManager.GameRules.PreparePlayerStartingPositions(1);
                    var pos = gameLoading._clientGameManager.GameRules.PickStartingPosition(0, 0, -1, 0, false);

                    //SLOP
                    var spawn = new GameMessageServerSpawnObject();
                    spawn.Init(new()
                    {
                        NetID = GlobalGameStateClient.Instance.NetObjectManager.GetNextNetID(),
                        _additionalSpawnData = new PlayerSpawnData(
                            GlobalGameStateClient.Instance.GetLocalClientNetworkID(),
                            1,
                            GlobalGameStateClient.Instance.GetLocalClientAccountID(),
                            "android_ega",
                            GlobalGameStateClient.Instance.GetLocalPlayerName(),
                            "",
                            0,
                            -1,
                            "",
                            0,
                            false,
                            GlobalGameStateClient.Instance.PlayerProfile.CustomisationSelections),
                        _creationMode = NetObjectCreationMode.Spawn,
                        _lodControllerBehaviour = LodControllerBehaviour.Default,
                        _prefabHash = -491682846,
                        _scale = Vector3.one,
                        _spawnObjectType = EnumSpawnObjectType.PLAYER,
                        _syncTransform = false,
                        _syncScale = false,
                        _position = pos.transform.position,
                        _rotation = pos.transform.rotation,
                        _useUnifiedSetup = true,
                    }, true);

                    RoundLoaderService.CGM._clientPlayerManager._localPlayerCount = 1;
                    RoundLoaderService.CGM._delayedSpawnNetObjectMessages.Enqueue(spawn);

                    CGMDespatcher.process(new GameMessageServerEventGeneric()
                    {
                        Type = GameMessageServerEventGeneric.EventType.StartIntroCameras,
                        Data = new()
                        {
                            StrParam1 = default,
                            StrParam2 = default,
                            FloatParam1 = default,
                            FloatParam2 = default,
                            MpgNetId = default,
                            ObjectArray = new(1)
                        }
                    });
                    break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MotorFunctionJump), nameof(MotorFunctionJump.CanJump))]
        public static bool CanJump(ref bool __result)
        {
            if (FLZ_Game.IsAirJumpEnabled)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
