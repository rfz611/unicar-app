USE unicar_db;

-- Limpeza prévia para evitar duplicados em reexecuções
DELETE FROM ratings;
DELETE FROM ride_requests;
DELETE FROM rides;
DELETE FROM vehicles;
DELETE FROM users;

-- 1. Utilizadores institucionais de teste
INSERT INTO users (id, name, email, password_hash, phone, ra_enrollment, user_type, profile_photo_url)
VALUES 
(1, 'Lucas Oliveira', 'lucas.oliveira@positivo.edu.br', '$2a$11$mockhashpass123', '41999991111', 'RA202401', 'DRIVER', NULL),
(2, 'Beatriz Santos', 'beatriz.santos@up.edu.br', '$2a$11$mockhashpass123', '41999992222', 'RA202402', 'PASSENGER', NULL),
(3, 'Carlos Eduardo', 'carlos.eduardo@up.edu.br', '$2a$11$mockhashpass123', '41999993333', 'PF10089', 'BOTH', NULL);

-- 2. Veículos de teste
INSERT INTO vehicles (id, user_id, make, model, color, plate, seat_capacity)
VALUES 
(1, 1, 'Chevrolet', 'Onix', 'Prata', 'ABC1D23', 4),
(2, 3, 'Volkswagen', 'Polo', 'Preto', 'XYZ9K87', 4);

-- 3. Ofertas de boleia
INSERT INTO rides (id, driver_id, vehicle_id, origin_address, destination_address, departure_time, available_seats, price_contribution, status, observations)
VALUES 
(1, 1, 1, 'Terminal Portão - Curitiba', 'Universidade Positivo - Campus Ecoville', DATE_ADD(NOW(), INTERVAL 1 DAY), 3, 8.50, 'SCHEDULED', 'Saída pontual às 18:30.'),
(2, 3, 2, 'Praça 19 de Dezembro - Centro', 'Universidade Positivo - Campus Praça Osório', DATE_ADD(NOW(), INTERVAL 2 DAY), 2, 6.00, 'SCHEDULED', 'Espaço na mala para mochilas.');

-- 4. Pedido de reserva
INSERT INTO ride_requests (id, ride_id, passenger_id, pickup_address, status)
VALUES 
(1, 1, 2, 'Av. República Argentina, 2100', 'PENDING');