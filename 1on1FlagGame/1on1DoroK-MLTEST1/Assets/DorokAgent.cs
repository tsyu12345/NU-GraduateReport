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
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        print("Initialized Agent For Team: " + team + "");
    }


    /**
    環境の観測
    */
    public override void CollectObservations(VectorSensor sensor) {
        // 自身の位置
        //FIXME:NullReferenceException: Object reference not set to an instance of an object
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


    /**
    エージェントの行動による報酬の設定(報酬設計)
    */
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        //Action size is 2
        var actions = actionBuffers.ContinuousActions;
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actions[0];
        controlSignal.z = actions[1];
        //agentRb.AddForce(controlSignal * m_Settings.agentRunSpeed);

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
