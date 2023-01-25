using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonController : MonoBehaviour {

    //Prisonの位置
    public Vector3 prisonPos;
    //とらわれている逃走役エージェントたち
    public List<GameObject> capturedAgents = new List<GameObject>();

    // Start is called before the first frame update
    void Start() {
        //Prisonの位置を取得
        prisonPos = transform.position;
    }

    // Update is called once per frame
    void Update() {
        //とらわれている逃走役エージェントたちを更新
        capturedAgents = GetCapturedAgents();
    }

    /**
    * 捕らわれている逃走役エージェントを取得
    */
    private List<GameObject> GetCapturedAgents() {
        List<GameObject> capturedAgents = new List<GameObject>();
        foreach (GameObject agent in GameObject.FindGameObjectsWithTag("Criminer")) {
            if (agent.GetComponent<DorokAgent>().isCaptured) {
                capturedAgents.Add(agent);
            }
        }
        return capturedAgents;
    }

    /**
    * 捕らわれている逃走役エージェントを牢屋からランダムにフィールドに開放する
    */
    public void ReleaseCapturedAgents() {
        foreach (GameObject agent in capturedAgents) {
            agent.GetComponent<DorokAgent>().isCaptured = false;
            agent.transform.position = new Vector3(Random.Range(-5.0f, 5.0f), 0.5f, Random.Range(-5.0f, 5.0f));
        }
    }
}
