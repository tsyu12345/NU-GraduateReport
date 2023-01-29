using System.Collections.Generic;
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
    [SerializeField] Transform prisonArea;

    EnvironmentParameters m_ResetParams;
    EnvController envController;


    /**
    エージェントの初期化
    */
    public override void Initialize() {
        envController = GetComponentInParent<EnvController>();
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
        m_ResetParams = Academy.Instance.EnvironmentParameters;

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


    public override void CollectObservations(VectorSensor sensor) {
    }


    /**
    エージェントの行動による報酬の設定(報酬設計)
    */
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        SetReward(1.0f);
        if(team == Team.Police) {
            onActionPolice(actionBuffers);
        } else {
            onActionCriminer(actionBuffers);
        }
        MoveAgent(actionBuffers.DiscreteActions); 
        EndEpisode();
        
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

        //AI自身が逃走者を検出しているかで報酬を与える場合
        

        GameObject[] enemys = GetEnemies(team);
        GameObject nearestEnemy = GetNearestEnemy(enemys);
        float distance = Vector3.Distance(transform.position, nearestEnemy.transform.position);
        // 逃走役エージェントとの距離が近いほど報酬を与える
        if(distance < 1.42f) {
            AddReward(1.0f);
        } else {
            AddReward(-1.0f);
        }

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
            //AI自身が逃走者を検出しているかで報酬を与える場合
            /*
            if(target != null) {
                print("Detected Police!!");
                AddReward(2.0f);
            } else {
                print("None of Detected");
            }
            */
            GameObject[] enemys = GetEnemies(team);
            GameObject nearestEnemy = GetNearestEnemy(enemys);
            float distance = Vector3.Distance(transform.position, nearestEnemy.transform.position);
            // 警察エージェントとの距離が遠いほど報酬を与える
            if(distance > 1.42f) {
                AddReward(1.0f);
            } else {
                AddReward(-1.0f);
            }
        }
    }

    /**
     * 他のエージェントや壁などゲームオブジェクトとの接触時に呼び出されるハンドラ
     */
    void OnCollisionEnter(Collision c) {
        if(team == Team.Police) {
            onCollisionPolice(c);
        } else {
            onCollisionCriminer(c);
        }
        //envController.onGameEnd(team);
        EndEpisode();
    }

    private void onCollisionPolice(Collision c) {
        // 警察エージェントが逃走役エージェントと接触した場合、捕まえたと判定し、報酬を与える
        if (c.gameObject.CompareTag("Criminer")) {
            print("onCollisionPolice: Catch Criminer");
            AddReward(1.0f);
        }
    }

    private void onCollisionCriminer(Collision c) {
        //捕まっている場合、牢屋の位置から動かないようにする
        if(isCaptured) {
            agentRb.velocity = Vector3.zero;
        }
        // 逃走役エージェントが警察エージェントと接触した場合、捕まえられたと判定し、負の報酬を与える
        if (c.gameObject.CompareTag("Police")) {
            print("onCollisionCriminer: Caught by Police");
            AddReward(-1.0f);
            isCaptured = true;
            //エージェントを所定の牢屋位置に移動させた後、エージェントの動きを止める
            //FIXME:牢屋の位置へ移動しない
            transform.position = prisonArea.transform.position;
            agentRb.velocity = Vector3.zero;
            //自分自身を牢屋に入れる
            envController.onCaught(team, this);
        }
        // 逃走役エージェントが牢屋にいる他の逃走者と接触した場合、報酬を与え、接触した他の逃走者を解放する
        if (c.gameObject.CompareTag("Criminer") && c.gameObject.GetComponent<DorokAgent>().isCaptured) {
            AddReward(1.0f);
            c.gameObject.GetComponent<DorokAgent>().isCaptured = false;
            List<GameObject> capturedAgents = envController.GetCapturedAgents();
            envController.ReleaseCapturedAgents(capturedAgents);
        }
    }


    /**
     * テスト用キーボード操作の受付
     * */
    public override void Heuristic(in ActionBuffers actionsOut) {
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
