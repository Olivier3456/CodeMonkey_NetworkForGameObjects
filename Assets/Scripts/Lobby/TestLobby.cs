using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class TestLobby : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartBeatTimer = 0f;
    private string playerName;

    private float lobbyUpdateTimer;

    private const string KEY_GAME_MODE = "Game Mode";
    private const string KEY_START_GAME = "Start Game";


    public event EventHandler OnGameReadyToStart;



    private bool IsLobbyHost()
    {
        if (joinedLobby == null)
        {
            Debug.Log("IsLobbyHost - Lobby is null!");
            return false;
        }

        return joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }


    [SerializeField] private Relay relay;


    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        playerName = "Le Joueur Pro " + UnityEngine.Random.Range(0, 9999);
        Debug.Log($"Player Name: {playerName}.");
    }


    public async Task CreateLobby()
    {
        // Attention, les opérations liées aux lobbies peuvent créer des erreurs.
        // Pour que ça ne casse pas tout le programme, on les fait toujours en try-catch.
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {                    
                    // La visibilité doit être publique pour pouvoir être vue de l'extérieur du lobby.
                    {KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, "Capture The Flag")},

                    // Pour pouvoir prévenir les joueurs quand la partie commence.
                    {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")}
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log($"Created Lobby {lobbyName} with game mode: {lobby.Data["Game Mode"].Value}. Max players: {maxPlayers}. Available slots left: {hostLobby.AvailableSlots}. Lobby Id is: {lobby.Id}. Lobby Code is: {lobby.LobbyCode}.");
            PrintPlayers();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        // Member = le joueur sera vu par les membres du lobby
                        // playerName est une string qu'on a ajoutée au début de notre classe
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
                    }
        };
    }


    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby;

            Debug.Log($"Joined Lobby with code {lobbyCode}.");
            PrintPlayers();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions quickJoinLobbyOptions = new QuickJoinLobbyOptions
            {
                Player = GetPlayer()
            };

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinLobbyOptions);
            // (on peut ajouter à cette fonction des QuickJoinLobbyOptions)
            joinedLobby = lobby;

            Debug.Log("Lobby Quick Joined.");
            PrintPlayers();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public void PrintPlayers()
    {
        if (joinedLobby == null)
        {
            Debug.Log("Can't print players, lobby is null!");
            return;
        }

        Debug.Log($"Players in Lobby {joinedLobby.Name} with game mode: {joinedLobby.Data["Game Mode"].Value}.");
        foreach (Player player in joinedLobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }


    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLobbyPollForUpdates();
    }


    private async void HandleLobbyHeartBeat()
    {
        if (hostLobby == null)
        {
            return;
        }

        heartBeatTimer -= Time.deltaTime;

        if (heartBeatTimer < 0f)
        {
            float heartBeatTimerMax = 15f;
            heartBeatTimer = heartBeatTimerMax;

            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    }


    private async void HandleLobbyPollForUpdates()
    {
        if (joinedLobby == null)
        {
            return;
        }

        lobbyUpdateTimer -= Time.deltaTime;

        if (lobbyUpdateTimer < 0f)
        {
            // on ne peut pas le faire plus d'une fois par seconde
            float lobbyUpdateTimerMax = 1.1f;
            lobbyUpdateTimer = lobbyUpdateTimerMax;

            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            joinedLobby = lobby;


            if (!IsPlayerInLobby())
            {
                // Player was kicked out of this lobby
                Debug.Log("Kicked from Lobby!");

                //OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                joinedLobby = null;
                return;
            }


            // handle ready to start game condition
            if (IsLobbyHost())
            {
                if (joinedLobby.Players.Count == 2)
                {
                    Debug.Log("Game Ready to start!");
                    OnGameReadyToStart?.Invoke(this, EventArgs.Empty);
                }
            }


            // Handle game start
            if (joinedLobby.Data[KEY_START_GAME].Value != "0")
            {
                Debug.Log("Game Started! Joining relay, if player not the Host...");

                if (!IsLobbyHost()) // Because lobby host already joined relay: see CreateRelay() in Relay class
                {
                    Debug.Log("Player is not the Host. Joining relay...");
                    relay.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                }
                else
                {
                    Debug.Log("Player is the Host, relay is already joigned.");
                }

                joinedLobby = null;
            }
        }
    }


    private bool IsPlayerInLobby()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }


    public async void StartGame()
    {
        if (!IsLobbyHost())
        {
            Debug.Log("Can't start game, player is not the host!");
            return;
        }

        try
        {
            string relayCode = await relay.CreateRelay();

            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                    {
                        // players wait this key to start game
                        {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                    }
            });

            joinedLobby = lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }





    // OPTIONAL FUNCTIONS
    public async void ListLobbies()
    {
        try
        {
            // Voir les classes QueryLobbiesOptions et QueryFilter pour avoir plus de détails
            // sur les options possibles.
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                // nombre de résultats à retourner
                Count = 25,

                Filters = new List<QueryFilter>
                {
                    // GT = greater than, donc notre filtre = plus que 0 places disponibles
                    // (la valeur (ici 0) doit être au format string)
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots,
                                    "0",
                                    QueryFilter.OpOptions.GT),
                },

                Order = new List<QueryOrder>
                {
                    // false pour ascending, Created pour date de création
                    // donc ici, on classe par ordre descendant de création
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
                Debug.Log($"Lobby name: {lobby.Name} with game mode: {lobby.Data["Game Mode"].Value}.");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void UpdateLobbyGameMode(string gameMode)
    {
        if (!IsLobbyHost())
        {
            Debug.Log("Can't update lobby, this player is not the host!");
            return;
        }

        try
        {
            UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Member, gameMode)}
                }
            };

            // Lobby est une classe, mais elle n'est pas mise à jour automatiquement, il faut donc
            // mettre à jour notre instance en récupérant le résultat de la fonction d'Update : 
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, updateLobbyOptions);
            joinedLobby = hostLobby;

            PrintPlayers();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void UpdatePlayerName(string newName)
    {
        try
        {
            playerName = newName;
            // On n'est plus obligés de mettre à jour notre lobby manuellement,
            // vu qu'on le fait maintenant toutes les 1.1 secondes. Mais faisons-le
            // quand même, pour que la mise à jour soit immédiate.
            joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(
                joinedLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName",
                        new PlayerDataObject(
                                PlayerDataObject.VisibilityOptions.Member,
                                playerName)}
                    }
                });

            PrintPlayers();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void LeaveLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            hostLobby = null;
            joinedLobby = null;
            Debug.Log("Lobby leaved by player.");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void KickPlayer()
    {
        try
        {
            // ici on vire le 2e joueur dans la liste (donc pas le host)
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id);
            Debug.Log("Player kicked.");
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void MigrateLobbyHost()
    {
        if (!IsLobbyHost())
        {
            Debug.Log("Can't migrate Lobby Host, player is not the  host!");
        }

        try
        {
            UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions
            {
                // faisons du second joueur le Host
                HostId = joinedLobby.Players[1].Id
            };
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, updateLobbyOptions);
            joinedLobby = hostLobby;
            hostLobby = null;

            PrintPlayers();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    public async void DeleteLobby()
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}
