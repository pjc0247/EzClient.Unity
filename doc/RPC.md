RPC
====


Register functions
----
__using lambda expr__
```cs
client.RegisterFunction("Sum", (EzPlayer sender, int a, int b) => {
  return a + b;
});
```
__using class method__
```cs
int Sum(int a, int b) {
  return a + b;
}

client.RegisterFunction<int, int, int>("Sum", Sum);
```

__unregister__
```cs
client.UnregisterFunction("Sum");
```


Remote Call
----
```cs
EzPlayer targetPlayer; 

client.RemoteCall(targetPlayer, "Sum", 5, 5,
  (response) => {
    Console.WriteLine( response.Result );
  });
```

__exception handling__
```cs
client.RemoteCall(targetPlayer, "Sum", 5, 5,
  (response) => {
    if (response.isSuccess) {
      Console.WriteLine( response.Result );
    }
    else {
      Console.WriteLine( result.Exception );
    }
  });
```
