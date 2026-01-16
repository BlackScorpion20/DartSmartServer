# DartSmartNet Server Architecture

## Overview
ASP.NET Core Web API mit SignalR für Real-time Multiplayer-Funktionalität, PostgreSQL/TimescaleDB für Statistiken und Game History.

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 9.0 Web API
- **Real-time**: SignalR Hub
- **Database**: PostgreSQL 16 + TimescaleDB
- **ORM**: Entity Framework Core 9.0
- **Authentication**: JWT Bearer Tokens
- **Caching**: Redis (optional, für Leaderboards)

### Architecture Pattern
- **Clean Architecture** (Domain → Application → Infrastructure → API)
- **DDD** (Domain-Driven Design)
- **CQRS** (Command Query Responsibility Segregation) - optional
- **Event-Driven** (SignalR Events)

## Layer Structure

```
Server/
├── Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── GameSession.cs
│   │   ├── GameHistory.cs
│   │   ├── PlayerStats.cs
│   │   ├── DartThrow.cs
│   │   └── Bot.cs
│   ├── ValueObjects/
│   │   ├── Score.cs
│   │   ├── UserId.cs
│   │   └── GameId.cs
│   ├── Enums/
│   │   ├── GameType.cs (X01, Cricket, AroundTheClock)
│   │   ├── BotDifficulty.cs (Easy, Medium, Hard, Expert)
│   │   └── GameStatus.cs
│   └── Events/
│       ├── GameStartedEvent.cs
│       ├── ThrowRegisteredEvent.cs
│       └── GameEndedEvent.cs
│
├── Application/
│   ├── Services/
│   │   ├── IAuthService.cs
│   │   ├── IGameService.cs
│   │   ├── IStatisticsService.cs
│   │   ├── IBotService.cs
│   │   └── IMatchmakingService.cs
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   ├── RegisterRequest.cs
│   │   ├── GameStateDto.cs
│   │   └── PlayerStatsDto.cs
│   └── Interfaces/
│       ├── IUserRepository.cs
│       ├── IGameRepository.cs
│       └── IStatsRepository.cs
│
├── Infrastructure/
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── Authentication/
│   │   ├── JwtTokenService.cs
│   │   └── UserService.cs
│   ├── AI/
│   │   ├── BotEngine.cs
│   │   ├── EasyBot.cs
│   │   ├── MediumBot.cs
│   │   ├── HardBot.cs
│   │   └── ExpertBot.cs
│   └── SignalR/
│       └── GameHub.cs
│
└── API/
    ├── Controllers/
    │   ├── AuthController.cs
    │   ├── GameController.cs
    │   ├── StatsController.cs
    │   └── UserController.cs
    ├── Hubs/
    │   └── GameHub.cs
    ├── Middleware/
    │   ├── ExceptionMiddleware.cs
    │   └── JwtMiddleware.cs
    └── Program.cs
```

## Database Schema

### Users Table
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_login_at TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT TRUE,
    role VARCHAR(20) DEFAULT 'User'
);
```

### Player_Stats Table
```sql
CREATE TABLE player_stats (
    user_id UUID PRIMARY KEY REFERENCES users(id),
    games_played INT DEFAULT 0,
    games_won INT DEFAULT 0,
    games_lost INT DEFAULT 0,
    total_darts_thrown INT DEFAULT 0,
    total_points_scored INT DEFAULT 0,
    average_ppd DECIMAL(5,2) DEFAULT 0,
    highest_checkout INT DEFAULT 0,
    total_180s INT DEFAULT 0,
    total_171s INT DEFAULT 0,
    total_140s INT DEFAULT 0,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Game_Sessions Table (TimescaleDB Hypertable)
```sql
CREATE TABLE game_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_type VARCHAR(50) NOT NULL, -- X01, Cricket, etc.
    starting_score INT,
    status VARCHAR(20) DEFAULT 'InProgress', -- InProgress, Completed, Abandoned
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    winner_id UUID REFERENCES users(id),
    is_online BOOLEAN DEFAULT FALSE,
    is_bot_game BOOLEAN DEFAULT FALSE
);

-- Convert to TimescaleDB hypertable for time-series optimization
SELECT create_hypertable('game_sessions', 'started_at');
```

### Game_Players Table
```sql
CREATE TABLE game_players (
    game_id UUID REFERENCES game_sessions(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    player_order INT NOT NULL,
    final_score INT,
    darts_thrown INT DEFAULT 0,
    points_scored INT DEFAULT 0,
    ppd DECIMAL(5,2),
    is_winner BOOLEAN DEFAULT FALSE,
    PRIMARY KEY (game_id, user_id)
);
```

### Dart_Throws Table (TimescaleDB Hypertable)
```sql
CREATE TABLE dart_throws (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_id UUID REFERENCES game_sessions(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    round_number INT NOT NULL,
    dart_number INT NOT NULL CHECK (dart_number BETWEEN 1 AND 3),
    segment INT NOT NULL,
    multiplier INT NOT NULL CHECK (multiplier BETWEEN 1 AND 3),
    points INT NOT NULL,
    thrown_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_data BYTEA -- Granboard raw data
);

-- Convert to TimescaleDB hypertable
SELECT create_hypertable('dart_throws', 'thrown_at');

-- Create index for fast querying
CREATE INDEX idx_dart_throws_game_user ON dart_throws(game_id, user_id);
CREATE INDEX idx_dart_throws_user_time ON dart_throws(user_id, thrown_at DESC);
```

### Bots Table
```sql
CREATE TABLE bots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL,
    difficulty VARCHAR(20) NOT NULL, -- Easy, Medium, Hard, Expert
    avg_ppd DECIMAL(5,2) NOT NULL,
    consistency_factor DECIMAL(3,2) NOT NULL, -- 0.0-1.0
    checkout_skill DECIMAL(3,2) NOT NULL, -- 0.0-1.0
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

## SignalR Hub Events

### Client → Server
- `JoinGame(gameId)` - Join existing game
- `CreateGame(gameType, startingScore, playerCount)` - Create new game
- `RegisterThrow(gameId, throwData)` - Register a dart throw
- `NextPlayer(gameId)` - Switch to next player
- `LeaveGame(gameId)` - Leave current game

### Server → Client
- `GameStateChanged(gameState)` - Game state updated
- `PlayerJoined(playerInfo)` - Player joined game
- `PlayerLeft(playerInfo)` - Player left game
- `ThrowRegistered(throwInfo)` - Throw was registered
- `TurnChanged(currentPlayerId)` - Turn changed
- `GameEnded(winnerInfo, stats)` - Game finished
- `BotThrowSimulated(throwInfo)` - Bot made a throw

## Bot AI System

### Difficulty Levels

**Easy Bot** (PPD: 30-40)
- Random scatter: ±5 segments
- Low checkout success: 20%
- Prefers singles
- Simulated delay: 2-4s

**Medium Bot** (PPD: 50-60)
- Moderate accuracy: ±2 segments
- Checkout success: 40%
- Occasionally hits triples
- Simulated delay: 1.5-3s

**Hard Bot** (PPD: 70-80)
- Good accuracy: ±1 segment
- Checkout success: 60%
- Frequently hits triples
- Strategic finishing
- Simulated delay: 1-2.5s

**Expert Bot** (PPD: 85-95)
- Very accurate: exact segment 80% of time
- Checkout success: 80%
- Always aims for triples (T20, T19, T18)
- Optimal checkout paths
- Simulated delay: 1-2s

### Bot Throw Algorithm
```csharp
public Score SimulateThrow(int currentScore, BotDifficulty difficulty)
{
    // 1. Determine target (T20, checkout number, etc.)
    var target = CalculateOptimalTarget(currentScore);

    // 2. Apply difficulty-based accuracy
    var hitProbability = GetHitProbability(difficulty);
    var actualHit = ApplyAccuracy(target, hitProbability);

    // 3. Add random variance
    var finalScore = ApplyVariance(actualHit, difficulty);

    return finalScore;
}
```

## Authentication Flow

### Registration
```
POST /api/auth/register
{
  "username": "player1",
  "email": "player1@example.com",
  "password": "SecurePassword123!"
}

Response: 201 Created
{
  "userId": "uuid",
  "username": "player1",
  "token": "jwt-token",
  "refreshToken": "refresh-token"
}
```

### Login
```
POST /api/auth/login
{
  "username": "player1",
  "password": "SecurePassword123!"
}

Response: 200 OK
{
  "userId": "uuid",
  "username": "player1",
  "token": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresIn": 3600
}
```

### Token Refresh
```
POST /api/auth/refresh
{
  "refreshToken": "refresh-token"
}

Response: 200 OK
{
  "token": "new-jwt-token",
  "refreshToken": "new-refresh-token",
  "expiresIn": 3600
}
```

## Game Flow (Online Multiplayer)

### 1. Matchmaking
```
POST /api/game/matchmaking/join
{
  "gameType": "X01",
  "startingScore": 501
}

Response: SignalR notification when match found
```

### 2. Game Start
```
Server → All Clients: GameStateChanged
{
  "gameId": "uuid",
  "players": [...],
  "currentPlayerIndex": 0,
  "status": "InProgress"
}
```

### 3. Player Throws
```
Client → Server (SignalR): RegisterThrow
{
  "gameId": "uuid",
  "throwData": { "segment": 20, "multiplier": 3 }
}

Server → All Clients: ThrowRegistered + GameStateChanged
```

### 4. Game End
```
Server → All Clients: GameEnded
{
  "winner": { "userId": "uuid", "username": "player1" },
  "finalStats": [...],
  "gameHistory": "uuid"
}
```

## Leaderboard System

### Global Leaderboard
```
GET /api/stats/leaderboard?type=ppd&limit=100

Response:
[
  { "rank": 1, "username": "pro_player", "ppd": 92.5, "gamesPlayed": 500 },
  { "rank": 2, "username": "dart_master", "ppd": 88.3, "gamesPlayed": 350 },
  ...
]
```

### Personal Stats
```
GET /api/stats/user/{userId}

Response:
{
  "userId": "uuid",
  "username": "player1",
  "stats": {
    "gamesPlayed": 150,
    "gamesWon": 75,
    "winRate": 50.0,
    "avgPPD": 65.5,
    "highest180s": 5,
    "highestCheckout": 170
  },
  "recentGames": [...]
}
```

## Deployment

### Docker Compose Setup
```yaml
version: '3.8'

services:
  postgres:
    image: timescale/timescaledb:latest-pg16
    environment:
      POSTGRES_DB: dartsmart
      POSTGRES_USER: dartsmart
      POSTGRES_PASSWORD: <secure-password>
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:alpine
    ports:
      - "6379:6379"

  api:
    build: ./server
    depends_on:
      - postgres
      - redis
    environment:
      - ConnectionStrings__DefaultConnection=<connection-string>
      - JWT__Secret=<jwt-secret>
    ports:
      - "5000:80"

volumes:
  postgres_data:
```

## Performance Considerations

1. **Caching**: Redis für Leaderboards, aktive Games
2. **TimescaleDB**: Automatische Daten-Compression für alte Würfe
3. **SignalR Scaling**: Redis Backplane für Multi-Server
4. **Connection Pooling**: EF Core Connection Pooling
5. **Query Optimization**: Indexes auf häufig abgefragten Spalten

## Security

1. **JWT**: HttpOnly Cookies für Refresh Tokens
2. **Rate Limiting**: API Rate Limiting pro User
3. **SQL Injection**: Parameterized Queries (EF Core)
4. **CORS**: Whitelist nur Client-Origins
5. **Password Hashing**: BCrypt mit Salt
6. **HTTPS**: TLS 1.3 enforced

## Next Steps

1. ✅ Setup Server Project Structure
2. ✅ Implement Domain Entities
3. ⏳ Setup PostgreSQL/TimescaleDB with Docker
4. ⏳ Implement Authentication (JWT)
5. ⏳ Implement SignalR GameHub
6. ⏳ Implement Bot AI System
7. ⏳ Implement Statistics Service
8. ⏳ Client-Server Integration
9. ⏳ Testing & Deployment
