using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public enum Team {
    Police = 0,
    Criminer = 1
}


public class DorokAgent: Agent {

    public Position position;

    [HideInInspector]
    public Team team;

    [HideInInspector]
    public Rigidbody agentRb;
    DorokSettings m_Settings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;


    public override void Initialize() {
        EnvCtrler envController = GetComponentInParent<EnvCtrler>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        if (m_BehaviorParameters.TeamId == (int)Team.Police) {
            team = Team.Police;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        } else {
            team = Team.Criminer;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
        }

        m_Settings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }


    public void MoveAgent(ActionSegment<int> act) {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];


        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,ForceMode.VelocityChange);
    }


    public override void OnActionReceived(ActionBuffers actionBuffers) {

        MoveAgent(actionBuffers.DiscreteActions);
    }

}