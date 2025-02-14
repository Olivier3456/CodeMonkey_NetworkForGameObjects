using UnityEngine;

public class RelayButtons : MonoBehaviour
{
    [SerializeField] private Relay relay;
    public void CreateRelayButton() => relay.CreateRelay();

    public void JoinRelayButton(string joinCode) => relay.JoinRelay(joinCode);
}
