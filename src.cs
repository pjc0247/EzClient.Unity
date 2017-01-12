using System;
using System.Collections.Generic;

using WebSocketSharp;
using WebSocketSharp.Net;

using UnityEngine;

using GSF.Packet;
using GSF.Ez.Packet;

public class EzClient : MonoBehaviour {
    #region DELEGATE
    public delegate void JoinPlayerCallback(JoinPlayer packet);
    public delegate void LeavePlayerCallback(LeavePlayer packet);
    public delegate void CustomPacketCallback(BroadcastPacket packet);
    #endregion

    public JoinPlayerCallback onJoinPlayer;
    public LeavePlayerCallback onLeavePlayer;
    public CustomPacketCallback onCustomPacket;

    public EzPlayer player;
    public List<EzPlayer> players;
    public Dictionary<string, object> worldProperty;

    private string host;
    private WebSocket ws;

    public static EzClient Connect(string host, int userId, Dictionary<string, object> property)
    {
        var gobj = new GameObject("EzClientObj");
        var ezclient = gobj.AddComponent<EzClient>();

        PacketSerializer.Protocol = new GSF.Packet.Json.JsonProtocol();

        ezclient.host = host;
        ezclient.worldProperty = new Dictionary<string, object>();
        ezclient.players = new List<EzPlayer>();
        ezclient.player = new EzPlayer()
        {
            UserId = userId,
            Property = property
        };

        return ezclient;
    }

	void Start () {
        DontDestroyOnLoad(gameObject);

        ws = new WebSocket(host);
        ws.OnOpen += Ws_OnOpen;
        ws.OnError += Ws_OnError;
        ws.OnClose += Ws_OnClose;
        ws.OnMessage += Ws_OnMessage;

        ws.Connect();
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

        if (packet is WorldInfo)
            ProcessWorldInfo((WorldInfo)packet);
        else if (packet is ModifyWorldProperty)
            ProcessModifyWorldProperty((ModifyWorldProperty)packet);
        else if (packet is JoinPlayer)
            ProcessJoinPlayer((JoinPlayer)packet);
        else if (packet is LeavePlayer)
            ProcessLeavePlayer((LeavePlayer)packet);
        else if (packet is BroadcastPacket)
            ProcessBroadcastPacket((BroadcastPacket)packet);
    }

    private void ProcessWorldInfo(WorldInfo packet)
    {
        players = new List<EzPlayer>(packet.Players);
        worldProperty = packet.Property;
    }
    private void ProcessModifyWorldProperty(ModifyWorldProperty packet)
    {
        for (var pair in packet.Property)
            worldProperty[pair.Key] = pair.Value;   
    }
    private void ProcessJoinPlayer(JoinPlayer packet)
    {
        players.Add(packet.Player);

        if (onJoinPlayer != null)
            onJoinPlayer.Invoke(packet);
    }
    private void ProcessLeavePlayer(LeavePlayer packet)
    {
        players.Remove(packet.Player);

        if (onLeavePlayer != null)
            onLeavePlayer.Invoke(packet);
    }
    private void ProcessBroadcastPacket(BroadcastPacket packet)
    {
        if (onCustomPacket != null)
            onCustomPacket.Invoke(packet);
    }

    private void Send(PacketBase packet)
    {
        Debug.Log("Send : " + packet);

        var json = PacketSerializer.Serialize(packet);
        ws.Send(json);
    }

    public void SendPacket(int packetType, Dictionary<string, object> data)
    {
        Send(new RequestBroadcast()
        {
            Type = packetType,
            Data = data
        });
    }
    public void SetPlayerProperty(Dictionary<string, object> property)
    {
        Send(new ModifyPlayerProperty()
        {
            Property = property
        });
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
        })
    }
    /// <summary>
    /// 연결을 끊는다.
    /// </summary>
    public void Disconnect()
    {
        ws.Close();
    }
}