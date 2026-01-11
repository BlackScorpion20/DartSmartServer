# DartSmartServer API Reference

**Base URL**: `http://localhost:8080`

---

## Authentication

### Register
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "PlayerName",
  "email": "player@example.com",
  "password": "SecurePass123!"
}
```
**Response**: `{ "accessToken": "jwt...", "refreshToken": "...", "player": {...} }`

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "player@example.com",
  "password": "SecurePass123!"
}
```
**Response**: Same as Register

---

## Games (🔐 Auth Required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/games` | List active games |
| POST | `/api/games` | Create game |
| POST | `/api/games/{id}/join` | Join game |
| POST | `/api/games/{id}/start` | Start game |
| POST | `/api/games/{id}/throw` | Register throw |

### Create Game
```json
{
  "gameType": "X01_501",
  "startScore": 501,
  "inMode": "StraightIn",
  "outMode": "DoubleOut"
}
```

### Register Throw
```json
{
  "segment": 20,
  "multiplier": 3,
  "dartNumber": 1
}
```

---

## SignalR Hubs

### GameHub
**URL**: `ws://localhost:8080/hubs/game?access_token={JWT}`

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinGame` | gameId | Join game room |
| `LeaveGame` | gameId | Leave game room |
| `ThrowDart` | gameId, segment, multiplier, dartNumber | Register throw |
| `StartGame` | gameId | Start the game |

**Events received**:
- `PlayerJoined` - Player joined game
- `PlayerLeft` - Player left
- `ThrowRegistered` - Dart throw result
- `TurnComplete` - Turn ended
- `GameStarted` - Game started

### LobbyHub
**URL**: `ws://localhost:8080/hubs/lobby?access_token={JWT}`

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinLobby` | - | Enter matchmaking |
| `LeaveLobby` | - | Exit matchmaking |
| `GetMatches` | avgTolerance | Find similar players |
| `ChallengePlayer` | targetPlayerId | Challenge player |

**Events received**:
- `PlayerJoinedLobby`
- `PlayerLeftLobby`
- `MatchesFound`
- `ChallengeReceived`

---

## Auth Header
```
Authorization: Bearer {accessToken}
```

## Enums
- **GameType**: `X01_301`, `X01_501`, `X01_701`, `Cricket`
- **InMode**: `StraightIn`, `DoubleIn`, `MasterIn`
- **OutMode**: `StraightOut`, `DoubleOut`, `MasterOut`
