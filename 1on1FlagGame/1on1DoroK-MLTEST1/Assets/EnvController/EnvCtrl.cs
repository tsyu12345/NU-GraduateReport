using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public enum Team {
    Police = 0,
    Criminer = 1
}


public class EnvCtrler : MonoBehaviour {

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    /*警察グループ*/
    private SimpleMultiAgentGroup PoiliceGroup;
    /*逃走者グループ*/
    private SimpleMultiAgentGroup CriminerGroup;

    private EnvCtrler Ctrl = new EnvCtrler();
    
    public void Start() {

        PoliceGroup = new SimpleMultiAgentGroup();
        CriminerGroup = new SimpleMultiAgentGroup();

        foreach (var item in AgentsList) {
            if (item.Agent.team == Team.Police) {
                PoliceAgentGroup.RegisterAgent(item.Agent);
            } else {
                CriminerAgentGroup.RegisterAgent(item.Agent);
            }
        }
    }

    /**
    * エージェントを警察or逃走者グループに分ける
    */
    private void DividingAgents() {
        
    }

    public void FixedUpdate() {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0) {
            PoliceAgentGroup.GroupEpisodeInterrupted();
            CriminerAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    /**
    1マッチ終了時の報酬処理
    （逃走者が全員捕まるか、制限時間切れの場合）
    */
    public void MatchEnd(Team team) {
        if (team == Team.Police) {
            PoliceAgentGroup.AddGroupReward(1);
            CriminerAgentGroup.AddGroupReward(-1);
        } else {
            PoliceAgentGroup.AddGroupReward(-1);
            CriminerAgentGroup.AddGroupReward(1);
        }

        PoliceAgentGroup.GroupEpisodeInterrupted();
        CriminerAgentGroup.GroupEpisodeInterrupted();
        ResetScene();
    }





}
