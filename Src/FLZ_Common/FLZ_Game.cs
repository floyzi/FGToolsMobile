using Il2Cpp;
using Il2CppFG.Common;
using Il2CppFG.Common.Character;
using Il2CppFG.Common.Character.MotorSystem;
using Il2CppFGClient;
using Il2CppFGClient.UI;
using NOTFGT.FLZ_Common.GUI;
using NOTFGT.FLZ_Common.Loader;
using NOTFGT.FLZ_Common.Localization;
using NOTFGT.FLZ_Common.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static Il2Cpp.GameStateEvents;
using static Il2CppFG.Common.CommonEvents;
using static Il2CppFGClient.GlobalGameStateClient;
using static NOTFGT.FLZ_Common.FLZ_ToolsManager;

namespace NOTFGT.FLZ_Common
{
    internal class FLZ_Game
    {
        internal struct PlayerMeta
        {
            internal PlayerMeta(PlayerInfoDisplay tag, string name, FallGuysCharacterController fgcc, string platfrom)
            {
                Tag = tag;
                Name = name;
                Platform = platfrom;
                FGCC = fgcc;
            }

            public PlayerInfoDisplay Tag;
            public string Name;
            public string Platform;
            public FallGuysCharacterController FGCC;
            public readonly MPGNetID NetId
            {
                get
                {
                    if (FGCC != null && FGCC.NetObject != null)
                        return FGCC.NetObject.NetID;

                    return default;
                }
            }
            public readonly bool LocalPlayer => FGCC != null && FGCC.IsLocalPlayer;
        }

        const string AdvancedNamePattern = "[{0}] | {1} | [{2}]";
        readonly List<PlayerMeta> PlayerMetas = [];
        readonly Dictionary<string, string> NamesMap = [];
        CharacterControllerData ActiveFGCCData;
        object[] DefaultFGCCData = null;

        bool playersHidden = false;

        internal bool SeePlayerPlatforms;
        internal bool UseCaptureTools;
        internal float RunSpeedModifier;
        internal float JumpYModifier;
        internal float DiveSens;
        internal bool DisableFGCCCheck;
        internal bool DisableAFK;
        internal float GravityModifier;
        internal float DiveForce;
        internal float DiveForceInAir;


        public FLZ_Game()
        {
            OnMenuEnter += MenuEvent;
            OnRoundStarts += OnGameplayBegin;
            OnIntroStarts += OnIntroStart;
            OnIntroEnds += OnIntroEnd;
            OnGUIInit += OnInitOverlay;
            OnSpectatorEvent += OnSpectate;
        }

        internal void RegisterTag(PlayerInfoDisplay tag)
        {

        }

        internal void SetNames()
        {
            if (PlayerMetas == null || PlayerMetas.Count == 0)
                return;

            foreach (var name in PlayerMetas)
            {
                var cleanName = Regex.Replace(name.Name, @"<size=(.*?)>|</size>", "");

                NamesMap.TryAdd(cleanName, name.Name);

                if (SeePlayerPlatforms)
                    name.Tag.SetText(string.Format(AdvancedNamePattern, name.FGCC.NetObject.NetID, cleanName, name.Platform));
                else
                    name.Tag.SetText($"{cleanName}");
            }
        }

        private void MenuEvent()
        {
            foreach (var fgcc in Resources.FindObjectsOfTypeAll<FallGuysCharacterController>())
                fgcc.MotorAgent._motorFunctionsConfig = MotorAgent.MotorAgentConfiguration.Default;
        }

        void OnGameplayBegin()
        {
            playersHidden = false;
#if CHEATS
            SetupFGCCData();
            RollFGCCSettings();
#endif
            Instance.GUIUtil.UpdateGPUI(true, false);

            if (IsInRoundLoader)
            {
                FallGuyBehaviour.FGBehaviour.LoadGPActions();
                Instance.RoundLoader.RoundLoadingAllowed = true;
                FallGuyBehaviour.FGBehaviour.FallGuy.GetComponent<Rigidbody>().isKinematic = false;
                FallGuyBehaviour.FGBehaviour.spawnpoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(FallGuyBehaviour.FGBehaviour.spawnpoint.GetComponent<BoxCollider>());
                FallGuyBehaviour.FGBehaviour.spawnpoint.name = "Checkpoint";
                FallGuyBehaviour.FGBehaviour.spawnpoint.transform.SetPositionAndRotation(FallGuyBehaviour.FGBehaviour.FallGuy.transform.position, FallGuyBehaviour.FGBehaviour.FallGuy.transform.rotation);
            }
            else
            {
                //something for future
                Instance.GUIUtil.UpdateGPActions(null);
            }
        }

        void OnIntroStart()
        {
#if CHEATS
            ResetFGCCData();
            GlobalGameStateClient.Instance.GameStateView.GetCharacterDataMonitor()._timeToRunNextCharacterControllerDataCheck = float.MaxValue;
#endif
        }

        void OnIntroEnd()
        {
            SetNames();

            if (RoundLoaderService.CGM == null || !IsInRoundLoader)
                return;

            Instance.RoundLoader.RoundLoadingAllowed = true;
            FallGuyBehaviour.Create();
            SpeedBoostManager SPM = FallGuyBehaviour.FGBehaviour.FGCC.SpeedBoostManager;
            SPM.SetAuthority(true);
            SPM.SetCharacterController(FallGuyBehaviour.FGBehaviour.FGCC);


            RoundLoaderService.GameLoading.HandleGameServerStartGame(new GameMessageServerStartGame(0, RoundLoaderService.CGM.CurrentGameSession.EndRoundTime, 0, 1, RoundLoaderService.CGM.GameRules.NumPerVsGroup, 1, 0));

        }

        void OnInitOverlay()
        {
            if (IsInRoundLoader)
                return;
        }

        void OnSpectate()
        {
#if CHEATS
            ForceUnHidePlayers();
#endif
        }

        void OnRoundOverEvent(OnRoundOver evt)
        {
#if CHEATS
            ForceUnHidePlayers();
#endif
            PlayerMetas.Clear();
        }

#if CHEATS
        public void SetupFGCCData()
        {

            ActiveFGCCData = Resources.FindObjectsOfTypeAll<CharacterControllerData>().FirstOrDefault();

            if (ActiveFGCCData != null && DefaultFGCCData == null)
            {
                DefaultFGCCData = new object[255];
                DefaultFGCCData[0] = ActiveFGCCData.normalMaxSpeed;
                DefaultFGCCData[1] = ActiveFGCCData.jumpForceUltimateParty;
                DefaultFGCCData[2] = ActiveFGCCData.divePlayerSensitivity;
                DefaultFGCCData[3] = ActiveFGCCData.maxGravityVelocity;
                DefaultFGCCData[4] = ActiveFGCCData.diveForce;
                DefaultFGCCData[5] = ActiveFGCCData.airDiveForce;
            }
    }

        void ResetFGCCData()
        {
            if (ActiveFGCCData != null && DefaultFGCCData != null)
            {
                ActiveFGCCData.normalMaxSpeed = (float)DefaultFGCCData[0];
                ActiveFGCCData.jumpForceUltimateParty = (Vector3)DefaultFGCCData[1];
                ActiveFGCCData.divePlayerSensitivity = (float)DefaultFGCCData[2];
                ActiveFGCCData.maxGravityVelocity = (float)DefaultFGCCData[3];
                ActiveFGCCData.diveForce = (float)DefaultFGCCData[4];
                ActiveFGCCData.airDiveForce = (float)DefaultFGCCData[5];
            }
    }
        public void RollFGCCSettings()
        {
            //REWRITE

            //var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);
            //if (player != null)
            //{
            //    var motorAgent = player.MotorAgent;

            //    if (Instance.SettingsMenu.GetValue<bool>(ToolsMenu.DisableMonitorCheck))
            //    {
            //        Vector3 defJump = (Vector3)DefaultFGCCData[1];
            //        ActiveFGCCData.normalMaxSpeed = (float)DefaultFGCCData[0] + float.Parse(Instance.SettingsMenu.GetValue<object>(ToolsMenu.RunSpeedModifier).ToString());
            //        ActiveFGCCData.jumpForceUltimateParty = new Vector3(defJump.x, defJump.y + float.Parse(Instance.SettingsMenu.GetValue<object>(ToolsMenu.JumpYModifier).ToString()), defJump.z); ;
            //        ActiveFGCCData.divePlayerSensitivity = float.Parse(Instance.SettingsMenu.GetValue<object>(ToolsMenu.DiveSens).ToString());
            //        ActiveFGCCData.maxGravityVelocity = float.Parse(Instance.SettingsMenu.GetValue<object>(ToolsMenu.GravityChange).ToString());
            //        ActiveFGCCData.diveForce = float.Parse(Instance.SettingsMenu.GetValue<object>(ToolsMenu.DiveForce).ToString());
            //        ActiveFGCCData.airDiveForce = float.Parse(Instance.SettingsMenu.GetValue<object>(ToolsMenu.DiveInAirForce).ToString());
            //    }
            //    else
            //        ResetFGCCData();

            //    motorAgent.GetMotorFunction<MotorFunctionJump>()._jumpForce = ActiveFGCCData.jumpForceUltimateParty;
            //}
        }


        public void TeleportToFinish()
        {
            if (!DefaultCheck())
                return;

            var finish = Resources.FindObjectsOfTypeAll<COMMON_ObjectiveReachEndZone>().FirstOrDefault();
            if (finish == null)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_no_finish"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Disruptive);
                return;
            }

            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);
            player.transform.SetPositionAndRotation(finish.transform.position + new Vector3(0, 10f, 0), finish.transform.rotation);
        }

        public void TeleportToSafeZone()
        {
            if (!DefaultCheck())
                return;

            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);

            var safeZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeZone.name = "SafeZone";
            safeZone.transform.localScale = new Vector3(200, 5, 200);
            safeZone.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 150, player.transform.position.z);
            player.transform.position = safeZone.transform.position + new Vector3(0, 10, 0);
            safeZone.GetComponent<MeshRenderer>().enabled = false;
        }

        public void TeleportToRandomPlayer()
        {
            if (!DefaultCheck())
                return;

            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);

            var target = players[UnityEngine.Random.RandomRange(0, players.Count)];
            player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        }

        public void TogglePlayers()
        {
            if (!DefaultCheck())
                return;

            playersHidden = !playersHidden;
            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            foreach (var player in players)
                player.gameObject.SetActive(!playersHidden);
        }

        bool DefaultCheck()
        {
            if (GlobalGameStateClient.Instance.IsInMainMenu)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_in_menu"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Disruptive);
                return false;
            }
            if (!GlobalGameStateClient.Instance.GameStateView.IsGamePlaying)
            {
                FLZ_Extensions.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_game_not_active"), UIModalMessage.ModalType.MT_OK, UIModalMessage.OKButtonType.Disruptive);
                return false;
            }
            
            return true;
        }

        void ForceUnHidePlayers()
        {
            playersHidden = false;
            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            foreach (var player in players)
                player.gameObject.SetActive(!playersHidden);
        }
#endif
    }
}
