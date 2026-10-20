-- Migração aditiva para o banco de modelos API + script 003.
USE unicar_db;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='pricing_mode');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `pricing_mode` VARCHAR(20) NOT NULL DEFAULT ''LegacyUnknown''','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='reference_amount');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `reference_amount` DECIMAL(10,2) NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='original_seats');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `original_seats` INT NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='origin_latitude');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `origin_latitude` DOUBLE NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='origin_longitude');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `origin_longitude` DOUBLE NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='destination_latitude');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `destination_latitude` DOUBLE NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='destination_longitude');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `destination_longitude` DOUBLE NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='origin_place_id');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `origin_place_id` LONGTEXT NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='destination_place_id');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `destination_place_id` LONGTEXT NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='rides' AND column_name='pricing_config_version');
SET @ddl=IF(@present=0,'ALTER TABLE `rides` ADD COLUMN `pricing_config_version` LONGTEXT NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='ride_reservations' AND column_name='AgreedAmount');
SET @ddl=IF(@present=0,'ALTER TABLE `ride_reservations` ADD COLUMN `AgreedAmount` DECIMAL(10,2) NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='ride_reservations' AND column_name='Currency');
SET @ddl=IF(@present=0,'ALTER TABLE `ride_reservations` ADD COLUMN `Currency` VARCHAR(3) NOT NULL DEFAULT ''BRL''','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='ride_reservations' AND column_name='PricingMode');
SET @ddl=IF(@present=0,'ALTER TABLE `ride_reservations` ADD COLUMN `PricingMode` VARCHAR(20) NOT NULL DEFAULT ''LegacyUnknown''','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
SET @present=(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='ride_reservations' AND column_name='NegotiationId');
SET @ddl=IF(@present=0,'ALTER TABLE `ride_reservations` ADD COLUMN `NegotiationId` INT NULL','SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
UPDATE rides r SET original_seats=available_seats+(SELECT COUNT(*) FROM ride_reservations b WHERE b.RideId=r.id) WHERE original_seats IS NULL;
ALTER TABLE rides MODIFY COLUMN original_seats INT NOT NULL;
CREATE TABLE IF NOT EXISTS fare_negotiations (
 Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, RideId INT NOT NULL, PassengerId INT NOT NULL,
 LatestAuthorId INT NOT NULL, LatestAmount DECIMAL(10,2) NOT NULL, Version INT NOT NULL,
 Status VARCHAR(20) NOT NULL, CreatedAt DATETIME(6) NOT NULL, UpdatedAt DATETIME(6) NOT NULL,
 UNIQUE KEY IX_negotiation_ride_passenger(RideId,PassengerId),
 FOREIGN KEY(RideId) REFERENCES rides(id) ON DELETE CASCADE,
 FOREIGN KEY(PassengerId) REFERENCES users(id) ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS fare_proposals (
 Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, NegotiationId INT NOT NULL, AuthorId INT NOT NULL,
 Amount DECIMAL(10,2) NOT NULL, CreatedAt DATETIME(6) NOT NULL,
 FOREIGN KEY(NegotiationId) REFERENCES fare_negotiations(Id) ON DELETE CASCADE,
 FOREIGN KEY(AuthorId) REFERENCES users(id) ON DELETE RESTRICT
);
