using UnityEngine;

public class LobbyButtons : MonoBehaviour
{
    [SerializeField] private TestLobby testLobby;

    public async void CreateLobbyButton() => await testLobby.CreateLobby();
    public void ListLobbiesButton() => testLobby.ListLobbies();
    public void JoinLobbyButton(string lobbyCode) => testLobby.JoinLobbyByCode(lobbyCode);
    public void QuickJoinLobbyButton() => testLobby.QuickJoinLobby();
    public void PrintPlayersButton() => testLobby.PrintPlayers();
    public void UpdateLobbyGameModeButton() => testLobby.UpdateLobbyGameMode("Death Match");
    public void UpdatePlayerNameButton()
    {
        string newName = "Changed Name";
        testLobby.UpdatePlayerName(newName + Random.Range(0, 9999));
    }
    public void LeaveLobbyButton() => testLobby.LeaveLobby();
    public void KickPlayerButton() => testLobby.KickPlayer();
    public void MigrateLobbyHostButton() => testLobby.MigrateLobbyHost();
    public void DeleteLobbyButton() => testLobby.DeleteLobby();
}
