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
    public int PoliceCount;
    /*逃走者グループ*/
    public SimpleMultiAgentGroup CriminerGroup;
    public int CriminerCount;

    private int m_ResetTimer;

    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    
    void Start() {
        PoliceCount = 0;
        CriminerCount = 0;
        m_DorokSettings = FindObjectOfType<DorokSettings>();
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
            onTimeUp();
            ResetScene();
        }
    }

    /**
    * 
    */
    public void onCaught(Team team, Agent capturedAgent) {
        if (team == Team.Police) {
            //牢屋にとらえている犯人役の人数分だけ報酬を与える
            int count = GetCapturedAgents().Count;
            //全ての逃走者を捕まえた場合、報酬を最大値1にするようにする
            float reward = count / CriminerCount;
            PoliceGroup.AddGroupReward(reward);
            PoliceGroup.GroupEpisodeInterrupted();
        } else {
            //牢屋にとらえている犯人役の人数分だけ報酬を減らす
            int count = GetCapturedAgents().Count;
            float reward = count / CriminerCount;
            CriminerGroup.AddGroupReward(-reward);
            //捕まった逃走者をグループから外す
            CriminerGroup.UnregisterAgent(capturedAgent);
            CriminerGroup.EndGroupEpisode();
        }
        //生き残っている逃走者の数がいない場合、シーンをリセットする
        List<GameObject> survivers = GetFreeCriminers();
        if(survivers.Count == 0) {
            ResetScene();
        }
    }


    public void onTimeUp() {
        //捕まっていない逃走者の分だけ負の報酬を与える
        int freeCount = GetFreeCriminers().Count;
        PoliceGroup.SetGroupReward(1f - freeCount/CriminerCount);
        CriminerGroup.SetGroupReward(-1f + freeCount/CriminerCount);
        int caughtCount = GetCapturedAgents().Count;
        PoliceGroup.AddGroupReward(caughtCount/CriminerCount);
        CriminerGroup.AddGroupReward(-caughtCount/CriminerCount);
        PoliceGroup.GroupEpisodeInterrupted();
        CriminerGroup.GroupEpisodeInterrupted();
    }


    /**
    * 捕らわれている逃走役エージェントを取得
    */
    public List<GameObject> GetCapturedAgents() {
        List<GameObject> capturedAgents = new List<GameObject>();
        foreach (GameObject agent in GameObject.FindGameObjectsWithTag("Criminer")) {
            if (agent.GetComponent<DorokAgent>().isCaptured) {
                capturedAgents.Add(agent);
            }
        }
        print("PrisonController: capturedAgents.Count = " + capturedAgents.Count);
        return capturedAgents;
    }


    public List<GameObject> GetFreeCriminers() {
        List<GameObject> freeCriminers = new List<GameObject>();
        foreach (GameObject agent in GameObject.FindGameObjectsWithTag("Criminer")) {
            if (agent.GetComponent<DorokAgent>().isCaptured == false) {
                freeCriminers.Add(agent);
            }
        }
        return freeCriminers;
    }

    /**
    * 捕らわれている逃走役エージェントを牢屋からランダムにフィールドに開放する
    */
    public void ReleaseCapturedAgents(List<GameObject> capturedAgents) {
        foreach (GameObject agent in capturedAgents) {
            agent.GetComponent<DorokAgent>().isCaptured = false;
            //エージェントを再登録
            CriminerGroup.RegisterAgent(agent.GetComponent<DorokAgent>());
            //属するフィールドのランダムな位置に移動
            //TODO: ここでフィールドの範囲を取得して、そこからランダムに位置を決める
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = agent.GetComponent<DorokAgent>().initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = agent.GetComponent<DorokAgent>().rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            agent.transform.SetPositionAndRotation(newStartPos, newRot);
            agent.GetComponent<Rigidbody>().velocity = Vector3.zero;
            agent.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }


    public void ResetScene() {
        m_ResetTimer = 0;
        print("ResetScene");
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
        //捕まっているフラグを戻す
        foreach (GameObject agent in GameObject.FindGameObjectsWithTag("Criminer")) {
            if (agent.GetComponent<DorokAgent>().isCaptured) {
                agent.GetComponent<DorokAgent>().isCaptured = false;
            }
        }
    }
}
