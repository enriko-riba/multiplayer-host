# .NET 5 based multiplayer game server

![.NET Core](https://github.com/enriko-riba/multiplayer-host/workflows/.NET%20Core/badge.svg)

## Overview
The multiplayer game server is a class library helping game developers implementing multiplayer game servers. The game developers still have to deal with game specific logic, client connections and state persistence but the game server provides structure and plumbing code to glue all components together.

### The multiplayer game server implements
* abstract User class and user management
* main game loop
* message dispatcher loop
* client message double buffering (accepts new incoming messages simultaneously with turn processing)
* server message buffering
* dedicated background threads for the main loop and message dispatcher
* public IServer api supporting basic server operations, queuing client messages and creating response messages

### The multiplayer game server does not implement
* a real game - although a minimalistic reference game (server and client) is provided for test purposes
* any kind of game specific logic
* any kind of client connectivity
* any kind of data storage

## Components
The server cannot run as a stand-alone application, instead it is designed to be used with external components providing:
* client connection management and message sending/receiving by implementing a **`IConnectionManager`** component
* game persistence by implementing a **`IRepository`** component
* game logic by implementing a **`ITurnProcessor`** component
* in addition an **`IServer`** component is provided for the game implementors to support server operations

## Turn processing
The server uses the `ITurnProcessor` interface to invoke game specific logic. The server invokes turn processors in the following order:
1. `ITurnProcessor.ProcessClientMessage(User user, in ClientMessage message);` invoked for every received message 
2. `ITurnProcessor.ProcessUserTurn(User user, int ellapsedMilliseconds);` invoked for every existing user (both online and offline)
3. `ITurnProcessor.OnTurnComplete();`

## Quickstart
1. create a game specific user entity inheriting from `MultiplayerHost.Domain.User`
```cs
public class Player : User
{
    public int Diamond { get; internal set; }
    public int Gold { get; internal set; }
    public int Reputation { get; internal set; }
    ...
}
```
2. implement the three mandatory interfaces: `IConnectionManager`, `IRepository` and `ITurnProcessor`. 
Note: for testing purposes a single class could implement all of them. When dealing with users inside your game logic, make sure to cast the provided User instance into your specific user entity class.
The server will only use the IRepository interface to save the user state. If you need to persist additional state you must implement it as part of your game logic.
3. Implement client messages and corresponding parsers/handlers, define server messages and specify their payload format
4. obtain an IServer reference, grab the context and provide the interface implementations
```cs
IServer server;
var context = server.Context;
context.Configure(repository, connMngr, turnProcessor);
// optionally subscribe for initial game setup: context.Server.OnBeforeServerStart += OnServerStart;
```
5. start the server
```cs
await server.Start();
```
6. The server starts invoking the turn processing logic
7. You also need a client capable of exchanging messages with your IConnectionManager implementation

**Note**: The multiplayer game server is easiest to be used with a DI container. Consult the reference game server project how to setup DI in a console application.