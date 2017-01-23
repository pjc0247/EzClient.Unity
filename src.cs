using System;
using System.Threading;
using System.Collections.Generic;

using WebSocketSharp;
using WebSocketSharp.Net;

using UnityEngine;

using GSF.Packet;
using GSF.Ez.Packet;

using Newtonsoft.Json;

public static class EzSerializer
{
    public static string ToEzObject(this object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }
    public static T ToGameObject<T>(this object json)
    {
        if (!(json is string))
            throw new ArgumentException("not an ezobject");
            
        return JsonConvert.DeserializeObject<T>((string)json);
    }
}

public class EzClient : MonoBehaviour
{
    #region DELEGATE
    public delegate void WorldInfoCallback(WorldInfo packet);
    public delegate void JoinPlayerCallback(JoinPlayer packet);
    public delegate void LeavePlayerCallback(LeavePlayer packet);
    public delegate void CustomPacketCallback(BroadcastPacket packet);
    public delegate void ModifyPlayerPropertyCallback(ModifyPlayerProperty packet);
    public delegate void ModifyWorldPropertyCallback(ModifyWorldProperty packet);
    public delegate void ModifyOptionalWorldPropertyCallback(ModifyOptionalWorldProperty packet);

    public delegate void ChangeRootPlayerCallback();

    public delegate void OptionalWorldPropertyCallback(OptionalWorldProperty packet);
    #endregion

    public bool isAlive
    {
        get
        {
            return ws.IsAlive;
        }
    }

    public WorldInfoCallback onWorldInfo;
    public JoinPlayerCallback onJoinPlayer;
    public LeavePlayerCallback onLeavePlayer;
    public CustomPacketCallback onCustomPacket;
    public ModifyPlayerPropertyCallback onModifyPlayerProperty;
    public ModifyWorldPropertyCallback onModifyWorldProperty;
    public ModifyOptionalWorldPropertyCallback onModifyOptionalWorldProperty;
    public ChangeRootPlayerCallback onDesignatedRootPlayer;

    public EzPlayer player;
    public List<EzPlayer> players;
    public Dictionary<string, object> worldProperty;
    public Dictionary<string, object> optionalWorldProperty;
    public string rootPlayerId;

    private List<PacketBase> packetQ;

    private string host;
    private WebSocket ws;

    private int nextPacketId = 0;
    private Dictionary<long, Action<PacketBase>> responseCallbacks;
    private bool isRootPlayer = false;

    // jwvg0425
    public static EzClient Instance;

    /// <summary>
    /// </summary>
    /// <param name="host">서버 주소</param>
    /// <param name="playerId">유저 식별값 (주로 닉네임)</param>
    /// <param name="property">로그인과 함께 서버에 보낼 개인 데이터</param>
    /// <returns></returns>
    public static EzClient Connect(string host, string playerId, Dictionary<string, object> property)
    {
        if (host.EndsWith("/") == false)
            host = host + "/";
        host += "ez?version=1.0.0&userType=guest&userId=1";

        var gobj = new GameObject("EzClientObj");
        var ezclient = gobj.AddComponent<EzClient>();

        PacketSerializer.Protocol = new GSF.Packet.Json.JsonProtocol();

        ezclient.host = host;
        ezclient.worldProperty = new Dictionary<string, object>();
        ezclient.optionalWorldProperty = new Dictionary<string, object>();
        ezclient.player = new EzPlayer()
        {
            PlayerId = playerId,
            Property = property
        };
        ezclient.players = new List<EzPlayer>() { ezclient.player };
        ezclient.responseCallbacks = new Dictionary<long, Action<PacketBase>>();
        ezclient.packetQ = new List<PacketBase>();

        // jwvg0425
        Instance = ezclient;

        return ezclient;
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        ws = new WebSocket(host);
        ws.OnOpen += Ws_OnOpen;
        ws.OnError += Ws_OnError;
        ws.OnClose += Ws_OnClose;
        ws.OnMessage += Ws_OnMessage;

        ws.Connect();
    }
    void Update()
    {
        List<PacketBase> qCopy = null;

        lock (packetQ)
        {
            qCopy = new List<PacketBase>(packetQ);
            packetQ.Clear();
        }

        foreach (var packet in qCopy)
            ProcessPacket(packet);
    }
    void OnDisable()
    {
        Disconnect();
    }

    private void Ws_OnOpen(object sender, EventArgs e)
    {
        Debug.Log("OpWebSocketOpen");

        Send(new JoinPlayer()
        {
            Player = player
        });
    }

    private void Ws_OnError(object sender, ErrorEventArgs e)
    {
        Debug.LogError("OnWebSocketError : " + e.Message);
    }

    private void Ws_OnClose(object sender, CloseEventArgs e)
    {
        Debug.LogWarning("OnWebSocketClose : " + e.Reason);
    }

    private void Ws_OnMessage(object sender, MessageEventArgs e)
    {
        Debug.Log("OnWebSocketMessage : " + e.Data);

        var packet = PacketSerializer.Deserialize(e.RawData);

        lock (packetQ)
            packetQ.Add(packet);
    }

    // fixme
    private void AddTask(Action action)
    {
        //lock (tasks)
        //    tasks.Add(action);
        action.Invoke();
    }

    #region PROCESS_PACKET
    private void ProcessPacket(PacketBase packet)
    {
        if (packet is WorldInfo)
            ProcessWorldInfo((WorldInfo)packet);
        else if (packet is ModifyPlayerProperty)
            ProcessModifyPlayerProperty((ModifyPlayerProperty)packet);
        else if (packet is ModifyWorldProperty)
            ProcessModifyWorldProperty((ModifyWorldProperty)packet);
        else if (packet is ModifyOptionalWorldProperty)
            ProcessModifyOptionalWorldProperty((ModifyOptionalWorldProperty)packet);
        else if (packet is JoinPlayer)
            ProcessJoinPlayer((JoinPlayer)packet);
        else if (packet is LeavePlayer)
            ProcessLeavePlayer((LeavePlayer)packet);
        else if (packet is BroadcastPacket)
            ProcessBroadcastPacket((BroadcastPacket)packet);

        else if (packet is OptionalWorldProperty)
            ProcessOptionalWorldProperty((OptionalWorldProperty)packet);
    }
    private void ProcessWorldInfo(WorldInfo packet)
    {
        rootPlayerId = packet.RootPlayerId;

        players = new List<EzPlayer>(packet.OtherPlayers);
        players.Add(player);
        player = packet.Player;
        worldProperty = packet.Property;

        if (onWorldInfo != null)
            AddTask(() => onWorldInfo.Invoke(packet));
        if (onDesignatedRootPlayer != null &&
            isRootPlayer == false && player.PlayerId == rootPlayerId)
        {
            onDesignatedRootPlayer();
            isRootPlayer = true;
        }
    }
    private void ProcessModifyPlayerProperty(ModifyPlayerProperty packet)
    {
        EzPlayer player = null;
        lock (players)
            player = players.Find(x => x.PlayerId == packet.Player.PlayerId);

        foreach (var pair in packet.Property)
            player.Property[pair.Key] = pair.Value;
        if (packet.RemovedKeys != null)
        {
            foreach (var key in packet.RemovedKeys)
                player.Property.Remove(key);
        }

        if (onModifyPlayerProperty != null)
            AddTask(() => onModifyPlayerProperty.Invoke(packet));
    }
    private void ProcessModifyWorldProperty(ModifyWorldProperty packet)
    {
        foreach (var pair in packet.Property)
            worldProperty[pair.Key] = pair.Value;
        if (packet.RemovedKeys != null)
        {
            foreach (var key in packet.RemovedKeys)
                worldProperty.Remove(key);
        }

        if (onModifyWorldProperty != null)
            AddTask(() => onModifyWorldProperty.Invoke(packet));
    }
    private void ProcessModifyOptionalWorldProperty(ModifyOptionalWorldProperty packet)
    {
        foreach (var pair in packet.Property)
            optionalWorldProperty[pair.Key] = pair.Value;
        if (packet.RemovedKeys != null)
        {
            foreach (var key in packet.RemovedKeys)
                optionalWorldProperty.Remove(key);
        }

        if (onModifyOptionalWorldProperty != null)
            AddTask(() => onModifyOptionalWorldProperty.Invoke(packet));
    }
    private void ProcessJoinPlayer(JoinPlayer packet)
    {
        lock (players)
            players.Add(packet.Player);

        if (onJoinPlayer != null)
            AddTask(() => onJoinPlayer.Invoke(packet));
    }
    private void ProcessLeavePlayer(LeavePlayer packet)
    {
        rootPlayerId = packet.RootPlayerId;

        lock (players)
            players.Remove(packet.Player);

        if (onLeavePlayer != null)
            AddTask(() => onLeavePlayer.Invoke(packet));
        if (onDesignatedRootPlayer != null &&
            isRootPlayer == false && player.PlayerId == rootPlayerId)
        {
            onDesignatedRootPlayer();
            isRootPlayer = true;
        }
    }
    private void ProcessBroadcastPacket(BroadcastPacket packet)
    {
        if (onCustomPacket != null)
            AddTask(() => onCustomPacket.Invoke(packet));
    }
    private void ProcessOptionalWorldProperty(OptionalWorldProperty packet)
    {
        lock (responseCallbacks)
        {
            if (responseCallbacks.ContainsKey(packet.PacketId) == false)
            {
                Debug.LogWarning("UnknownPacket : " + packet.PacketId);
                return;
            }

            responseCallbacks[packet.PacketId].Invoke(packet);
            responseCallbacks.Remove(packet.PacketId);
        }
    }
    #endregion

    private void Send(PacketBase packet)
    {
        Debug.Log("Send : " + packet);

        var json = PacketSerializer.Serialize(packet);
        ws.Send(json);
    }

    #region PUBLIC_API
    public void SendPacket(int packetType, Dictionary<string, object> data)
    {
        Send(new RequestBroadcast()
        {
            Type = packetType,
            Data = data
        });
    }
    public void SendPacket(string tag, int packetType, Dictionary<string, object> data)
    {
        Send(new RequestBroadcast()
        {
            Tag = tag,
            Type = packetType,
            Data = data
        });
    }
    public void SetPlayerProperty(Dictionary<string, object> property)
    {
        foreach (var pair in property)
            player.Property[pair.Key] = pair.Value;

        Send(new ModifyPlayerProperty()
        {
            Property = property
        });
    }
    public void SetPlayerProperty(string key, object value)
    {
        SetPlayerProperty(new Dictionary<string, object>()
        {
            {key, value}
        });
    }
    public void RemovePlayerProperty(string[] keys)
    {
        foreach (var key in keys)
            player.Property.Remove(key);

        Send(new ModifyPlayerProperty()
        {
            Property = new Dictionary<string, object>(),
            RemovedKeys = keys
        });
    }
    public void RemovePlayerProperty(string key)
    {
        RemovePlayerProperty(new string[] { key });
    }

    public void SetWorldProperty(Dictionary<string, object> property)
    {
        Send(new ModifyWorldProperty()
        {
            Property = property
        });
    }
    public void SetWorldProperty(string key, object value)
    {
        SetWorldProperty(new Dictionary<string, object>() {
            {key, value}
        });
    }
    public void RemoveWorldProperty(string[] keys)
    {
        Send(new ModifyWorldProperty()
        {
            Property = new Dictionary<string, object>(),
            RemovedKeys = keys
        });
    }
    public void RemoveWorldProperty(string key)
    {
        RemoveWorldProperty(new string[] { key });
    }

    public void SetOptionalWorldProperty(Dictionary<string, object> property)
    {
        Send(new ModifyOptionalWorldProperty()
        {
            Property = property
        });
    }
    public void SetOptionalWorldProperty(string key, object value)
    {
        SetOptionalWorldProperty(new Dictionary<string, object>()
        {
            {key, value}
        });
    }
    public void RequestOptionalWorldProperty(string[] keys, OptionalWorldPropertyCallback callback)
    {
        int packetId = Interlocked.Increment(ref nextPacketId);

        lock (responseCallbacks)
            responseCallbacks[packetId] = (p) => callback.Invoke((OptionalWorldProperty)p);

        Send(new RequestOptionalWorldProperty()
        {
            PacketId = packetId,
            Keys = keys
        });
    }
    public void RequestOptionalWorldProperty(string key, OptionalWorldPropertyCallback callback)
    {
        RequestOptionalWorldProperty(new string[] { key }, callback);
    }

    /// <summary>
    /// 연결을 끊는다.
    /// </summary>
    public void Disconnect()
    {
        if (ws.IsAlive == false)
            return;

        ws.Close();
        Destroy(gameObject);
    }
    #endregion
}
