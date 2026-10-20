-- Alternativa ao "dotnet ef database update": aplica as colunas novas em um banco que já existe.
-- Use UM dos dois caminhos (migration do EF OU este script), nunca os dois.
USE unicar_db;

ALTER TABLE users
    ADD COLUMN cnh_number VARCHAR(11) NULL,
    ADD COLUMN cnh_category VARCHAR(2) NULL,
    ADD COLUMN cnh_expiry DATE NULL,
    ADD COLUMN password_reset_code_hash VARCHAR(255) NULL,
    ADD COLUMN password_reset_expires_at DATETIME NULL,
    ADD COLUMN password_reset_attempts INT NOT NULL DEFAULT 0;

CREATE UNIQUE INDEX IX_users_cnh_number ON users (cnh_number);
