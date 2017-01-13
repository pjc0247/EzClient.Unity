using UnityEngine;

using System.Linq;
using System.Collections.Generic;

using GSF.Ez.Packet;

public class TestScript : MonoBehaviour {
    class ClientData
    {
        public EzClient ezClient;
        public string log;

        #region UI_DATA
        public string chatMessage = "";

        public string playerPropertyKey = "KEY";
        public string playerPropertyValue = "VALUE";

        public string worldPropertyKey = "KEY";
        public string worldPropertyValue = "Value";

        public string optionalWorldPropertyKey = "KEY";
        public string optionalWorldPropertyValue = "Value";

        public string requestOptionalWorldPropertyKey = "KEY";
        #endregion
    }

    class Foo
    {
        public string Name;
        public long Level;
    }

    private List<ClientData> clients;

	void Start () {
        Debug.Log("START");

        clients = new List<ClientData>();

        var f = new Foo()
        {
            Name = "asdf"
        };

        Debug.Log(f.ToEzObject());
        Debug.Log(f.ToEzObject().ToGameObject<Foo>().Name);
	}

    string nickname = "jwvg";
    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Nickname");
        GUILayout.Space(10);
        nickname = GUILayout.TextField(nickname, GUILayout.Width(100));
        GUILayout.Space(10);
        if (GUILayout.Button("CreateNewClient"))
        {
            var client = new ClientData()
            {
                ezClient = EzClient.Connect("ws://localhost:9916",
                    nickname,
                    new Dictionary<string, object>() {
                    }),
                log = ""
            };

            client.ezClient.onWorldInfo += (WorldInfo packet) =>
            {
                client.log += "OnWorldInfo : " + Newtonsoft.Json.JsonConvert.SerializeObject(packet.Player.Property) + "\r\n";
            };
            client.ezClient.onJoinPlayer += (JoinPlayer packet) =>
            {
                client.log += "OnJoin : " + packet.Player.PlayerId + "\r\n";
            };
            client.ezClient.onLeavePlayer += (LeavePlayer packet) =>
            {
                client.log += "OnLeave : " + packet.Player.PlayerId + "\r\n";
            };
            client.ezClient.onModifyPlayerProperty += (ModifyPlayerProperty packet) =>
            {
                client.log += "OnModifyPlayerPropeperty : " + packet.Player.PlayerId + " / " + Newtonsoft.Json.JsonConvert.SerializeObject(packet.Property) + "\r\n";
            };
            client.ezClient.onCustomPacket += (BroadcastPacket packet) =>
            {
                // 커스텀 패킷을 받으면, Type에 따라 분류 처리해야 함
                if (packet.Type == PacketType.Chat)
                    client.log += "[" + packet.Sender.PlayerId + "] " + packet.Data["message"] + "\r\n";
                else if (packet.Type == PacketType.Move)
                {
                    client.log += "OnMove\r\n";
                }
            };

            clients.Add(client);

            nickname = "rini" + (new System.Random()).Next(9000) + 1000;
        }
        GUILayout.EndHorizontal();

        var offset = 0;
        foreach (var clientData in clients)
        {
            var client = clientData.ezClient;
            var rect = new Rect(offset * 310 + 10, 30, 300, Screen.height - 100);

            GUI.Box(rect, "CLIENT " + offset.ToString());
            GUILayout.BeginArea(rect);

            GUILayout.Space(30);

            if (GUILayout.Button("Close"))
            {
                client.Disconnect();
                clients.Remove(clientData);
            }

            GUILayout.Label("MyNickname : " + client.player.PlayerId);
            GUILayout.Label("Players : " + string.Join(", ", client.players.Select(x => (string)x.PlayerId).ToArray()));
            GUILayout.Label("WorldProperty : " + Newtonsoft.Json.JsonConvert.SerializeObject(client.worldProperty));
            GUILayout.Label("OptProperty : " + Newtonsoft.Json.JsonConvert.SerializeObject(client.optionalWorldProperty));

            GUILayout.BeginHorizontal();
            clientData.chatMessage = GUILayout.TextField(clientData.chatMessage);
            if (GUILayout.Button("SendChat", GUILayout.Width(150)))
            {
                client.SendPacket(PacketType.Chat, new Dictionary<string, object>()
                {
                    {"message", clientData.chatMessage}
                });
                clientData.chatMessage = "";
            }
            GUILayout.EndHorizontal();

            //
            GUILayout.BeginHorizontal();
            clientData.playerPropertyKey = GUILayout.TextField(clientData.playerPropertyKey);
            clientData.playerPropertyValue = GUILayout.TextField(clientData.playerPropertyValue);
            if (GUILayout.Button("SetPlayerProp", GUILayout.Width(150)))
            {
                client.SetPlayerProperty(clientData.playerPropertyKey, clientData.playerPropertyValue);
            }
            GUILayout.EndHorizontal();

            //
            GUILayout.BeginHorizontal();
            clientData.worldPropertyKey = GUILayout.TextField(clientData.worldPropertyKey);
            clientData.worldPropertyValue = GUILayout.TextField(clientData.worldPropertyValue);
            if (GUILayout.Button("SetWorldProp", GUILayout.Width(150)))
            {
                client.SetWorldProperty(clientData.worldPropertyKey, clientData.worldPropertyValue);
            }
            GUILayout.EndHorizontal();

            //
            GUILayout.BeginHorizontal();
            clientData.optionalWorldPropertyKey = GUILayout.TextField(clientData.optionalWorldPropertyKey);
            clientData.optionalWorldPropertyValue = GUILayout.TextField(clientData.optionalWorldPropertyValue);
            if (GUILayout.Button("SetOptProp", GUILayout.Width(150)))
            {
                client.SetOptionalWorldProperty(clientData.optionalWorldPropertyKey, clientData.optionalWorldPropertyValue);
            }
            GUILayout.EndHorizontal();

            // 
            GUILayout.BeginHorizontal();
            clientData.requestOptionalWorldPropertyKey = GUILayout.TextField(clientData.requestOptionalWorldPropertyKey);
            if (GUILayout.Button("RequestOptProp", GUILayout.Width(150)))
            {
                var _clientData = clientData;
                client.RequestOptionalWorldProperty(
                    new string[] { clientData.requestOptionalWorldPropertyKey },
                    (OptionalWorldProperty packet) =>
                    {
                        
                        _clientData.log += "OptionalWorldProperty : " + Newtonsoft.Json.JsonConvert.SerializeObject(packet.Property) + "\r\n";
                    });
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.TextArea(clientData.log, GUILayout.Height(400));

            GUILayout.EndArea();
            offset++;
        }
    }
}
