using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EnvController : MonoBehaviour
{
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

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    /*警察グループ*/
    public SimpleMultiAgentGroup PoliceGroup;
    /*逃走者グループ*/
    public SimpleMultiAgentGroup CriminerGroup;


    private int m_ResetTimer;

    //牢屋オブジェクト
    public PrisonController Prison;

    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    
    void Start() {
        var PoliceCount = 0;
        var CriminerCount = 0;
        m_DorokSettings = FindObjectOfType<DorokSettings>();
        Prison = FindObjectOfType<PrisonController>();
        // Initialize TeamManager
        PoliceGroup = new SimpleMultiAgentGroup();
        CriminerGroup = new SimpleMultiAgentGroup();
        foreach (var item in AgentsList) {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Police) {
                PoliceGroup.RegisterAgent(item.Agent);
                PoliceCount++;
            } else {
                CriminerGroup.RegisterAgent(item.Agent);
                CriminerCount++;
            }
        }
        ResetScene();
    }


    void FixedUpdate() {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0) {
            PoliceGroup.GroupEpisodeInterrupted();
            CriminerGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    /**
    * 1ゲーム終了時に呼び出される
    */
    public void onGameEnd(Team team) {
        if (team == Team.Police) {
            //牢屋にとらえている犯人役の人数分だけ報酬を与える
            int count = Prison.GetCapturedAgents().Count;
            PoliceGroup.AddGroupReward(count);
        } else {
            //牢屋にとらえている犯人役の人数分だけ報酬を減らす
            int count = Prison.GetCapturedAgents().Count;
            CriminerGroup.AddGroupReward(-count);
        }
        PoliceGroup.EndGroupEpisode();
        CriminerGroup.EndGroupEpisode();
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
