using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public enum Team {
    Police = 0,
    Criminer = 1
}


public class DorokAgent: Agent {

    [HideInInspector]
    public Team team;

    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector]
    public Rigidbody agentRb;
    DorokSettings m_Settings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;

    /**
    エージェントの初期化
    */
    public override void Initialize() {
        
        EnvController envController = GetComponentInParent<EnvController>();
        if (envController != null) {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        } else {
            m_Existential = 1f / MaxStep;
        }
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

        m_Settings = FindObjectOfType<DorokSettings>();
        print("INIT M_SETTING" + m_Settings);
        if(m_Settings == null) {
            print("Failed to find DorokSettings");
        }
        agentRb = GetComponent<Rigidbody>();
        
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        print("Initialized one Agent For Team: " + team + "");
    }




    /**
    環境の観測
    */
    public override void CollectObservations(VectorSensor sensor) {
        
        // 自身の位置
        sensor.AddObservation(agentRb.velocity.x);
        sensor.AddObservation(agentRb.velocity.z);

        // 敵エージェントの位置
        var tagname = team == Team.Police ? "Criminer" : "Police";
        GameObject[] enemys = GameObject.FindGameObjectsWithTag(tagname);

        foreach(GameObject enemy in enemys) {
            sensor.AddObservation(enemy.transform.position.x);
            sensor.AddObservation(enemy.transform.position.z);
        }

    }


    public void MoveAgent(ActionSegment<int> act) {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        print("m_Settings" + m_Settings+ "");
        print("ForceMode.VelocityChange" + ForceMode.VelocityChange+ "");
        agentRb.AddForce(dirToGo * m_Settings.agentRunSpeed, ForceMode.VelocityChange);
    }


    /**
    エージェントの行動による報酬の設定(報酬設計)
    */
    public override void OnActionReceived(ActionBuffers actionBuffers) {

        MoveAgent(actionBuffers.DiscreteActions);

        //敵エージェントとの距離を算出
        var tagname = team == Team.Police ? "Criminer" : "Police";
        GameObject[] enemys = GameObject.FindGameObjectsWithTag(tagname);
        Vector3[] enemyPos = new Vector3[enemys.Length];
        for (int i = 0; i < enemys.Length; i++) {
            enemyPos[i] = enemys[i].transform.position;
        }
        var distanceToEnemy = Vector3.Distance(transform.position, enemyPos[0]);
        
        //敵エージェントとの距離が近いほど報酬を与える
        AddReward(1f / distanceToEnemy);
        //敵エージェントとの距離が遠いほどマイナス報酬を与える
        AddReward(-1f / distanceToEnemy);
        //警官が逃走役を捕まえたら報酬を与え,逃走役は報酬を引く。
        if (distanceToEnemy < 1f) {
            if (team == Team.Police) {
                AddReward(1f);
            } else {
                AddReward(-1f);
                //逃走役を初期位置にもどす。
                transform.position = initialPos;
            }
            EndEpisode();
        }


    }

    /**
    シーンの初期化
    */
    public override void OnEpisodeBegin() {
        // エージェントの初期位置を設定
        transform.position = initialPos;
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
    }

}
