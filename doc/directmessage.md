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

__Receiver__
```cs
client.SubscribeTag("MY_NICKNAME");
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
