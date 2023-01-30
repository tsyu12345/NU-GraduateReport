using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonController : MonoBehaviour {

    //Prisonの位置
    public Vector3 prisonPos;
    //とらわれている逃走役エージェントたち
    public List<GameObject> capturedAgents = new List<GameObject>();

    //EnvController
    private EnvController m_EnvController;
    // Start is called before the first frame update
    void Start() {
        //Prisonの位置を取得
        prisonPos = transform.position;
        //EnvControllerを取得
        m_EnvController = FindObjectOfType<EnvController>();
    }

    // Update is called once per frame
    void Update() {
        //とらわれている逃走役エージェントたちを更新
        capturedAgents = GetCapturedAgents();
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

    /**
    * 捕らわれている逃走役エージェントを牢屋からランダムにフィールドに開放する
    */
    public void ReleaseCapturedAgents(Team team) {
        foreach (GameObject agent in capturedAgents) {
            //agent.GetComponent<DorokAgent>().isCaptured = false;
            agent.transform.position = new Vector3(Random.Range(-5.0f, 5.0f), 0.5f, Random.Range(-5.0f, 5.0f));
        }
    }
}
