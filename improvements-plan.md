# Multiplayer Host Improvements Plan

## Scope
This plan captures practical improvements identified while upgrading the library to .NET 10. The focus is on preserving the project's generic multiplayer-host responsibilities while improving runtime safety, API clarity, observability, and maintainability.

## Current Observations
- The library has a clean, small surface area and a clear separation between host responsibilities and game-specific implementations.
- The server runtime currently relies on shared mutable state across background loops (`Server`, `RequestBuffer`, `ResponseBuffer`) without explicit concurrency guards.
- Startup and shutdown are asynchronous in behavior but only partially modeled as such in the public API.
- Nullability and initialization contracts are not fully enforced in several public models and context properties.
- No test project was found in the workspace, so core lifecycle and buffering behavior is currently not protected by automated regression tests.

## Implementation Status
- Completed: lifecycle/shutdown hardening, core thread-safety fixes, API/nullability contract cleanup, timing/persistence cleanup, observability/diagnostics improvements, automated regression test project, CI pull-request quality gates, and README/runtime documentation refresh.
- In progress: none.
- Not started: none.

## Improvement Proposals

### 1. Harden server lifecycle and shutdown behavior
**Why**
- `Server.Stop()` blocks on `Task.Wait()`, which can deadlock or wrap exceptions.
- The main loop and dispatcher loop are controlled only by `IsRunning`, with no cancellation token or coordinated shutdown.
- Event subscriptions are added on start and never removed on stop.

**Relevant files**
- `host/Domain/Server.cs`
- `host/Domain/Server_MainLoop.cs`
- `host/Domain/Server_Dispatcher.cs`
- `host/Abstract/IServer.cs`

**Trackable tasks**
- [x] Add coordinated cancellation for `MainLoop` and `DispatcherLoop` using `CancellationTokenSource`.
- [x] Introduce an async shutdown path (`StopAsync`) and keep `Stop()` as a compatibility wrapper if needed.
- [x] Replace blocking waits with awaited task completion and preserve original exceptions.
- [x] Unsubscribe `PlayerConnecting` and `PlayerDisconnected` handlers during shutdown.
- [x] Prevent duplicate starts and invalid stop calls with dedicated exception types or clearer error messages.
- [x] Ensure dispatcher shutdown drains queued outbound messages or explicitly documents drop behavior.

**Acceptance criteria**
- Server startup and shutdown can complete without blocking on `.Wait()`.
- Both background loops exit deterministically when shutdown begins.
- Repeated start/stop misuse yields clear, intentional behavior.

### 2. Make shared state access thread-safe
**Why**
- `users` is a regular `Dictionary<int, User>` accessed from multiple execution paths.
- `RequestBuffer` swaps queue roles without synchronization while other threads may enqueue incoming messages.
- `IsRunning` and `tickCounter` are read/written across multiple tasks without explicit concurrency guarantees.

**Relevant files**
- `host/Domain/Server.cs`
- `host/Domain/Server_MainLoop.cs`
- `host/Domain/RequestBuffer.cs`
- `host/Domain/ResponseBuffer.cs`

**Trackable tasks**
- [x] Replace `Dictionary<int, User>` with a concurrency-safe design, or protect all access through a lock strategy.
- [x] Redesign `RequestBuffer` so producer writes and consumer buffer swapping are synchronized safely.
- [x] Review `tickCounter` type consistency (`uint` vs `ulong`) and use atomic access where needed.
- [x] Ensure the running-state flag is safe for cross-thread visibility.
- [x] Add targeted concurrency tests for message enqueueing, buffer swapping, and user add/remove behavior.

**Acceptance criteria**
- Concurrent connection, message enqueue, and turn-processing operations do not risk data races.
- Buffer behavior remains correct under sustained parallel message production.

### 3. Strengthen public API contracts and nullability
**Why**
- `ServerContext` exposes non-nullable properties (`Repository`, `ConnectionManager`, `TurnProcessor`) that are assigned later.
- `ClientMessage.Data`, `ServerMessage.Data`, and `ServerMessage.Targets` are non-nullable but do not define defaults or constructor enforcement.
- Some public events and XML docs do not reflect nullable intent consistently.

**Relevant files**
- `host/Domain/ServerContext.cs`
- `host/Messages/ClientMessage.cs`
- `host/Messages/ServerMessage.cs`
- `host/Abstract/IServer.cs`
- `host/Abstract/IConnectionManager.cs`

**Trackable tasks**
- [x] Refactor `ServerContext` initialization so required collaborators are enforced before use.
- [x] Use `required` members, constructors, or safe defaults for message models to eliminate invalid partially initialized instances.
- [x] Align event declarations and delegate signatures with nullable reference type intent.
- [x] Review and tighten thrown exception types for invalid configuration and invalid lifecycle states.
- [x] Update XML documentation to match the actual runtime contracts and expectations.

**Acceptance criteria**
- Public API surfaces no obvious nullability ambiguities.
- Consumers receive earlier and clearer feedback for invalid configuration or malformed messages.

### 4. Improve timing, persistence, and runtime correctness semantics
**Why**
- `CreateServerMessage` timestamps use `DateTime.Now.Ticks`, while other code uses UTC.
- `ShouldSaveUser` comments mention one minute, but implementation uses 5 seconds.
- The turn loop uses elapsed timing that should stay internally consistent and easy to reason about.

**Relevant files**
- `host/Domain/Server.cs`
- `host/Domain/Server_MainLoop.cs`
- `host/Domain/User.cs`
- `host/Abstract/ITurnProcessor.cs`

**Trackable tasks**
- [x] Standardize all server-side timestamps on UTC or another documented monotonic strategy.
- [x] Align user-save throttling comments, naming, and implementation with the intended behavior.
- [x] Extract save throttling into a configurable policy or constant owned by the host.
- [x] Review tick and elapsed-time calculations for overflow, drift, and documentation clarity.
- [x] Add tests covering save throttling and turn-timing behavior.

**Acceptance criteria**
- Time-related behavior is internally consistent and documented.
- Persistence cadence is explicit, configurable, and validated by tests.

### 5. Increase observability and diagnostics
**Why**
- Logging is present but mostly message-based and lacks structured lifecycle, throughput, and error context.
- There is no built-in lightweight metric surface for queue depth, active users, or turn duration.

**Relevant files**
- `host/Domain/Server.cs`
- `host/Domain/Server_MainLoop.cs`
- `host/Domain/Server_Dispatcher.cs`

**Trackable tasks**
- [x] Add structured log scopes or richer event IDs for startup, shutdown, dispatch, and persistence actions.
- [x] Emit periodic diagnostics for active user count, processed client messages, queued responses, and turn duration.
- [x] Consider exposing optional meter/counter instrumentation using modern .NET diagnostics primitives.
- [x] Review error logs to include enough identifiers for production troubleshooting without leaking game-specific payload details.

**Acceptance criteria**
- Operators can identify lifecycle transitions and runtime hotspots from logs or metrics.
- Diagnostic output remains generic and host-focused rather than game-specific.

### 6. Add automated tests and CI quality gates
**Why**
- No test project was found in the current workspace.
- Core behavior such as lifecycle transitions, buffering, user persistence cadence, and message dispatch ordering should be regression-tested.

**Relevant files**
- New test project under the solution root
- `host/Domain/*`
- `host/Abstract/*`

**Trackable tasks**
- [x] Add a dedicated test project targeting .NET 10.
- [x] Add unit tests for startup validation, duplicate start prevention, and shutdown behavior.
- [x] Add tests for `RequestBuffer` and `ResponseBuffer` semantics.
- [x] Add tests for user loading, connection acceptance/rejection, and persistence triggers.
- [x] Update CI to build and run tests on pull requests.

**Acceptance criteria**
- Core host behavior is covered by automated tests.
- CI fails when runtime behavior regresses.

### 7. Refresh package and developer documentation
**Why**
- The README is clear at a high level, but it does not document lifecycle expectations, threading assumptions, or operational guarantees.
- The package now targets .NET 10 and should document that explicitly.

**Relevant files**
- `README.md`
- `host/Host.csproj`

**Trackable tasks**
- [x] Update the README quickstart to describe required configuration order and lifecycle expectations.
- [x] Document threading and callback expectations for `ITurnProcessor`, `IConnectionManager`, and repository implementations.
- [x] Document message contract expectations, including payload ownership and timestamp semantics.
- [x] Add a short section describing shutdown behavior and persistence timing.
- [x] Keep package metadata, badges, and target framework references aligned with supported versions.

**Acceptance criteria**
- Consumers can integrate the library without reading the internal implementation.
- Public documentation matches the actual runtime behavior.

## Suggested Delivery Order
1. Lifecycle and shutdown hardening
2. Shared-state thread-safety fixes
3. API/nullability contract cleanup
4. Timing and persistence correctness
5. Automated tests
6. Observability improvements
7. Documentation refresh

## Notes
- The recommendations above intentionally avoid game-specific features and keep the host focused on reusable server concerns.
- Any public API changes should be reviewed for compatibility impact before release as a new package version.
