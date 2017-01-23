DirectMessage(귓속말) 구현하기
====

패킷의 `tag`는 채널과 같은 기능입니다.<br>
이를 이용하여, 개인 또는 파티간에만 주고받는 패킷을 생성할 수 있습니다.<br>
<br>
여기서는 자신의 닉네임을 `tag`로 이용하여 개인 채팅을 구현하는 방법을 보여줍니다.

```cs
class PacketType {
    public static readonly int DirectMessage = 1;
}
```

__Sender__
```cs
client.SendPacket(
    "RECIEVER_PLAYER_ID", // TAG
    PacketType.DirectMessage,
    new Dictionary<string, object>() {
        {"message", "MESSAGE_TO_SEND"}
    });
```

__Receiver__<br>
자기 플레이어 아이디를 구독함으로써, 누군가가 자기 아이디를 태그로 보낸 메세지를 수신합니다.
```cs
client.SubscribeTag("MY_PLAYER_ID");
```
```cs
client.onCustomPacket += (BroadcastPacket packet) =>
{
    if (packet.Type == PacketType.DirectMessage) {
        string message = (string)packet.Data["message"];
        string sender = packet.Sender;

        // print message
        Console.WriteLine(message);
    }
};
```
