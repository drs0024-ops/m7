# Player HSM State Machine Flow

## Overview

The player uses a hierarchical state machine (HSM) to separate high-level movement modes from grounded movement substates.

The runtime flow is:

```text
PlayerStateDriverShell
    Update / FixedUpdate
        PlayerController
            PlayerRoot
                GroundedState
                    IdleState
                    WalkState
                    RunState
                AirborneState
```

`PlayerStateDriverShell` is the Unity `MonoBehaviour` attached to the player prefab. It owns the Unity lifecycle methods and delegates gameplay logic to the pure C# `PlayerController`.

## Main Components

### PlayerStateDriverShell

File: `Assets/Project/Scripts/Characters/Player/StateMachine/PlayerStateDriverShell.cs`

Responsibilities:

- Cache the player's `Rigidbody2D`, `Animator`, and `KnockBack` components.
- Receive dependencies through VContainer.
- Construct the `PlayerController`.
- Call `PlayerController.Tick()` from `Update()`.
- Call `PlayerController.TickFixed()` from `FixedUpdate()`.

```csharp
private void Update()
{
    _controller?.Tick(Time.deltaTime);
}

private void FixedUpdate()
{
    _controller?.TickFixed(Time.fixedDeltaTime);
}
```

### PlayerController

File: `Assets/Project/Scripts/Characters/Player/StateMachine/HSM/PlayerController.cs`

Responsibilities:

- Own the `PlayerContext` state data.
- Mirror Rigidbody data into the context before each physics tick.
- Copy input values from `InputManager` into the context during `Update()`.
- Build and start the HSM.
- Provide shared helpers to the states.

Input snapshot:

```csharp
State.MoveInput = _deps.Input.Movement;
State.JumpPressed = _deps.Input.JumpWasPressed;
State.JumpHeld = _deps.Input.JumpIsHeld;
State.RunHeld = _deps.Input.RunIsHeld;
State.DownWasPressed = _deps.Input.DownWasPressed;
```

Physics synchronization:

```csharp
State.Velocity = _deps.Rb.linearVelocity;
State.RbIsStationary = _deps.Rb.linearVelocity.sqrMagnitude < 0.01f;
State.IsFalling = State.Velocity.y < 0f;
```

## HSM Hierarchy

```mermaid
## Overview

The player uses a hierarchical state machine (HSM) with two high-level states:

```text
PlayerStateDriverShell
    Update / FixedUpdate
        PlayerController
            StateMachine
                PlayerRoot
                    GroundedState
                        IdleState
                        WalkState
                        RunState
                    AirborneState
```

`PlayerStateDriverShell` is the Unity `MonoBehaviour` attached to the player. It owns Unity lifecycle methods and applies the velocity calculated by the pure C# state layer to the root `Rigidbody2D`.
    [*] --> PlayerRoot
    PlayerRoot --> GroundedState: isGrounded
    PlayerRoot --> AirborneState: not grounded

    GroundedState --> IdleState: no horizontal input
    GroundedState --> WalkState: horizontal input
    GroundedState --> RunState: horizontal input + RunHeld
    RunState --> WalkState: Run released
    RunState --> IdleState: input released or stationary

    GroundedState --> AirborneState: JumpPressed or no ground hit
    AirborneState --> GroundedState: ground hit and descending
```

## PlayerRoot

File: `Assets/Project/Scripts/Characters/Player/StateMachine/HSM/CongreateStates/PlayerRoot.cs`

`PlayerRoot` is the top-level state. It owns two high-level states:

- `GroundedState`
- `AirborneState`

The initial child is `GroundedState`.

Root transition rules:
When knockback is active, the root mirrors the Rigidbody velocity into the context:

```csharp
Ctx.Velocity.x = Ctx.Rb.linearVelocity.x;
Ctx.VerticalVelocity = Ctx.Rb.linearVelocity.y;
```

## GroundedState

File: `Assets/Project/Scripts/Characters/Player/StateMachine/HSM/CongreateStates/GroundedState.cs`

`GroundedState` owns the grounded substates:

- `IdleState`
- `WalkState`
- `RunState`

On entry, it:

- Marks the player grounded.
- Resets jump counters and airborne flags.
- Sets vertical context velocity to zero.
- Clears horizontal Rigidbody velocity.
- Fires `PlayerLandedSignal`.

Ground detection uses a BoxCast from the feet collider:

```csharp
Vector2 origin = feet.bounds.center;
Vector2 size = new Vector2(
    feet.bounds.size.x * 0.9f,
    Ctx.MoveStats.GroundDetectionRayLenth);

RaycastHit2D hit = Physics2D.BoxCast(
    origin,
    size,
    0f,
    Vector2.down,
    Ctx.MoveStats.GroundDetectionRayLenth,
    Ctx.MoveStats.GroundLayer);

## PlayerRoot

File: `Assets/Project/Scripts/Characters/Player/StateMachine/HSM/CongreateStates/PlayerRoot.cs`

`PlayerRoot` is the top-level state. It owns two high-level states:

- `GroundedState`
- `AirborneState`

The initial child is `GroundedState`.

Root transition rules:

1. If the player is being knocked back, the root does not transition.
2. If `Ctx.isGrounded` is true, the active state should be `GroundedState`.
3. If the player is not grounded and vertical velocity is at or below `0.1`, the active state should be `AirborneState`.

When knockback is active, the root mirrors the Rigidbody velocity into the context:

```csharp
Ctx.Velocity.x = Ctx.Rb.linearVelocity.x;
Ctx.VerticalVelocity = Ctx.Rb.linearVelocity.y;
```

## GroundedState

File: `Assets/Project/Scripts/Characters/Player/StateMachine/HSM/CongreateStates/GroundedState.cs`

`GroundedState` owns the grounded substates:

- `IdleState`
- `WalkState`
- `RunState`

On entry, it:

- Marks the player grounded.
- Resets jump counters and airborne flags.
- Sets vertical context velocity to zero.
- Clears horizontal Rigidbody velocity.
- Fires `PlayerLandedSignal`.

Ground detection uses a BoxCast from the feet collider:

```csharp
Vector2 origin = feet.bounds.center;
Vector2 size = new Vector2(
    feet.bounds.size.x * 0.9f,
    Ctx.MoveStats.GroundDetectionRayLenth);

RaycastHit2D hit = Physics2D.BoxCast(
    origin,
    size,
    0f,
    Vector2.down,
    Ctx.MoveStats.GroundDetectionRayLenth,
    Ctx.MoveStats.GroundLayer);

Ctx.isGrounded = hit.collider != null;
```

The ground check is performed before the grounded transition decision. This allows the state to transition to `AirborneState` when the feet no longer detect ground.

### IdleState

`IdleState` is the initial grounded substate.

Behavior:

- Disables dust effects.
- Transitions to `WalkState` when horizontal input is greater than `0.01` in magnitude.
- Transitions to `RunState` instead when horizontal input exists and `RunHeld` is true.

### WalkState

Behavior:

- Plays grounded dust while the player is moving.
- Transitions to `RunState` when `RunHeld` is true and horizontal input exists.
- Transitions to `IdleState` when input is released or the Rigidbody is stationary.

### RunState

Behavior:

- Plays grounded dust while the player is moving.
- Transitions to `WalkState` when the run button is released but horizontal input remains.
- Transitions to `IdleState` when input is released or the Rigidbody is stationary.

## Grounded Horizontal Movement

Grounded movement is calculated in `GroundedState.OnFixedUpdate()`.

The input value is read from:

```csharp
Ctx.MoveInput.x
```

The speed is selected from `PlayerMovementStats`:

```csharp
float targetSpeed = Ctx.RunHeld
    ? Ctx.MoveStats.MaxRunSpeed
    : Ctx.MoveStats.MaxWalkSpeed;
```

When there is no horizontal input, velocity decelerates toward zero:

```csharp
float newVelX = Mathf.Lerp(
    currentVelX,
    0f,
    decel * deltaTime);

Ctx.Rb.linearVelocity = new Vector2(
    newVelX,
    Ctx.Rb.linearVelocity.y);
```

When horizontal input exists, velocity approaches the input target speed:

```csharp
float targetVelX = Ctx.MoveInput.x * targetSpeed;
float newVelX = Mathf.Lerp(
    currentVelX,
    targetVelX,
    accel * deltaTime);

Ctx.Rb.linearVelocity = new Vector2(
    newVelX,
    Ctx.Rb.linearVelocity.y);
```

This is the active horizontal movement write for the current HSM player path.

## AirborneState

File: `Assets/Project/Scripts/Characters/Player/StateMachine/HSM/CongreateStates/AirborneState.cs`

`AirborneState` handles:

- Jumping.
- Falling.
- Apex handling.
- Jump release and fast fall.
- Coyote time.
- Jump buffering.
- Ground detection while airborne.

On entry, it clears grounded state and initializes airborne flags. `AttemptJump()` may then set the initial upward velocity.

During `FixedUpdate`, it calculates vertical velocity using the gravity value from `PlayerMovementStats`:

```csharp
Ctx.VerticalVelocity += Ctx.MoveStats.Gravity * deltaTime;
```

The result is clamped to the configured maximum fall speed:

```csharp
float maxFall = -Ctx.MoveStats.MaxFallSpeed;
Ctx.VerticalVelocity = Mathf.Clamp(
    Ctx.VerticalVelocity,
    maxFall,
    50f);
```

The calculated velocity is written to the Rigidbody through `ApplyVelocity()`:

```csharp
Ctx.Velocity = new Vector2(
    Ctx.Velocity.x,
    Ctx.VerticalVelocity);

ApplyVelocity(Ctx.Velocity);
```

`ApplyVelocity()` is defined in `PlayerBaseState`:

```csharp
protected void ApplyVelocity(Vector2 vel)
{
    Ctx.Rb.linearVelocity = vel;
}
```

The airborne state transitions back to `GroundedState` when the ground check succeeds and the player is descending.

## Complete Runtime Tick Order

### Variable-frame update

```text
PlayerStateDriverShell.Update()
    -> PlayerController.Tick(deltaTime)
        -> Read InputManager properties
        -> Copy input into PlayerContext
        -> StateMachine.Tick(deltaTime)
            -> Check transitions
            -> Update active child state
            -> Run state OnUpdate()
```

### Fixed physics update

```text
PlayerStateDriverShell.FixedUpdate()
    -> PlayerController.TickFixed(fixedDeltaTime)
        -> Sync Rigidbody into PlayerContext
        -> StateMachine.TickFixedUpdate(fixedDeltaTime)
            -> Run active state OnFixedUpdate()
                -> GroundedState writes horizontal velocity
                -> AirborneState writes vertical velocity
```

## Important Data Flow

```text
Keyboard / controller
    -> InputManager.Movement
    -> PlayerController.State.MoveInput
    -> GroundedState targetVelX
    -> Rigidbody2D.linearVelocity.x
```

For jumping and falling:

```text
Jump input or lost ground
    -> AirborneState
    -> PlayerMovementStats.Gravity
    -> PlayerContext.VerticalVelocity
    -> PlayerBaseState.ApplyVelocity()
    -> Rigidbody2D.linearVelocity.y
```

## Movement Statistics

File: `Assets/Project/Scripts/Characters/Player/PlayerMovementStats.asset`

The main values used by the HSM are:

- `MaxWalkSpeed`
- `MaxRunSpeed`
- `GroundAcceleration`
- `GroundDeceleration`
- `AirAcceleration`
- `AirDeceleration`
- `JumpHeight`
- `TimeTillJumpApex`
- `GravityOnReleaseMultiplier`
- `MaxFallSpeed`
- `GroundLayer`
- `OneWayPlatform`

`Gravity` and `InitialJumpVelocity` are calculated from the jump height and time-to-apex values when the stats asset is enabled or validated.

## Physics Ownership

The player movement HSM should write to one active root `Rigidbody2D`:

```text
PlayerStateDriverShell
    -> PlayerController
        -> PlayerContext.Rb
            -> root Rigidbody2D.linearVelocity
```

The player prefab should not use a second simulated Rigidbody2D for the player collider hierarchy. Multiple active Rigidbody2D components can make collision movement difficult to diagnose because the state machine may read one body while another body participates in contacts.

## Debugging Checklist

When the player moves without input:

1. Check the `GroundedState` debug log for `Ctx.MoveInput`.
2. Check Rigidbody velocity before and after the HSM tick.
3. Confirm only one `PlayerStateDriverShell` exists at runtime.
4. Confirm the legacy `PlayerStateDriver` is not active.
5. Confirm `PlayerContext.Rb` references the root player Rigidbody2D.
6. Check `KnockBack.IsBeingKnockedBack` and any active knockback coroutine.
7. Check the player parent for unexpected transform motion.
8. Check the `GroundLayer` mask and feet BoxCast configuration.
9. Check that the root Rigidbody2D is simulated and the player collider hierarchy does not contain a second active body.

Ctx.isGrounded = hit.collider != null;
```

The ground check is performed before the grounded transition decision. This allows the state to transition to `AirborneState` when the feet no longer detect ground.

### IdleState

`IdleState` is the initial grounded substate.

Behavior:

- Disables dust effects.
- Transitions to `WalkState` when horizontal input is greater than `0.01` in magnitude.
- Transitions to `RunState` instead when horizontal input exists and `RunHeld` is true.

### WalkState

Behavior:

- Plays grounded dust while the player is moving.
- Transitions to `RunState` when `RunHeld` is true and horizontal input exists.
- Transitions to `IdleState` when input is released or the Rigidbody is stationary.

### RunState

Behavior:

- Plays grounded dust while the player is moving.
- Transitions to `WalkState` when the run button is released but horizontal input remains.
- Transitions to `IdleState` when input is released or the Rigidbody is stationary.

## Grounded Horizontal Movement

Grounded movement is calculated in `GroundedState.OnFixedUpdate()`.

The input value is read from:

```csharp
Ctx.MoveInput.x
```

The speed is selected from `PlayerMovementStats`:

```csharp
float targetSpeed = Ctx.RunHeld
    ? Ctx.MoveStats.MaxRunSpeed
    : Ctx.MoveStats.MaxWalkSpeed;
```

When there is no horizontal input, velocity decelerates toward zero:

```csharp
float newVelX = Mathf.Lerp(
    currentVelX,
    0f,
    decel * deltaTime);

Ctx.Rb.linearVelocity = new Vector2(
    newVelX,
    Ctx.Rb.linearVelocity.y);
```

When horizontal input exists, velocity approaches the input target speed:

```csharp
float targetVelX = Ctx.MoveInput.x * targetSpeed;
float newVelX = Mathf.Lerp(
    currentVelX,
    targetVelX,
    accel * deltaTime);

Ctx.Rb.linearVelocity = new Vector2(
    newVelX,
    Ctx.Rb.linearVelocity.y);
```

This is the active horizontal movement write for the current HSM player path.

## AirborneState

File: `Assets/Project/Scripts/Characters/Player/StateMachine/HSM/CongreateStates/AirborneState.cs`

`AirborneState` handles:

- Jumping.
- Falling.
- Apex handling.
- Jump release and fast fall.
- Coyote time.
- Jump buffering.
- Ground detection while airborne.

On entry, it clears grounded state and initializes airborne flags. `AttemptJump()` may then set the initial upward velocity.

During `FixedUpdate`, it calculates vertical velocity using the gravity value from `PlayerMovementStats`:

```csharp
Ctx.VerticalVelocity += Ctx.MoveStats.Gravity * deltaTime;
```

The result is clamped to the configured maximum fall speed:

```csharp
float maxFall = -Ctx.MoveStats.MaxFallSpeed;
Ctx.VerticalVelocity = Mathf.Clamp(
    Ctx.VerticalVelocity,
    maxFall,
    50f);
```

The calculated velocity is written to the Rigidbody through `ApplyVelocity()`:

```csharp
Ctx.Velocity = new Vector2(
    Ctx.Velocity.x,
    Ctx.VerticalVelocity);

ApplyVelocity(Ctx.Velocity);
```

`ApplyVelocity()` is defined in `PlayerBaseState`:

```csharp
protected void ApplyVelocity(Vector2 vel)
{
    Ctx.Rb.linearVelocity = vel;
}
```

The airborne state transitions back to `GroundedState` when the ground check succeeds and the player is descending.

## Complete Runtime Tick Order

### Variable-frame update

```text
PlayerStateDriverShell.Update()
    -> PlayerController.Tick(deltaTime)
        -> Read InputManager properties
        -> Copy input into PlayerContext
        -> StateMachine.Tick(deltaTime)
            -> Check transitions
            -> Update active child state
            -> Run state OnUpdate()
```

### Fixed physics update

```text
PlayerStateDriverShell.FixedUpdate()
    -> PlayerController.TickFixed(fixedDeltaTime)
        -> Sync Rigidbody into PlayerContext
        -> StateMachine.TickFixedUpdate(fixedDeltaTime)
            -> Run active state OnFixedUpdate()
                -> GroundedState writes horizontal velocity
                -> AirborneState writes vertical velocity
```

## Important Data Flow

```text
Keyboard / controller
    -> InputManager.Movement
    -> PlayerController.State.MoveInput
    -> GroundedState targetVelX
    -> Rigidbody2D.linearVelocity.x
```

For jumping and falling:

```text
Jump input or lost ground
    -> AirborneState
    -> PlayerMovementStats.Gravity
    -> PlayerContext.VerticalVelocity
    -> PlayerBaseState.ApplyVelocity()
    -> Rigidbody2D.linearVelocity.y
```

## Movement Statistics

File: `Assets/Project/Scripts/Characters/Player/PlayerMovementStats.asset`

The main values used by the HSM are:

- `MaxWalkSpeed`
- `MaxRunSpeed`
- `GroundAcceleration`
- `GroundDeceleration`
- `AirAcceleration`
- `AirDeceleration`
- `JumpHeight`
- `TimeTillJumpApex`
- `GravityOnReleaseMultiplier`
- `MaxFallSpeed`
- `GroundLayer`
- `OneWayPlatform`

`Gravity` and `InitialJumpVelocity` are calculated from the jump height and time-to-apex values when the stats asset is enabled or validated.

## Physics Ownership

The player movement HSM should write to one active root `Rigidbody2D`:

```text
PlayerStateDriverShell
    -> PlayerController
        -> PlayerContext.Rb
            -> root Rigidbody2D.linearVelocity
```

The player prefab should not use a second simulated Rigidbody2D for the player collider hierarchy. Multiple active Rigidbody2D components can make collision movement difficult to diagnose because the state machine may read one body while another body participates in contacts.

## Debugging Checklist

When the player moves without input:

1. Check the `GroundedState` debug log for `Ctx.MoveInput`.
2. Check Rigidbody velocity before and after the HSM tick.
3. Confirm only one `PlayerStateDriverShell` exists at runtime.
4. Confirm the legacy `PlayerStateDriver` is not active.
5. Confirm `PlayerContext.Rb` references the root player Rigidbody2D.
6. Check `KnockBack.IsBeingKnockedBack` and any active knockback coroutine.
7. Check the player parent for unexpected transform motion.
8. Check the `GroundLayer` mask and feet BoxCast configuration.
9. Check that the root Rigidbody2D is simulated and the player collider hierarchy does not contain a second active body.
