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