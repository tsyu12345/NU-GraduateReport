using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


public class EnvCtrler : MonoBehaviour {

    private DorokSettings m_DorokSettings;

    [System.Serializable]
    public class PlayerInfo {
        public DorokAgent Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }


    /*警察グループ*/
    private SimpleMultiAgentGroup PoliceGroup;
    /*逃走者グループ*/
    private SimpleMultiAgentGroup CriminerGroup;


    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private int m_ResetTimer;

    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    
    public void Start() {
        m_DorokSettings = FindObjectOfType<DorokSettings>();
        // Initialize TeamManager
        PoliceGroup = new SimpleMultiAgentGroup();
        CriminerGroup = new SimpleMultiAgentGroup();
        
        foreach (var item in AgentsList) {
            if (item.Agent.team == Team.Police) {
                PoliceGroup.RegisterAgent(item.Agent);
            } else {
                CriminerGroup.RegisterAgent(item.Agent);
            }
        }
    }


    public void FixedUpdate() {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0) {
            PoliceGroup.GroupEpisodeInterrupted();
            CriminerGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    /**
    1マッチ終了時の報酬処理
    （逃走者が全員捕まるか、制限時間切れの場合）
    */
    public void MatchEnd(Team team) {
        if (team == Team.Police) {
            PoliceGroup.AddGroupReward(1);
            CriminerGroup.AddGroupReward(-1);
        } else {
            PoliceGroup.AddGroupReward(-1);
            CriminerGroup.AddGroupReward(1);
        }

        PoliceGroup.GroupEpisodeInterrupted();
        CriminerGroup.GroupEpisodeInterrupted();
        ResetScene();
    }


    public void ResetScene() {
        m_ResetTimer = 0;

        //Reset Agents
        foreach (var item in AgentsList) {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }
    }
}
