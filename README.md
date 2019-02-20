EzClient.Unity
====

__GSF.Ez__ implementation for Unity.<br>
[![works badge](https://cdn.rawgit.com/nikku/works-on-my-machine/v0.2.0/badge.svg)](https://github.com/nikku/works-on-my-machine)

![](test.png)





Connect to Server
----
```cs
// 클라이언트 식별값
//    닉네임을 사용해도 무관
var uniqId = "pjc0247";

client = EzClient.Connect(
    "ws://localhost:9916",
    uniqId,
    new Dictionary<string, object>() {
        {"class", "oven-breaker"}
    }),
```

Players
----
__GetAllPlayers__
```cs
foreach(var player in client.players) {
    var playerId = player.PlayerId;
    var level = (int)player.Property["level"];
}
```

__Join/Leave Events__
```cs
client.onJoinPlayer += (JoinPlayer packet) => {
    var player = packet.Player;

    var playerId = player.PlayerId;
    var level = (int)player.Property[level];
};

client.onLeavePlayer += (LeavePlayer packet) => {
    var player = packet.Player;
    
    var playerId = player.PlayerId;
    var level = (int)player.Property[level];
};
```

Communication
----
__Declaring Packet__
```cs
public class PacketType {
    public static readonly int Chat = 1;
}
```

__Broadcasting__
```cs
client.SendPacket(
    PacketType.Chat,
    new Dictionary<string, object>()
    {
        {"message", clientData.chatMessage}
    });
```

__Subscribing__
```cs
client.onCustomPacket += (BroadcastPacket packet) =>
{            
    if (packet.Type == PacketType.Chat)
        log += "[" + packet.Sender.PlayerId + "] " + packet.Data["message"] + "\r\n";
};
```


Player Property
----
```cs
client.SetPlayerProperty(new Dictionary<string, object>() {
    {"level", 10}
});
```
* 지정한 KEY는 VALUE로 덮어쓰기 됩니다.
* 지정하지 않은 KEY는 변동 사항이 없습니다.


World Property
----
월드 데이터는 서버 인스턴스 전체에 공유되는 데이터입니다.<br>

__GetWorldProperty__
```cs
var mapId = (int)client.worldProperty["map_id"];
```

__SetWorldProperty__
```cs
client.SetWorldProperty(new Dictionary<string, object>() {
    {"map_id", 11}
});
```
* 지정한 KEY는 VALUE로 덮어쓰기 됩니다.
* 지정하지 않은 KEY는 변동 사항이 없습니다.


RPC
----

