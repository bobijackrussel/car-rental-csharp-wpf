
-- ------------------------------------------------------------
-- Car Rental App (MySQL 8.0+) - Database Schema
-- Author: ChatGPT
-- Notes:
--   * Uses utf8mb4 and InnoDB.
--   * Dates for reservations are treated as [start_date, end_date) (end is exclusive).
--   * Overlapping reservations are blocked via triggers.
--   * Total amount auto-calculated from vehicle.daily_rate * DATEDIFF(end, start).
-- ------------------------------------------------------------

-- CREATE DATABASE IF NOT EXISTS car_rental DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- USE car_rental;

SET NAMES utf8mb4;
SET time_zone = '+00:00';

-- ------------------------------------------------------------
-- Branches (optional but useful if you expand to multiple locations)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS branches (
  id           BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name         VARCHAR(100) NOT NULL,
  address_line VARCHAR(200) NULL,
  city         VARCHAR(80)  NULL,
  state_region VARCHAR(80)  NULL,
  postal_code  VARCHAR(20)  NULL,
  country_code CHAR(2)      NULL,
  phone        VARCHAR(30)  NULL,
  created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- Users
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
  id             BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  username       VARCHAR(50) NOT NULL,
  email          VARCHAR(255) NULL,
  password_hash  VARCHAR(255) NOT NULL,
  full_name      VARCHAR(120) NULL,
  phone          VARCHAR(30)  NULL,
  theme          ENUM('LIGHT','DARK') NOT NULL DEFAULT 'LIGHT',
  language_code  VARCHAR(8) NOT NULL DEFAULT 'en',
  is_active      TINYINT(1) NOT NULL DEFAULT 1,
  created_at     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT uq_users_username UNIQUE (username),
  CONSTRAINT uq_users_email UNIQUE (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
-- Vehicles
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS vehicles (
  id            BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  branch_id     BIGINT UNSIGNED NULL,
  vin           VARCHAR(17) NULL,
  plate_number  VARCHAR(20) NOT NULL,
  make          VARCHAR(50) NOT NULL,
  model         VARCHAR(50) NOT NULL,
  model_year    SMALLINT    NOT NULL,
  category      ENUM('ECONOMY','COMPACT','MIDSIZE','SUV','LUXURY','VAN','TRUCK') NOT NULL,
  transmission  ENUM('MANUAL','AUTOMATIC') NOT NULL,
  fuel          ENUM('GASOLINE','DIESEL','HYBRID','ELECTRIC') NOT NULL,
  seats         TINYINT UNSIGNED NOT NULL,
  doors         TINYINT UNSIGNED NOT NULL,
  color         VARCHAR(30) NULL,
  description   TEXT NULL,
  daily_rate    DECIMAL(10,2) NOT NULL,
  status        ENUM('ACTIVE','MAINTENANCE','RETIRED') NOT NULL DEFAULT 'ACTIVE',
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT uq_vehicles_plate UNIQUE (plate_number),
  CONSTRAINT uq_vehicles_vin UNIQUE (vin),
  CONSTRAINT fk_vehicles_branch FOREIGN KEY (branch_id) REFERENCES branches(id)
    ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT chk_vehicles_daily_rate CHECK (daily_rate >= 0),
  CONSTRAINT chk_vehicles_year CHECK (model_year BETWEEN 1980 AND 2100)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX idx_vehicles_make_model ON vehicles(make, model);
CREATE INDEX idx_vehicles_category_rate ON vehicles(category, daily_rate);

-- ------------------------------------------------------------
-- Vehicle Photos
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS vehicle_photos (
  id          BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  vehicle_id  BIGINT UNSIGNED NOT NULL,
  photo_url   VARCHAR(500) NOT NULL,
  caption     VARCHAR(140) NULL,
  is_primary  TINYINT(1) NOT NULL DEFAULT 0,
  created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_vphotos_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(id)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX idx_vehicle_photos_vehicle ON vehicle_photos(vehicle_id, is_primary);

-- ------------------------------------------------------------
-- Reservations
--   * end_date is exclusive; a 1-day rental has end_date = start_date + INTERVAL 1 DAY
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS reservations (
  id                 BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id            BIGINT UNSIGNED NOT NULL,
  vehicle_id         BIGINT UNSIGNED NOT NULL,
  start_date         DATE NOT NULL,
  end_date           DATE NOT NULL,
  status             ENUM('PENDING','CONFIRMED','CANCELLED','COMPLETED') NOT NULL DEFAULT 'PENDING',
  total_amount       DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  notes              VARCHAR(500) NULL,
  created_at         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  cancelled_at       DATETIME NULL,
  cancellation_reason VARCHAR(255) NULL,
  CONSTRAINT fk_res_user FOREIGN KEY (user_id) REFERENCES users(id)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT fk_res_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(id)
    ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT chk_res_dates CHECK (end_date > start_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX idx_res_user ON reservations(user_id, start_date, end_date);
CREATE INDEX idx_res_vehicle_dates ON reservations(vehicle_id, start_date, end_date, status);

-- ------------------------------------------------------------
-- Feedback (about the overall rental firm; optionally tie to a reservation)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS feedbacks (
  id            BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id       BIGINT UNSIGNED NOT NULL,
  reservation_id BIGINT UNSIGNED NULL,
  rating        TINYINT UNSIGNED NOT NULL, -- 1..5
  comment       VARCHAR(1000) NULL,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_feedback_user FOREIGN KEY (user_id) REFERENCES users(id)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_feedback_reservation FOREIGN KEY (reservation_id) REFERENCES reservations(id)
    ON UPDATE CASCADE ON DELETE SET NULL,
  CONSTRAINT chk_feedback_rating CHECK (rating BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX idx_feedback_user ON feedbacks(user_id, created_at);

-- ------------------------------------------------------------
-- Violation Reports (tied to a reservation/appointment)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS violation_reports (
  id             BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  reservation_id BIGINT UNSIGNED NOT NULL,
  user_id        BIGINT UNSIGNED NOT NULL, -- who reported
  vtype          ENUM('LATE_RETURN','DAMAGE','CLEANLINESS','OTHER') NOT NULL DEFAULT 'OTHER',
  severity       ENUM('LOW','MEDIUM','HIGH','CRITICAL') NOT NULL DEFAULT 'LOW',
  description    VARCHAR(1500) NULL,
  status         ENUM('OPEN','UNDER_REVIEW','RESOLVED','DISMISSED') NOT NULL DEFAULT 'OPEN',
  reported_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  resolved_at    DATETIME NULL,
  resolution_notes VARCHAR(1000) NULL,
  CONSTRAINT fk_violation_reservation FOREIGN KEY (reservation_id) REFERENCES reservations(id)
    ON UPDATE CASCADE ON DELETE CASCADE,
  CONSTRAINT fk_violation_user FOREIGN KEY (user_id) REFERENCES users(id)
    ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX idx_violation_reservation ON violation_reports(reservation_id, status);
CREATE INDEX idx_violation_user ON violation_reports(user_id, status);

-- ------------------------------------------------------------
-- Triggers: Block overlaps & auto-calc total_amount
-- ------------------------------------------------------------
DELIMITER $$

CREATE TRIGGER trg_reservations_bi
BEFORE INSERT ON reservations
FOR EACH ROW
BEGIN
  DECLARE v_rate DECIMAL(10,2);
  DECLARE v_vehicle_status ENUM('ACTIVE','MAINTENANCE','RETIRED');
  DECLARE conflict_count INT DEFAULT 0;
  IF NEW.end_date <= NEW.start_date THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'end_date must be after start_date';
  END IF;

  -- Vehicle must exist and be ACTIVE
  SELECT daily_rate, status INTO v_rate, v_vehicle_status
  FROM vehicles WHERE id = NEW.vehicle_id
  FOR UPDATE;

  IF v_rate IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Vehicle not found';
  END IF;

  IF v_vehicle_status <> 'ACTIVE' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Vehicle is not available (not ACTIVE)';
  END IF;

  -- Check for overlapping reservations of the same vehicle
  SELECT COUNT(*) INTO conflict_count
  FROM reservations r
  WHERE r.vehicle_id = NEW.vehicle_id
    AND r.status IN ('PENDING','CONFIRMED')
    AND NEW.start_date < r.end_date
    AND NEW.end_date > r.start_date;

  IF conflict_count > 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Vehicle is already reserved for the selected dates';
  END IF;

  -- Auto-calc total amount
  SET NEW.total_amount = v_rate * DATEDIFF(NEW.end_date, NEW.start_date);
END$$

CREATE TRIGGER trg_reservations_bu
BEFORE UPDATE ON reservations
FOR EACH ROW
BEGIN
  DECLARE v_rate DECIMAL(10,2);
  DECLARE conflict_count INT DEFAULT 0;

  -- Prevent invalid date updates
  IF NEW.end_date <= NEW.start_date THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'end_date must be after start_date';
  END IF;

  -- If dates, vehicle, or status change, re-check overlap (only if status isn't CANCELLED)
  IF (NEW.vehicle_id <> OLD.vehicle_id OR NEW.start_date <> OLD.start_date OR NEW.end_date <> OLD.end_date OR NEW.status <> OLD.status)
     AND NEW.status IN ('PENDING','CONFIRMED') THEN
    SELECT COUNT(*) INTO conflict_count
    FROM reservations r
    WHERE r.vehicle_id = NEW.vehicle_id
      AND r.id <> OLD.id
      AND r.status IN ('PENDING','CONFIRMED')
      AND NEW.start_date < r.end_date
      AND NEW.end_date > r.start_date;

    IF conflict_count > 0 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Vehicle is already reserved for the selected dates';
    END IF;
  END IF;

  -- Recompute total if dates or vehicle changed
  IF (NEW.vehicle_id <> OLD.vehicle_id OR NEW.start_date <> OLD.start_date OR NEW.end_date <> OLD.end_date) THEN
    SELECT daily_rate INTO v_rate FROM vehicles WHERE id = NEW.vehicle_id;
    IF v_rate IS NULL THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Vehicle not found when updating reservation';
    END IF;
    SET NEW.total_amount = v_rate * DATEDIFF(NEW.end_date, NEW.start_date);
  END IF;
END$$

-- ------------------------------------------------------------
-- Stored Procedures (optional but handy for app use)
-- ------------------------------------------------------------
CREATE PROCEDURE sp_create_reservation (
  IN p_user_id BIGINT UNSIGNED,
  IN p_vehicle_id BIGINT UNSIGNED,
  IN p_start DATE,
  IN p_end DATE,
  OUT p_reservation_id BIGINT UNSIGNED
)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    SET p_reservation_id = NULL;
  END;
  START TRANSACTION;
    INSERT INTO reservations(user_id, vehicle_id, start_date, end_date, status)
    VALUES (p_user_id, p_vehicle_id, p_start, p_end, 'CONFIRMED');
    SET p_reservation_id = LAST_INSERT_ID();
  COMMIT;
END$$

CREATE PROCEDURE sp_cancel_reservation (
  IN p_reservation_id BIGINT UNSIGNED,
  IN p_user_id BIGINT UNSIGNED,
  IN p_reason VARCHAR(255)
)
BEGIN
  UPDATE reservations
     SET status = 'CANCELLED',
         cancelled_at = CURRENT_TIMESTAMP,
         cancellation_reason = p_reason
   WHERE id = p_reservation_id
     AND user_id = p_user_id
     AND status IN ('PENDING','CONFIRMED');
END$$

DELIMITER ;

-- ------------------------------------------------------------
-- Sample Data (optional)
-- ------------------------------------------------------------
INSERT INTO branches (name, city, country_code) VALUES
  ('Downtown Branch', 'Sarajevo', 'BA'),
  ('Airport Branch',  'Sarajevo', 'BA');

INSERT INTO users (username, email, password_hash, full_name)
VALUES
  ('alice', 'alice@example.com', '$2b$12$examplehashalice', 'Alice Doe'),
  ('bob',   'bob@example.com',   '$2b$12$examplehashbob',   'Bob Smith');

INSERT INTO vehicles (branch_id, vin, plate_number, make, model, model_year, category, transmission, fuel, seats, doors, color, daily_rate, status)
VALUES
  (1, 'WBA3A5C55FF000001', 'E63-AAA', 'BMW', '320d', 2018, 'MIDSIZE', 'AUTOMATIC', 'DIESEL', 5, 4, 'Black', 65.00, 'ACTIVE'),
  (2, 'WAUZZZ8V0G1000002', 'E63-BBB', 'Audi', 'A3',   2017, 'COMPACT', 'MANUAL',    'GASOLINE', 5, 4, 'White', 50.00, 'ACTIVE'),
  (1, 'VF1R9800523000003', 'E63-CCC', 'Renault', 'Clio', 2020, 'ECONOMY', 'MANUAL', 'GASOLINE', 5, 4, 'Blue', 35.00, 'MAINTENANCE');

-- Example reservation (1 day: 2025-11-01 to 2025-11-02)
INSERT INTO reservations (user_id, vehicle_id, start_date, end_date, status)
VALUES (1, 1, '2025-11-01', '2025-11-02', 'CONFIRMED');

-- Example feedback (company-wide, no reservation link)
INSERT INTO feedbacks (user_id, rating, comment)
VALUES (1, 5, 'Great service!');

-- Example violation (ties to existing reservation)
INSERT INTO violation_reports (reservation_id, user_id, vtype, severity, description)
VALUES (1, 1, 'OTHER', 'LOW', 'Test violation record');

-- ------------------------------------------------------------
-- Useful Queries
-- ------------------------------------------------------------
-- 1) Browse vehicles with primary photo (if any)
-- SELECT v.*, vp.photo_url AS primary_photo
-- FROM vehicles v
-- LEFT JOIN (
--   SELECT vehicle_id, photo_url FROM vehicle_photos WHERE is_primary = 1
-- ) vp ON vp.vehicle_id = v.id
-- WHERE v.status = 'ACTIVE'
-- ORDER BY v.daily_rate ASC;

-- 2) Check available vehicles for a date range
-- SET @start = DATE('2025-11-10');
-- SET @end   = DATE('2025-11-13');
-- SELECT v.*
-- FROM vehicles v
-- WHERE v.status = 'ACTIVE'
--   AND NOT EXISTS (
--     SELECT 1 FROM reservations r
--     WHERE r.vehicle_id = v.id
--       AND r.status IN ('PENDING','CONFIRMED')
--       AND @start < r.end_date AND @end > r.start_date
--   )
-- ORDER BY v.daily_rate;

-- 3) Get a user's reservations (for "Reservations" screen)
-- SELECT r.*, v.make, v.model, v.plate_number
-- FROM reservations r
-- JOIN vehicles v ON v.id = r.vehicle_id
-- WHERE r.user_id = ?
-- ORDER BY r.start_date DESC;

-- 4) Cancel a reservation you own
-- CALL sp_cancel_reservation( /* reservation_id */ 1, /* user_id */ 1, 'Changed plans' );

-- 5) Create a reservation safely (auto checks & total)
-- CALL sp_create_reservation( /* user_id */ 1, /* vehicle_id */ 2, '2025-11-05', '2025-11-08', @new_id );
-- SELECT @new_id AS new_reservation_id;
