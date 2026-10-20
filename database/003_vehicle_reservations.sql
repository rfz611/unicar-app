-- Banco criado com os modelos da API. Preserva usuários e caronas existentes.
USE unicar_db;
SET @has_column = (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='vehicle_type');
SET @ddl = IF(@has_column=0, 'ALTER TABLE rides ADD COLUMN vehicle_type VARCHAR(4) NOT NULL DEFAULT "CAR"', 'SELECT 1');
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
CREATE TABLE IF NOT EXISTS ride_reservations (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    RideId INT NOT NULL,
    PassengerId INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    UNIQUE KEY IX_reservation_ride_passenger (RideId, PassengerId),
    FOREIGN KEY (RideId) REFERENCES rides(id) ON DELETE CASCADE,
    FOREIGN KEY (PassengerId) REFERENCES users(id) ON DELETE CASCADE
);
