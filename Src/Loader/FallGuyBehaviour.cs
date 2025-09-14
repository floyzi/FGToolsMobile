using FG.Common;
using FG.Common.Character;
using FGClient;
using FGClient.UI;
using Levels.Progression;
using Levels.SeeSaw;
using NOTFGT.Logic;
using System;
using System.Linq;
using UnityEngine;

namespace NOTFGT.Loader
{
    public class FallGuyBehaviour : MonoBehaviour
    {
        public FallGuyBehaviour(IntPtr ptr) : base(ptr) { }

        public static FallGuyBehaviour FGBehaviour;

        public GameObject spawnpoint;
        public GameObject FallGuy;
        public FallGuysCharacterController FGCC;
        public MPGNetObject FGMPG;
        string gamemodeType;
        public const int PeakId = 102;

        public int PlayerTeamId = -1;

        public float respawnPos = 75f;

        bool finishedEndRoundAct = false;

        public static void Create()
        {
            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().First(x => x.IsLocalPlayer);
            if (player == null)
                return;

            var inst = player.gameObject.AddComponent<FallGuyBehaviour>();
            FGBehaviour = inst;

            inst.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            inst.FallGuy = inst.gameObject;
            inst.FGCC = inst.gameObject.GetComponent<FallGuysCharacterController>();
            inst.FGMPG = inst.gameObject.GetComponent<MPGNetObject>();

            inst.gamemodeType = RoundLoaderService.CGM._round.Archetype.Id.Split('_')[1];
            var vfxplayer = inst.gameObject.GetComponent<FallGuyVFXController>();
            vfxplayer.InjectCameraScreenController(Resources.FindObjectsOfTypeAll<CameraScreenVFXController>().Last());

            inst.PreFixObstacles();
            inst.CalculateRespawnPos();
        }

        public void LoadGPActions()
        {
            NOTFGTools.Instance.GUIUtil.UpdateGPActions(new()
            {
                { RespawnPlayer, "Respawn" },
                { Checkpoint, "Checkpoint" },
                { ResetCheckpointPos, "Reset Checkpoint" },
            });
        }

        void PreFixObstacles()
        {
            foreach (COMMON_SeeSaw360 seesaw in Resources.FindObjectsOfTypeAll<COMMON_SeeSaw360>())
            {
                seesaw._rb = seesaw.gameObject.GetComponent<Rigidbody>();
                seesaw.LimitAngularVelocity();
            }
        }

        void CalculateRespawnPos()
        {
            foreach (StaticGeometryHashID testCol in Resources.FindObjectsOfTypeAll<StaticGeometryHashID>())
            {
                if (testCol.transform.position.y < respawnPos)
                {
                    respawnPos = testCol.transform.position.y;
                }
            }

            respawnPos -= 10f;
        }

        void OnDestroy()
        {
            NOTFGTools.Instance.GUIUtil.UpdateGPActions(null);
        }

        void RespawnPlayer()
        {
            FGCC.TeleportMotorFunction.RequestTeleport(spawnpoint.transform.position, spawnpoint.transform.rotation);
            FGCC.TeleportMotorFunction.ForceState(FGCC.TeleportMotorFunction.GetState<MotorFunctionTeleportStateActive>().ID);
            FallGuy.GetComponent<FallGuysCharacterController>().ResetToDefaultState();
            FallGuy.gameObject.GetComponent<Rigidbody>().velocity = new Vector3(0, 5, 0);
        }

        void Checkpoint()
        {
            spawnpoint.transform.position = FallGuy.transform.position + new Vector3(0f, 1f, 0f);
            FallGuy.GetComponent<FallGuysCharacterController>().CharacterEventSystem.RaiseEvent(FGEventFactory.GetVfxCheckpointEvent());
        }

        void ResetCheckpointPos()
        {
            var list = Resources.FindObjectsOfTypeAll<MultiplayerStartingPosition>().ToList();
            var pos = list[UnityEngine.Random.Range(0, list.Count)];
            spawnpoint.transform.SetPositionAndRotation(pos.transform.position, pos.transform.rotation);    
        }

        void Update()
        {
            if (FGCC != null && FGCC.CachedTransform.position.y < respawnPos)
                RespawnPlayer();
        }

        void OnTriggerEnter(Collider collision)
        {
            if (collision.gameObject.GetComponent<EndZoneVFXTrigger>() != null || collision.gameObject.GetComponent<COMMON_ObjectiveReachEndZone>() != null)
            {
                if (!finishedEndRoundAct)
                {
                    QualifiedScreenViewModel.Show("qualified", new Action(() =>
                    {
                      
                    }), null);
                    finishedEndRoundAct = true;
                }
            }

            if (collision.gameObject.GetComponent<COMMON_PlayerEliminationVolume>() != null)
            {
                if (!finishedEndRoundAct)
                {
                    EliminatedScreenViewModel.Show("eliminated", null, new Action(() =>
                    {

                    }));
                    finishedEndRoundAct = true;
                }
            }
        }
    }
}
