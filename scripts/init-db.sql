-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Users Table
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_login_at TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT TRUE,
    role VARCHAR(20) DEFAULT 'User'
);

-- Refresh Tokens Table
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(500) UNIQUE NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMPTZ
);

-- Player Stats Table
CREATE TABLE IF NOT EXISTS player_stats (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(id) ON DELETE CASCADE,
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
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Game Sessions Table (TimescaleDB Hypertable)
CREATE TABLE IF NOT EXISTS game_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_type VARCHAR(50) NOT NULL,
    starting_score INT,
    status VARCHAR(20) DEFAULT 'WaitingForPlayers',
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    winner_id UUID REFERENCES users(id),
    is_online BOOLEAN DEFAULT FALSE,
    is_bot_game BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Convert to TimescaleDB hypertable
SELECT create_hypertable('game_sessions', 'started_at', if_not_exists => TRUE);

-- Game Players Table
CREATE TABLE IF NOT EXISTS game_players (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_id UUID NOT NULL REFERENCES game_sessions(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id),
    player_order INT NOT NULL,
    final_score INT,
    darts_thrown INT DEFAULT 0,
    points_scored INT DEFAULT 0,
    ppd DECIMAL(5,2) DEFAULT 0,
    is_winner BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(game_id, user_id)
);

-- Dart Throws Table (TimescaleDB Hypertable)
CREATE TABLE IF NOT EXISTS dart_throws (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    game_id UUID NOT NULL REFERENCES game_sessions(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id),
    round_number INT NOT NULL,
    dart_number INT NOT NULL CHECK (dart_number BETWEEN 1 AND 3),
    segment INT NOT NULL,
    multiplier INT NOT NULL CHECK (multiplier BETWEEN 1 AND 3),
    points INT NOT NULL,
    thrown_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    raw_data BYTEA,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Convert to TimescaleDB hypertable
SELECT create_hypertable('dart_throws', 'thrown_at', if_not_exists => TRUE);

-- Bots Table
CREATE TABLE IF NOT EXISTS bots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL,
    difficulty VARCHAR(20) NOT NULL,
    avg_ppd DECIMAL(5,2) NOT NULL,
    consistency_factor DECIMAL(3,2) NOT NULL,
    checkout_skill DECIMAL(3,2) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create Indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_revoked ON refresh_tokens(user_id, is_revoked);
CREATE INDEX IF NOT EXISTS idx_player_stats_user_id ON player_stats(user_id);
CREATE INDEX IF NOT EXISTS idx_game_sessions_status ON game_sessions(status);
CREATE INDEX IF NOT EXISTS idx_game_sessions_winner ON game_sessions(winner_id);
CREATE INDEX IF NOT EXISTS idx_game_players_game_id ON game_players(game_id);
CREATE INDEX IF NOT EXISTS idx_game_players_user_id ON game_players(user_id);
CREATE INDEX IF NOT EXISTS idx_dart_throws_game_user ON dart_throws(game_id, user_id);
CREATE INDEX IF NOT EXISTS idx_dart_throws_user_time ON dart_throws(user_id, thrown_at DESC);

-- Insert Default Bots
INSERT INTO bots (name, difficulty, avg_ppd, consistency_factor, checkout_skill) VALUES
    ('Easy Bot', 'Easy', 35.00, 0.30, 0.20),
    ('Medium Bot', 'Medium', 55.00, 0.50, 0.40),
    ('Hard Bot', 'Hard', 75.00, 0.70, 0.60),
    ('Expert Bot', 'Expert', 90.00, 0.85, 0.80)
ON CONFLICT DO NOTHING;

-- Create function to automatically update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger for player_stats
CREATE TRIGGER update_player_stats_updated_at BEFORE UPDATE ON player_stats
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
