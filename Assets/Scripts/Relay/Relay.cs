using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class Relay : MonoBehaviour
{
    private async void Start()
    {
        // initilization is exactly the same as for the lobby

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        { Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId); };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }


    public async void CreateRelay()
    {
        // any relay function can throw an exception, and break our game
        try
        {
            // max number of players, without counting the host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            // so here, it is 4 players, host + 3 clients
            // we can add as an argument the region we wish

            // we now just have to get our join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            // we can now send this code to our friends, so they connect to the same relay'

            Debug.Log($"Join code is: {joinCode}.");

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            //JoinRelay(joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void JoinRelay(string joinCode)
    {
        Debug.Log($"Trying to join relay with code: {joinCode}.");

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            Debug.Log($"Joined relay with code: {joinCode}.");

            // if we want to start the client immediatly:
            NetworkManager.Singleton.StartClient();
            Debug.Log("Client started.");
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
