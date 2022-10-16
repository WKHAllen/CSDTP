# Data Transfer Protocol for C#

Modern networking interfaces for C#.

## Data Transfer Protocol

The Data Transfer Protocol (DTP) is a larger project to make ergonomic network programming available in any language.
See the full project [here](https://wkhallen.com/dtp/).

## Creating a server

A server can be built using the `Server` implementation:

```csharp
using CSDTP;

// Create a server that receives strings and returns the length of each string
public class MyServer : Server<int, string>
{
    protected override void Receive(ulong clientId, string data)
    {
        // Send back the length of the string
        Send(clientId, data.Length);
    }

    protected override void Connect(ulong clientId)
    {
        Console.WriteLine("Client with ID {0} connected", clientId);
    }

    protected override void Disconnect(ulong clientId)
    {
        Console.WriteLine("Client with ID {0} disconnected", clientId);
    }
}

public class Main
{
    public static void Main()
    {
        // Start the server
        var server = new MyServer();
        server.Start("127.0.0.1", 29275);
    }
}
```

## Creating a client

A client can be built using the `Client` implementation:

```csharp
using CSDTP;

// Create a client that sends a message to the server and receives the length of the message
public class MyClient : Client<string, int>
{
    private readonly string _message;

    public MyClient(string message)
    {
        _message = message;
    }

    protected override void Receive(int data)
    {
        // Validate the response
        Console.WriteLine("Received response from server: {0}", data);
        Assert.AreEqual(data, _message.Length);
    }

    protected override void Disconnected()
    {
        Console.WriteLine("Unexpectedly disconnected from server");
    }
}

public class Main
{
    public static void Main()
    {
        // Connect to the server
        var message = "Hello, server!";
        var client = new MyClient(message);
        client.Connect("127.0.0.1", 29275);

        // Send a message to the server
        client.Send(message);
    }
}
```

## Security

Information security comes included. Every message sent over a network interface is encrypted with AES-256. Key
exchanges are performed using a 4096-bit RSA key-pair.
