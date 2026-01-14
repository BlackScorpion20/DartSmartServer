# DartSmartNet Server

ASP.NET Core 9.0 Web API mit SignalR für DartSmartNet Multiplayer-Funktionalität.

## Architektur

Clean Architecture mit folgenden Layers:
- **Domain**: Entities, ValueObjects, Enums, Events
- **Application**: Service Interfaces, DTOs, Repository Interfaces
- **Infrastructure**: EF Core, Repositories, Authentication, Bot AI
- **API**: Controllers, SignalR Hubs, Middleware

## Technologie Stack

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core 9.0
- PostgreSQL 16 + TimescaleDB
- SignalR (Real-time Communication)
- JWT Bearer Authentication
- Redis (optional, für Caching/Leaderboards)

## Projekt Status

### ✅ Abgeschlossen
- [x] Domain Layer (Entities, ValueObjects, Events)
  - User, PlayerStats, GameSession, GamePlayer, DartThrow, Bot
  - Score ValueObject mit Multiplier
  - GameType, GameStatus, BotDifficulty Enums
- [x] Infrastructure Layer (EF Core, DbContext)
  - ApplicationDbContext
  - Entity Configurations für alle Entities
  - Fluent API Mappings
- [x] Application Layer (Interfaces, DTOs)
  - Repository Interfaces (IUserRepository, IGameRepository, etc.)
  - Service Interfaces (IAuthService, IGameService, IBotService, etc.)
  - DTOs für Auth, Game State, Statistics
- [x] Repository Implementierungen
  - UserRepository, GameRepository, StatsRepository, BotRepository
- [x] Docker Compose Setup
  - PostgreSQL 16 mit TimescaleDB Extension
  - Redis Container
  - Health Checks
  - Init Script mit Schema und Seed Data

### 🚧 In Arbeit / Ausstehend
- [ ] Authentication System (JWT Token Service)
- [ ] Bot AI Engine mit 4 Schwierigkeitsgraden
- [ ] Service Implementierungen (AuthService, GameService, etc.)
- [ ] SignalR GameHub für Multiplayer
- [ ] API Controllers (AuthController, GameController, StatsController)
- [ ] Middleware (Exception Handling, JWT Validation)
- [ ] Matchmaking Service
- [ ] Training Mode Games
- [ ] Client-Server Integration

## Setup & Installation

### Voraussetzungen
- .NET 9.0 SDK
- Docker Desktop
- Git

### 1. Repository klonen
```bash
cd server
```

### 2. Datenbank starten
```bash
docker-compose up -d
```

Dies startet:
- PostgreSQL 16 mit TimescaleDB auf Port 5432
- Redis auf Port 6379
- Automatische DB Initialisierung mit Schema und Seed Data (4 Bots)

### 3. Connection String konfigurieren
Kopiere `.env.example` zu `.env` und passe Werte an:
```bash
cp .env.example .env
```

Wichtige Settings:
- `DATABASE_PASSWORD`: Ändere das Passwort in Production!
- `JWT_SECRET`: Generiere einen sicheren Secret Key!

### 4. Projekt builden
```bash
dotnet build
```

### 5. (TODO) Server starten
```bash
cd src/DartSmartNet.Server.API
dotnet run
```

API läuft dann auf: `http://localhost:5000`

## Datenbank

### Schema

**TimescaleDB Hypertables:**
- `game_sessions` - Partitioniert nach `started_at`
- `dart_throws` - Partitioniert nach `thrown_at`

**Normale Tabellen:**
- `users` - User Accounts
- `player_stats` - User Statistiken
- `game_players` - N:M Relation Game <-> User
- `bots` - Bot Konfigurationen

### Seeded Bots

4 Bots werden automatisch erstellt:
1. **Easy Bot** (PPD: 35, Checkout: 20%)
2. **Medium Bot** (PPD: 55, Checkout: 40%)
3. **Hard Bot** (PPD: 75, Checkout: 60%)
4. **Expert Bot** (PPD: 90, Checkout: 80%)

### Migrations (TODO)

```bash
# Migration erstellen
dotnet ef migrations add InitialCreate --project src/DartSmartNet.Server.Infrastructure --startup-project src/DartSmartNet.Server.API

# Migration anwenden
dotnet ef database update --project src/DartSmartNet.Server.Infrastructure --startup-project src/DartSmartNet.Server.API
```

## API Endpoints (Geplant)

### Authentication
- `POST /api/auth/register` - User Registrierung
- `POST /api/auth/login` - User Login
- `POST /api/auth/refresh` - Token Refresh

### Games
- `POST /api/game/create` - Neues Spiel erstellen
- `GET /api/game/{id}` - Spiel abrufen
- `GET /api/game/user/{userId}` - User Games History

### Statistics
- `GET /api/stats/user/{userId}` - User Statistiken
- `GET /api/stats/leaderboard` - Global Leaderboard

### SignalR Hub
- Hub URL: `/gamehub`
- Events:
  - `JoinGame(gameId)`
  - `RegisterThrow(gameId, throwData)`
  - `OnGameStateChanged`
  - `OnTurnChanged`
  - `OnGameEnded`

## Game Types

### Competitive Games
- **X01** (301, 501, 701, etc.)
- **Cricket**
- **Around the Clock**
- **Shanghai**

### Training Modes
- **Bob's Doubles** - Double-Ring Training
- **Bob's Triples** - Triple-Ring Training
- **Bullseye Practice** - Bull Training
- **Segment Focus** - Bestimmtes Segment üben
- **Checkout Practice** - Finish-Training
- **Round the Board** - 1-20 in Reihenfolge
- **Shanghai Drill** - Kombinationstraining

## Development

### Project Structure
```
server/
├── src/
│   ├── DartSmartNet.Server.Domain/       # Entities, ValueObjects
│   ├── DartSmartNet.Server.Application/  # Interfaces, DTOs
│   ├── DartSmartNet.Server.Infrastructure/  # EF Core, Repositories
│   └── DartSmartNet.Server.API/          # Controllers, Hubs
├── docker-compose.yml
├── scripts/
│   └── init-db.sql
└── README.md
```

### Tests (TODO)
```bash
dotnet test
```

## Nächste Schritte

1. **JWT Authentication Service** implementieren (BCrypt Password Hashing)
2. **Bot AI Engine** mit probabilistischem Wurf-Algorithmus
3. **Game Services** für Business Logic
4. **SignalR Hub** für Real-time Multiplayer
5. **API Controllers** mit Input Validation
6. **Exception Middleware** für Error Handling
7. **Integration Tests** schreiben
8. **Client Integration** mit SignalR Client

## Lizenz

Proprietary - Alle Rechte vorbehalten
