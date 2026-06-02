using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Gameville
{
    public class MultiplayerManager : MonoBehaviour
    {
        public bool isHost = true;
        public string connectAddress = "127.0.0.1";
        public int port = 7777;
        public PlayerController localPlayer;
        public GameObject playerPrefab;
        public Transform playerRoot;
        public GameObject blockPrefab;
        public Transform worldRoot;

        private TcpListener listener;
        private TcpClient serverClient;
        private StreamWriter serverWriter;
        private Thread serverReceiveThread;
        private Thread acceptThread;
        private volatile bool running;

        private readonly ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();
        private readonly Dictionary<int, ConnectionInfo> connections = new Dictionary<int, ConnectionInfo>();
        private readonly Dictionary<int, PlayerController> remotePlayers = new Dictionary<int, PlayerController>();
        private int nextClientId = 1;

        private void Start()
        {
            if (playerRoot == null)
            {
                playerRoot = transform;
            }

            if (localPlayer == null)
            {
                Debug.LogError("MultiplayerManager needs a localPlayer reference.");
                return;
            }

            localPlayer.SetMultiplayerManager(this);
            localPlayer.SetLocal(true);
            localPlayer.networkId = isHost ? 0 : -1;
            localPlayer.ApplySkinIndex(localPlayer.skinIndex);

            running = true;

            if (isHost)
            {
                StartHost();
            }
            else
            {
                ConnectToHost();
            }
        }

        private void Update()
        {
            ProcessIncomingMessages();
        }

        private void OnDestroy()
        {
            StopNetwork();
        }

        public void RequestSendPlayerState(PlayerController player)
        {
            if (player == null)
                return;

            Vector3 position = player.transform.position;
            float yaw = player.transform.eulerAngles.y;

            if (isHost)
            {
                string payload = $"PLAYER_STATE:{player.networkId},{position.x:F3},{position.y:F3},{position.z:F3},{yaw:F1},{player.skinIndex}";
                BroadcastToClients(payload, player.networkId);
            }
            else
            {
                string payload = $"PLAYER_STATE:{position.x:F3},{position.y:F3},{position.z:F3},{yaw:F1},{player.skinIndex}";
                SendToServer(payload);
            }
        }

        public void RequestSpawnBlock(Vector3 position)
        {
            string payload = $"BLOCK:{position.x:F3},{position.y:F3},{position.z:F3}";
            if (isHost)
            {
                SpawnBlock(position);
                BroadcastToClients(payload);
            }
            else
            {
                SendToServer(payload);
            }
        }

        public void SpawnBlock(Vector3 position)
        {
            if (blockPrefab == null || worldRoot == null)
                return;

            Instantiate(blockPrefab, position, Quaternion.identity, worldRoot);
        }

        private void StartHost()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                acceptThread = new Thread(AcceptClientsLoop) { IsBackground = true };
                acceptThread.Start();
                Debug.Log($"Multiplayer host started on port {port}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start host: {ex.Message}");
            }
        }

        private void ConnectToHost()
        {
            try
            {
                serverClient = new TcpClient();
                serverClient.Connect(connectAddress, port);
                NetworkStream stream = serverClient.GetStream();
                serverWriter = new StreamWriter(stream) { AutoFlush = true };
                serverReceiveThread = new Thread(() => ClientReceiveLoop(serverClient, null)) { IsBackground = true };
                serverReceiveThread.Start();
                SendToServer($"HELLO:{Application.productName}");
                Debug.Log($"Connected to host at {connectAddress}:{port}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to host: {ex.Message}");
            }
        }

        private void AcceptClientsLoop()
        {
            while (running)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    int connectionId = nextClientId++;
                    var stream = client.GetStream();
                    var connection = new ConnectionInfo
                    {
                        Id = connectionId,
                        Client = client,
                        Writer = new StreamWriter(stream) { AutoFlush = true },
                        Reader = new StreamReader(stream)
                    };

                    lock (connections)
                    {
                        connections[connectionId] = connection;
                    }

                    Thread clientThread = new Thread(() => ClientReceiveLoop(client, connection)) { IsBackground = true };
                    clientThread.Start();

                    Debug.Log($"Client connected: {connectionId}");
                }
                catch (Exception ex)
                {
                    if (running)
                        Debug.LogError($"Host accept error: {ex.Message}");
                }
            }
        }

        private void ClientReceiveLoop(TcpClient tcp, ConnectionInfo connectionInfo)
        {
            StreamReader reader = connectionInfo?.Reader ?? new StreamReader(tcp.GetStream());
            try
            {
                while (running)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        break;

                    int id = connectionInfo != null ? connectionInfo.Id : -1;
                    incomingMessages.Enqueue($"{id}|{line}");
                }
            }
            catch (Exception ex)
            {
                if (running)
                    Debug.LogError($"Network receive error: {ex.Message}");
            }
            finally
            {
                if (connectionInfo != null)
                {
                    incomingMessages.Enqueue($"{connectionInfo.Id}|DISCONNECT:");
                }
            }
        }

        private void ProcessIncomingMessages()
        {
            while (incomingMessages.TryDequeue(out string message))
            {
                string[] parts = message.Split(new[] { '|' }, 2);
                if (parts.Length != 2)
                    continue;

                if (!int.TryParse(parts[0], out int sourceId))
                    continue;

                HandleIncomingMessage(sourceId, parts[1]);
            }
        }

        private void HandleIncomingMessage(int sourceId, string message)
        {
            if (message.StartsWith("HELLO:"))
            {
                if (!isHost)
                    return;

                int clientId = sourceId;
                SendToClient(clientId, $"WELCOME:{clientId}");
                SendToClient(clientId, BuildSpawnMessage(0, localPlayer.transform.position, localPlayer.transform.eulerAngles.y, localPlayer.skinIndex));

                lock (connections)
                {
                    foreach (var pair in remotePlayers)
                    {
                        SendToClient(clientId, BuildSpawnMessage(pair.Key, pair.Value.transform.position, pair.Value.transform.eulerAngles.y, pair.Value.skinIndex));
                    }
                }

                BroadcastToClients(BuildSpawnMessage(clientId, localPlayer.transform.position, localPlayer.transform.eulerAngles.y, localPlayer.skinIndex), clientId);
                Debug.Log($"Registered new client {clientId}.");
            }
            else if (message.StartsWith("WELCOME:"))
            {
                if (isHost)
                    return;

                string[] parts = message.Substring(8).Split(',');
                if (parts.Length == 1 && int.TryParse(parts[0], out int id))
                {
                    localPlayer.networkId = id;
                    Debug.Log($"Client assigned network id {id}.");
                }
            }
            else if (message.StartsWith("SPAWN_PLAYER:"))
            {
                string[] parts = message.Substring(13).Split(',');
                if (parts.Length >= 6 && int.TryParse(parts[0], out int id) && float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y) && float.TryParse(parts[3], out float z) && float.TryParse(parts[4], out float yaw) && int.TryParse(parts[5], out int skinIndex))
                {
                    if (id == localPlayer.networkId)
                        return;

                    if (!remotePlayers.ContainsKey(id))
                    {
                        SpawnRemotePlayer(id, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
                    }
                }
            }
            else if (message.StartsWith("PLAYER_STATE:"))
            {
                string payload = message.Substring(13);
                if (isHost)
                {
                    string[] parts = payload.Split(',');
                    if (parts.Length == 5 && float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) && float.TryParse(parts[2], out float z) && float.TryParse(parts[3], out float yaw) && int.TryParse(parts[4], out int skinIndex))
                    {
                        string serverPayload = $"PLAYER_STATE:{sourceId},{x:F3},{y:F3},{z:F3},{yaw:F1},{skinIndex}";
                        BroadcastToClients(serverPayload, sourceId);
                        UpdateRemotePlayerStateFromHost(sourceId, serverPayload.Substring(13));
                    }
                }
                else
                {
                    UpdateRemotePlayerStateFromClientMessage(payload);
                }
            }
            else if (message.StartsWith("BLOCK:"))
            {
                string payload = message.Substring(6);
                if (TryParseVector3(payload, out Vector3 position))
                {
                    if (isHost)
                    {
                        BroadcastToClients(message, sourceId);
                        SpawnBlock(position);
                    }
                    else
                    {
                        SpawnBlock(position);
                    }
                }
            }
            else if (message.StartsWith("DISCONNECT:"))
            {
                RemoveRemotePlayer(sourceId);
            }
        }

        private string BuildSpawnMessage(int id, Vector3 position, float yaw, int skinIndex)
        {
            return $"SPAWN_PLAYER:{id},{position.x:F3},{position.y:F3},{position.z:F3},{yaw:F1},{skinIndex}";
        }

        private void UpdateRemotePlayerStateFromHost(int sourceId, string payload)
        {
            string[] parts = payload.Split(',');
            if (parts.Length < 6)
                return;

            if (!int.TryParse(parts[0], out int id))
                return;
            if (!float.TryParse(parts[1], out float x))
                return;
            if (!float.TryParse(parts[2], out float y))
                return;
            if (!float.TryParse(parts[3], out float z))
                return;
            if (!float.TryParse(parts[4], out float yaw))
                return;
            if (!int.TryParse(parts[5], out int skinIndex))
                skinIndex = 0;

            if (id == localPlayer.networkId)
                return;

            if (remotePlayers.TryGetValue(id, out PlayerController remote))
            {
                remote.ApplyNetworkState(new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
            else
            {
                SpawnRemotePlayer(id, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
        }

        private void UpdateRemotePlayerStateFromClientMessage(string payload)
        {
            string[] parts = payload.Split(',');
            if (parts.Length < 6)
                return;

            if (!int.TryParse(parts[0], out int id))
                return;
            if (!float.TryParse(parts[1], out float x))
                return;
            if (!float.TryParse(parts[2], out float y))
                return;
            if (!float.TryParse(parts[3], out float z))
                return;
            if (!float.TryParse(parts[4], out float yaw))
                return;
            if (!int.TryParse(parts[5], out int skinIndex))
                skinIndex = 0;

            if (id == localPlayer.networkId)
                return;

            if (remotePlayers.TryGetValue(id, out PlayerController remote))
            {
                remote.ApplyNetworkState(new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
            else
            {
                SpawnRemotePlayer(id, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
        }

        private void SpawnRemotePlayer(int id, Vector3 position, Quaternion rotation, int skinIndex)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("MultiplayerManager needs a playerPrefab reference to spawn remote players.");
                return;
            }

            GameObject clone = Instantiate(playerPrefab, position, rotation, playerRoot);
            clone.name = $"RemotePlayer_{id}";

            PlayerController controller = clone.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError("Player prefab does not contain a PlayerController component.");
                Destroy(clone);
                return;
            }

            controller.SetMultiplayerManager(this);
            controller.SetLocal(false);
            controller.networkId = id;
            controller.ApplySkinIndex(skinIndex);

            remotePlayers[id] = controller;
        }

        private void RemoveRemotePlayer(int id)
        {
            if (remotePlayers.TryGetValue(id, out PlayerController existing))
            {
                Destroy(existing.gameObject);
                remotePlayers.Remove(id);
            }

            lock (connections)
            {
                if (connections.TryGetValue(id, out ConnectionInfo connection))
                {
                    connection.Dispose();
                    connections.Remove(id);
                }
            }
        }

        private void BroadcastToClients(string message, int excludeId = -1)
        {
            lock (connections)
            {
                foreach (var pair in connections)
                {
                    if (pair.Key == excludeId)
                        continue;

                    SendToClient(pair.Key, message);
                }
            }
        }

        private void SendToClient(int clientId, string message)
        {
            lock (connections)
            {
                if (connections.TryGetValue(clientId, out ConnectionInfo connection))
                {
                    try
                    {
                        connection.Writer.WriteLine(message);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to send to client {clientId}: {ex.Message}");
                    }
                }
            }
        }

        private void SendToServer(string message)
        {
            if (serverWriter == null)
                return;

            try
            {
                serverWriter.WriteLine(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send to server: {ex.Message}");
            }
        }

        private bool TryParseVector3(string payload, out Vector3 result)
        {
            result = Vector3.zero;
            string[] parts = payload.Split(',');
            if (parts.Length != 3)
                return false;

            if (!float.TryParse(parts[0], out float x))
                return false;
            if (!float.TryParse(parts[1], out float y))
                return false;
            if (!float.TryParse(parts[2], out float z))
                return false;

            result = new Vector3(x, y, z);
            return true;
        }

        private void StopNetwork()
        {
            running = false;

            if (serverClient != null)
            {
                serverClient.Close();
                serverClient = null;
            }

            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            lock (connections)
            {
                foreach (var connection in connections.Values)
                {
                    connection.Dispose();
                }
                connections.Clear();
            }
        }

        private class ConnectionInfo : IDisposable
        {
            public int Id;
            public TcpClient Client;
            public StreamReader Reader;
            public StreamWriter Writer;

            public void Dispose()
            {
                Writer?.Close();
                Reader?.Close();
                Client?.Close();
            }
        }
    }
}

                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                acceptThread = new Thread(AcceptClientsLoop) { IsBackground = true };
                acceptThread.Start();
                Debug.Log($"Multiplayer host started on port {port}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start host: {ex.Message}");
            }
        }

        private void ConnectToHost()
        {
            try
            {
                serverClient = new TcpClient();
                serverClient.Connect(connectAddress, port);
                NetworkStream stream = serverClient.GetStream();
                serverWriter = new StreamWriter(stream) { AutoFlush = true };
                serverReceiveThread = new Thread(() => ClientReceiveLoop(serverClient, null)) { IsBackground = true };
                serverReceiveThread.Start();
                SendToServer($"HELLO:{Application.productName}");
                Debug.Log($"Connected to host at {connectAddress}:{port}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to host: {ex.Message}");
            }
        }

        private void AcceptClientsLoop()
        {
            while (running)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    int connectionId = nextClientId++;
                    var stream = client.GetStream();
                    var connection = new ConnectionInfo
                    {
                        Id = connectionId,
                        Client = client,
                        Writer = new StreamWriter(stream) { AutoFlush = true },
                        Reader = new StreamReader(stream)
                    };

                    lock (connections)
                    {
                        connections[connectionId] = connection;
                    }

                    Thread clientThread = new Thread(() => ClientReceiveLoop(client, connection)) { IsBackground = true };
                    clientThread.Start();

                    Debug.Log($"Client connected: {connectionId}");
                }
                catch (Exception ex)
                {
                    if (running)
                        Debug.LogError($"Host accept error: {ex.Message}");
                }
            }
        }

        private void ClientReceiveLoop(TcpClient tcp, ConnectionInfo connectionInfo)
        {
            StreamReader reader = connectionInfo?.Reader ?? new StreamReader(tcp.GetStream());
            try
            {
                while (running)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        break;

                    int id = connectionInfo != null ? connectionInfo.Id : -1;
                    incomingMessages.Enqueue($"{id}|{line}");
                }
            }
            catch (Exception ex)
            {
                if (running)
                    Debug.LogError($"Network receive error: {ex.Message}");
            }
            finally
            {
                if (connectionInfo != null)
                {
                    incomingMessages.Enqueue($"{connectionInfo.Id}|DISCONNECT:");
                }
            }
        }

        private void ProcessIncomingMessages()
        {
            while (incomingMessages.TryDequeue(out string message))
            {
                string[] parts = message.Split(new[] { '|' }, 2);
                if (parts.Length != 2)
                    continue;

                if (!int.TryParse(parts[0], out int sourceId))
                    continue;

                HandleIncomingMessage(sourceId, parts[1]);
            }
        }

        private void HandleIncomingMessage(int sourceId, string message)
        {
            if (message.StartsWith("HELLO:"))
            {
                if (!isHost)
                    return;

                int clientId = sourceId;
                SendToClient(clientId, $"WELCOME:{clientId}");
                SendToClient(clientId, BuildSpawnMessage(0, localPlayer.transform.position, localPlayer.transform.eulerAngles.y, localPlayer.skinIndex));

                lock (connections)
                {
                    foreach (var pair in remotePlayers)
                    {
                        SendToClient(clientId, BuildSpawnMessage(pair.Key, pair.Value.transform.position, pair.Value.transform.eulerAngles.y, pair.Value.skinIndex));
                    }
                }

                BroadcastToClients(BuildSpawnMessage(clientId, localPlayer.transform.position, localPlayer.transform.eulerAngles.y, localPlayer.skinIndex), clientId);
                Debug.Log($"Registered new client {clientId}.");
            }
            else if (message.StartsWith("WELCOME:"))
            {
                if (isHost)
                    return;

                string[] parts = message.Substring(8).Split(',');
                if (parts.Length == 1 && int.TryParse(parts[0], out int id))
                {
                    localPlayer.networkId = id;
                    Debug.Log($"Client assigned network id {id}.");
                }
            }
            else if (message.StartsWith("SPAWN_PLAYER:"))
            {
                string[] parts = message.Substring(13).Split(',');
                if (parts.Length >= 6 && int.TryParse(parts[0], out int id) && float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y) && float.TryParse(parts[3], out float z) && float.TryParse(parts[4], out float yaw) && int.TryParse(parts[5], out int skinIndex))
                {
                    if (id == localPlayer.networkId)
                        return;

                    if (!remotePlayers.ContainsKey(id))
                    {
                        SpawnRemotePlayer(id, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
                    }
                }
            }
            else if (message.StartsWith("PLAYER_STATE:"))
            {
                string payload = message.Substring(13);
                if (isHost)
                {
                    string[] parts = payload.Split(',');
                    if (parts.Length == 5 && float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) && float.TryParse(parts[2], out float z) && float.TryParse(parts[3], out float yaw) && int.TryParse(parts[4], out int skinIndex))
                    {
                        string serverPayload = $"PLAYER_STATE:{sourceId},{x:F3},{y:F3},{z:F3},{yaw:F1},{skinIndex}";
                        BroadcastToClients(serverPayload, sourceId);
                        UpdateRemotePlayerStateFromHost(sourceId, serverPayload.Substring(13));
                    }
                }
                else
                {
                    UpdateRemotePlayerStateFromClientMessage(payload);
                }
            }
            else if (message.StartsWith("BLOCK:"))
            {
                string payload = message.Substring(6);
                if (TryParseVector3(payload, out Vector3 position))
                {
                    if (isHost)
                    {
                        BroadcastToClients(message, sourceId);
                        SpawnBlock(position);
                    }
                    else
                    {
                        SpawnBlock(position);
                    }
                }
            }
            else if (message.StartsWith("DISCONNECT:"))
            {
                RemoveRemotePlayer(sourceId);
            }
        }

        private string BuildSpawnMessage(int id, Vector3 position, float yaw, int skinIndex)
        {
            return $"SPAWN_PLAYER:{id},{position.x:F3},{position.y:F3},{position.z:F3},{yaw:F1},{skinIndex}";
        }

        private void UpdateRemotePlayerStateFromHost(int sourceId, string payload)
        {
            string[] parts = payload.Split(',');
            if (parts.Length < 6)
                return;

            if (!int.TryParse(parts[0], out int id))
                return;
            if (!float.TryParse(parts[1], out float x))
                return;
            if (!float.TryParse(parts[2], out float y))
                return;
            if (!float.TryParse(parts[3], out float z))
                return;
            if (!float.TryParse(parts[4], out float yaw))
                return;
            if (!int.TryParse(parts[5], out int skinIndex))
                skinIndex = 0;

            if (id == localPlayer.networkId)
                return;

            if (remotePlayers.TryGetValue(id, out PlayerController remote))
            {
                remote.ApplyNetworkState(new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
            else
            {
                SpawnRemotePlayer(id, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
        }

        private void UpdateRemotePlayerStateFromClientMessage(string payload)
        {
            string[] parts = payload.Split(',');
            if (parts.Length < 6)
                return;

            if (!int.TryParse(parts[0], out int id))
                return;
            if (!float.TryParse(parts[1], out float x))
                return;
            if (!float.TryParse(parts[2], out float y))
                return;
            if (!float.TryParse(parts[3], out float z))
                return;
            if (!float.TryParse(parts[4], out float yaw))
                return;
            if (!int.TryParse(parts[5], out int skinIndex))
                skinIndex = 0;

            if (id == localPlayer.networkId)
                return;

            if (remotePlayers.TryGetValue(id, out PlayerController remote))
            {
                remote.ApplyNetworkState(new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
            else
            {
                SpawnRemotePlayer(id, new Vector3(x, y, z), Quaternion.Euler(0, yaw, 0), skinIndex);
            }
        }

        private void SpawnRemotePlayer(int id, Vector3 position, Quaternion rotation, int skinIndex)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("MultiplayerManager needs a playerPrefab reference to spawn remote players.");
                return;
            }

            GameObject clone = Instantiate(playerPrefab, position, rotation, playerRoot);
            clone.name = $"RemotePlayer_{id}";

            PlayerController controller = clone.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError("Player prefab does not contain a PlayerController component.");
                Destroy(clone);
                return;
            }

            controller.SetMultiplayerManager(this);
            controller.SetLocal(false);
            controller.networkId = id;
            controller.ApplySkinIndex(skinIndex);

            remotePlayers[id] = controller;
        }

        private void RemoveRemotePlayer(int id)
        {
            if (remotePlayers.TryGetValue(id, out PlayerController existing))
            {
                Destroy(existing.gameObject);
                remotePlayers.Remove(id);
            }

            lock (connections)
            {
                if (connections.TryGetValue(id, out ConnectionInfo connection))
                {
                    connection.Dispose();
                    connections.Remove(id);
                }
            }
        }

        private void BroadcastToClients(string message, int excludeId = -1)
        {
            lock (connections)
            {
                foreach (var pair in connections)
                {
                    if (pair.Key == excludeId)
                        continue;

                    SendToClient(pair.Key, message);
                }
            }
        }

        private void SendToClient(int clientId, string message)
        {
            lock (connections)
            {
                if (connections.TryGetValue(clientId, out ConnectionInfo connection))
                {
                    try
                    {
                        connection.Writer.WriteLine(message);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to send to client {clientId}: {ex.Message}");
                    }
                }
            }
        }

        private void SendToServer(string message)
        {
            if (serverWriter == null)
                return;

            try
            {
                serverWriter.WriteLine(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send to server: {ex.Message}");
            }
        }

        private bool TryParseVector3(string payload, out Vector3 result)
        {
            result = Vector3.zero;
            string[] parts = payload.Split(',');
            if (parts.Length != 3)
                return false;

            if (!float.TryParse(parts[0], out float x))
                return false;
            if (!float.TryParse(parts[1], out float y))
                return false;
            if (!float.TryParse(parts[2], out float z))
                return false;

            result = new Vector3(x, y, z);
            return true;
        }

        private void StopNetwork()
        {
            running = false;

            if (serverClient != null)
            {
                serverClient.Close();
                serverClient = null;
            }

            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            lock (connections)
            {
                foreach (var connection in connections.Values)
                {
                    connection.Dispose();
                }
                connections.Clear();
            }
        }

        private class ConnectionInfo : IDisposable
        {
            public int Id;
            public TcpClient Client;
            public StreamReader Reader;
            public StreamWriter Writer;

            public void Dispose()
            {
                Writer?.Close();
                Reader?.Close();
                Client?.Close();
            }
        }
    }
}
