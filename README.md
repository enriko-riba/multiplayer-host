# Generic .NET 5.0 based multiplayer game server

![.NET Core](https://github.com/enriko-riba/multiplayer-host/workflows/.NET%20Core/badge.svg)

## Overview

### The multiplayer game server implements
 A class library implementing core server side operations like:
* abstract User class and user management
* main game loop
* message dispatcher loop
* client message double buffering (accepts new incoming messages simultaneously with turn processing)
* server message buffering
* dedicated background threads for the main loop and message dispatcher
* public IServer api supporting basic server operations, enqueuing client messages and creating response messages

### The multiplayer game server does not implement
* a real game - although a minimalistic reference game (server and client) is provided for test purpses
* any kind of game specific logic
* any kind of client connectivity
* any kind of data storage

## Components
The server cannot run as a stand-alone application, instead it is designed to be used with external components providing:
* client connection management and message sending/receiving by implementing a **`IConnectionManager`** component
* game persistance by implementing a **`IRepository`** component
* game logic by implementing a **`ITurnProcessor`** component
* in addition an **`IServer`** component is provided for the game implementors to support server operations

## Turn processing
The server uses the ITurnProcessor interface to invoke game specific logic. The execution order is order:
1. `ITurnProcessor.ProcessClientMessage(User user, in ClientMessage msg);` - for every received message 
2. `ITurnProcessor.ProcessUserTurn(User user, int ellapsedMilliseconds);` - for every existing user (both online and offline)
3. `ITurnProcessor.OnTurnComplete();`

## Quickstart
1. implement the three mandatory interfaces: IConnectionManager, IRepository and ITurnProcessor. For testing a single class could implement all of them.
2. obtain an IServer reference, grab the context and provide the interface implementations
```cs
IServer server;
var context = server.Context;
context.Configure(repository, connMngr, turnProcessor);
// optionally register on start event for initial game setup: context.Server.OnBeforeServerStart += OnServerStart;
```
3. start the server
```cs
await server.Start();
```
**Note**: The multiplayer game server is esiest to be used with a DI container. Consult the reference game server project how to setup DI in a console application.