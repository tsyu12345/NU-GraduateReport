using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

//todo:PoliceクラスとCrimmerクラスに継承分離させる
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

    //捕らわれているかどうか
    public bool isCaptured = false;
    PrisonController prisonController;

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
            rotSign = 1f;
        }

        m_Settings = FindObjectOfType<DorokSettings>();
        if(m_Settings == null) {
            throw new System.Exception("Failed to find DorokSettings");
        }
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ForwardSpeed = m_Settings.agentForwardSpeed;
        m_LateralSpeed = m_Settings.agentLateralSpeed;
        print("ForwardSpeed: " + m_ForwardSpeed + ", LateralSpeed: " + m_LateralSpeed + "");


        m_ResetParams = Academy.Instance.EnvironmentParameters;

        //牢屋のインスタンスを取得
        //prisonController = GameObject.Find("Prison").GetComponent<PrisonController>();

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
        print("Enemys: " + enemys.Length);

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

        switch (forwardAxis) {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis) {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis) {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_Settings.agentRunSpeed, ForceMode.VelocityChange);
    }


    /**
    エージェントの行動による報酬の設定(報酬設計)
    */
    public override void OnActionReceived(ActionBuffers actionBuffers) {

        MoveAgent(actionBuffers.DiscreteActions);
        if(team == Team.Police) {
            onActionPolice(actionBuffers);
        } else {
            onActionCriminer(actionBuffers);
        } 
        
    }

    /**
    * 敵エージェントのオブジェクトを返す
    */
    private GameObject[] GetEnemies(Team team) {
        var tagname = team == Team.Police ? "Criminer" : "Police";
        GameObject[] enemys = GameObject.FindGameObjectsWithTag(tagname);
        return enemys;
    }

    /**
    * 観測している敵エージェントの内、最短距離を計算し、最も近い敵エージェントを返す
    */
    private GameObject GetNearestEnemy(GameObject[] enemies) {
        GameObject nearestEnemy = null;
        float minDistance = float.MaxValue;
        foreach (GameObject enemy in enemies) {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance) {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }
        return nearestEnemy;
    }

    /**
    * 警察エージェントの報酬設計
    */
    private void onActionPolice(ActionBuffers actionBuffers) {
        GameObject[] enemys = GetEnemies(team);
        GameObject nearestEnemy = GetNearestEnemy(enemys);
        float distance = Vector3.Distance(transform.position, nearestEnemy.transform.position);
        // 逃走役エージェントとの距離が近いほど報酬を与える
        AddReward(1 - distance/m_Settings.rewardConstant);
        // 逃走役エージェントと接触した場合、捕まえたと判定し、報酬を与える
        if (distance < 1.42f) {
            AddReward(10.0f);
        }
        EndEpisode();

    }

    /**
    * 逃走役エージェントの報酬設計
    */
    private void onActionCriminer(ActionBuffers actionBuffers) {
        // 逃走役エージェントが捕まっている場合、牢屋の位置から動かないようにする
        if (isCaptured) {
            agentRb.velocity = Vector3.zero;
            AddReward(-1.0f);
        } else {
            GameObject[] enemys = GetEnemies(team);
            GameObject nearestEnemy = GetNearestEnemy(enemys);
            float distance = Vector3.Distance(transform.position, nearestEnemy.transform.position);
            // 警察エージェントとの距離が遠いほど報酬を与える
            AddReward(distance/m_Settings.rewardConstant);
            // 警察エージェントと接触した場合、捕まえられたと判定し、負の報酬を与える
            if (distance < 1.42f) {
                AddReward(-10.0f);
                isCaptured = true;
                //エージェントを所定の牢屋位置に移動させる
                //transform.position = prisonController.prisonPos;
            }
            //他の逃走役エージェントが捕まっていて、牢屋に接触した場合、仲間を解放し、報酬を与える
            /*
            if(prisonController.capturedAgents.Length > 0) {
                foreach (GameObject agent in prisonController.capturedAgents) {
                    float distanceToPrison = Vector3.Distance(transform.position, agent.transform.position);
                    if (distanceToPrison < 1.42f) {
                        AddReward(10.0f);
                        agent.GetComponent<CriminerAgent>().isCaptured = false;
                        prisonController.ReleaseCapturedAgents();
                    }
                }
            }
            */
        }
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        //rotate
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        //right
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }


    /**
    シーンの初期化
    */
    public override void OnEpisodeBegin() {
        
        
    }

}
