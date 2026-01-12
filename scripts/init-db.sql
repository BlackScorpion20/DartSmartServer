-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Create schema for DartSmart
CREATE SCHEMA IF NOT EXISTS dartsmart;

-- Set search path
SET search_path TO dartsmart, public;

-- Note: EF Core Migrations will create the actual tables
-- This script only initializes the database with TimescaleDB extension
