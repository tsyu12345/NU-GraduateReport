using UnityEngine;

public class DorokSettings : MonoBehaviour {
    public bool randomizePlayersTeamForTraining = true;
    public float agentRunSpeed;
    public float agentForwardSpeed;
    public float agentLateralSpeed;
    public float rewardConstant;
    public Vector3 prisonPos;
}

public enum Team {
    Police = 0,
    Criminer = 1
}
