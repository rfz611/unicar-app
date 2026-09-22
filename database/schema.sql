-- Criação do banco de dados (caso não exista)
CREATE DATABASE IF NOT EXISTS unicar_db;
USE unicar_db;

-- 1. Tabela de Usuários (Alunos, Professores e Colaboradores)
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    ra_enrollment VARCHAR(30) NOT NULL UNIQUE,
    user_type ENUM('DRIVER', 'PASSENGER', 'BOTH') DEFAULT 'BOTH',
    profile_photo_url VARCHAR(255) NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 2. Tabela de Veículos (Cadastrados por condutores)
CREATE TABLE IF NOT EXISTS vehicles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    make VARCHAR(50) NOT NULL,
    model VARCHAR(50) NOT NULL,
    color VARCHAR(30) NOT NULL,
    plate VARCHAR(10) NOT NULL UNIQUE,
    seat_capacity TINYINT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_vehicles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- 3. Tabela de Ofertas de Carona
CREATE TABLE IF NOT EXISTS rides (
    id INT AUTO_INCREMENT PRIMARY KEY,
    driver_id INT NOT NULL,
    vehicle_id INT NOT NULL,
    origin_address VARCHAR(255) NOT NULL,
    destination_address VARCHAR(255) NOT NULL,
    departure_time DATETIME NOT NULL,
    available_seats TINYINT NOT NULL,
    price_contribution DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    status ENUM('SCHEDULED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED') DEFAULT 'SCHEDULED',
    observations VARCHAR(500) NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_rides_driver FOREIGN KEY (driver_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_rides_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(id) ON DELETE RESTRICT
);

-- 4. Tabela de Solicitações / Reservas de Carona
CREATE TABLE IF NOT EXISTS ride_requests (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ride_id INT NOT NULL,
    passenger_id INT NOT NULL,
    pickup_address VARCHAR(255) NOT NULL,
    status ENUM('PENDING', 'ACCEPTED', 'REJECTED', 'CANCELLED') DEFAULT 'PENDING',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_requests_ride FOREIGN KEY (ride_id) REFERENCES rides(id) ON DELETE CASCADE,
    CONSTRAINT fk_requests_passenger FOREIGN KEY (passenger_id) REFERENCES users(id) ON DELETE CASCADE
);

-- 5. Tabela de Avaliações Mútuas
CREATE TABLE IF NOT EXISTS ratings (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ride_id INT NOT NULL,
    reviewer_id INT NOT NULL,
    reviewed_id INT NOT NULL,
    score TINYINT NOT NULL CHECK (score BETWEEN 1 AND 5),
    comment VARCHAR(300) NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_ratings_ride FOREIGN KEY (ride_id) REFERENCES rides(id) ON DELETE CASCADE,
    CONSTRAINT fk_ratings_reviewer FOREIGN KEY (reviewer_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_ratings_reviewed FOREIGN KEY (reviewed_id) REFERENCES users(id) ON DELETE CASCADE
);