-- ============================================================
-- Thaddeus Morrowind - Character Identity and Stats Fix V4
--
-- Ejecutar en MySQL Workbench o terminal:
-- mysql -u Arcane -p thaddeus_morrowind_db < database/migrations/2026_05_character_identity_stats_fix_v4.sql
-- ============================================================

USE thaddeus_morrowind_db;

START TRANSACTION;

-- ------------------------------------------------------------
-- A. Columnas para normalizar nombres y apodos
-- ------------------------------------------------------------

DROP PROCEDURE IF EXISTS add_column_if_not_exists;
DELIMITER $$

CREATE PROCEDURE add_column_if_not_exists(
    IN p_table_name VARCHAR(64),
    IN p_column_name VARCHAR(64),
    IN p_column_definition TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = p_table_name
          AND column_name = p_column_name
    ) THEN
        SET @sql = CONCAT('ALTER TABLE ', p_table_name, ' ADD COLUMN ', p_column_name, ' ', p_column_definition);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$

DELIMITER ;

CALL add_column_if_not_exists('characters', 'name_normalized', 'VARCHAR(120) NULL AFTER name');
CALL add_column_if_not_exists('characters', 'nickname_normalized', 'VARCHAR(120) NULL AFTER nickname');
CALL add_column_if_not_exists('stat_types', 'stat_category', 'VARCHAR(40) NOT NULL DEFAULT ''main'' AFTER value_kind');

DROP PROCEDURE IF EXISTS add_column_if_not_exists;

-- ------------------------------------------------------------
-- B. Renombrar duplicados actuales por usuario y estado
-- ------------------------------------------------------------

UPDATE characters
SET
    name_normalized = LOWER(TRIM(name)),
    nickname_normalized = CASE
        WHEN nickname IS NULL OR TRIM(nickname) = '' THEN NULL
        ELSE LOWER(TRIM(nickname))
    END;

DROP TEMPORARY TABLE IF EXISTS tmp_duplicate_character_names;

CREATE TEMPORARY TABLE tmp_duplicate_character_names AS
SELECT
    id,
    ROW_NUMBER() OVER (
        PARTITION BY user_account_id, character_status, LOWER(TRIM(name))
        ORDER BY id
    ) AS duplicate_number
FROM characters
WHERE name IS NOT NULL
  AND TRIM(name) <> '';

UPDATE characters
INNER JOIN tmp_duplicate_character_names
    ON tmp_duplicate_character_names.id = characters.id
SET characters.name = CONCAT(characters.name, ' #', characters.id)
WHERE tmp_duplicate_character_names.duplicate_number > 1;

DROP TEMPORARY TABLE IF EXISTS tmp_duplicate_character_names;

DROP TEMPORARY TABLE IF EXISTS tmp_duplicate_character_nicknames;

CREATE TEMPORARY TABLE tmp_duplicate_character_nicknames AS
SELECT
    id,
    ROW_NUMBER() OVER (
        PARTITION BY user_account_id, character_status, LOWER(TRIM(nickname))
        ORDER BY id
    ) AS duplicate_number
FROM characters
WHERE nickname IS NOT NULL
  AND TRIM(nickname) <> '';

UPDATE characters
INNER JOIN tmp_duplicate_character_nicknames
    ON tmp_duplicate_character_nicknames.id = characters.id
SET characters.nickname = CONCAT(characters.nickname, ' #', characters.id)
WHERE tmp_duplicate_character_nicknames.duplicate_number > 1;

DROP TEMPORARY TABLE IF EXISTS tmp_duplicate_character_nicknames;

UPDATE characters
SET
    name_normalized = LOWER(TRIM(name)),
    nickname_normalized = CASE
        WHEN nickname IS NULL OR TRIM(nickname) = '' THEN NULL
        ELSE LOWER(TRIM(nickname))
    END;

-- ------------------------------------------------------------
-- C. Índices únicos para evitar duplicados futuros
-- ------------------------------------------------------------

DROP PROCEDURE IF EXISTS add_unique_index_if_not_exists;
DELIMITER $$

CREATE PROCEDURE add_unique_index_if_not_exists(
    IN p_table_name VARCHAR(64),
    IN p_index_name VARCHAR(64),
    IN p_sql TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = DATABASE()
          AND table_name = p_table_name
          AND index_name = p_index_name
    ) THEN
        SET @sql = p_sql;
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$

DELIMITER ;

CALL add_unique_index_if_not_exists(
    'characters',
    'ux_characters_user_status_name_normalized',
    'CREATE UNIQUE INDEX ux_characters_user_status_name_normalized ON characters (user_account_id, character_status, name_normalized)'
);

CALL add_unique_index_if_not_exists(
    'characters',
    'ux_characters_user_status_nickname_normalized',
    'CREATE UNIQUE INDEX ux_characters_user_status_nickname_normalized ON characters (user_account_id, character_status, nickname_normalized)'
);

DROP PROCEDURE IF EXISTS add_unique_index_if_not_exists;

-- ------------------------------------------------------------
-- D. Stats oficiales y secundarias
-- ------------------------------------------------------------

UPDATE stat_types
SET stat_key = 'probabilidad',
    name = 'Probabilidad',
    value_kind = 'percent',
    default_base = 5,
    stat_category = 'main',
    can_be_base = TRUE,
    is_active = TRUE,
    display_order = 7
WHERE stat_key = 'probabilidad_critica';

UPDATE stat_types
SET stat_key = 'mana',
    name = 'Mana',
    value_kind = 'points',
    default_base = 50,
    stat_category = 'main',
    can_be_base = TRUE,
    is_active = TRUE,
    display_order = 9
WHERE stat_key IN ('mana_maximo', 'mana');

UPDATE stat_types
SET name = 'Daño crítico',
    value_kind = 'percent',
    default_base = 50,
    stat_category = 'main',
    can_be_base = TRUE,
    is_active = TRUE,
    display_order = 8
WHERE stat_key = 'danio_critico';

INSERT INTO stat_types (
    stat_key,
    name,
    value_kind,
    stat_category,
    default_base,
    min_value,
    max_value,
    can_be_base,
    is_active,
    display_order
)
VALUES
('vida', 'Vida', 'points', 'main', 100, 0, NULL, TRUE, TRUE, 1),
('ataque', 'Ataque', 'points', 'main', 10, 0, NULL, TRUE, TRUE, 2),
('poder_magico', 'Poder mágico', 'points', 'main', 5, 0, NULL, TRUE, TRUE, 3),
('armadura', 'Armadura', 'points', 'main', 20, 0, NULL, TRUE, TRUE, 4),
('resistencia_magica', 'Resistencia mágica', 'points', 'main', 20, 0, NULL, TRUE, TRUE, 5),
('velocidad', 'Velocidad', 'points', 'main', 55, 0, NULL, TRUE, TRUE, 6),
('probabilidad', 'Probabilidad', 'percent', 'main', 5, 0, 100, TRUE, TRUE, 7),
('danio_critico', 'Daño crítico', 'percent', 'main', 50, 0, NULL, TRUE, TRUE, 8),
('mana', 'Mana', 'points', 'main', 50, 0, NULL, TRUE, TRUE, 9)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    value_kind = VALUES(value_kind),
    stat_category = VALUES(stat_category),
    default_base = VALUES(default_base),
    min_value = VALUES(min_value),
    max_value = VALUES(max_value),
    can_be_base = VALUES(can_be_base),
    is_active = VALUES(is_active),
    display_order = VALUES(display_order);

INSERT INTO stat_types (
    stat_key,
    name,
    value_kind,
    stat_category,
    default_base,
    min_value,
    max_value,
    can_be_base,
    is_active,
    display_order
)
VALUES
('evasion', 'Evasión', 'percent', 'secondary', 0, 0, 100, TRUE, TRUE, 101),
('suerte', 'Suerte', 'percent', 'secondary', 0, 0, 100, TRUE, TRUE, 102),
('inmortalidad', 'Inmortalidad', 'percent', 'secondary', 0, 0, 100, TRUE, TRUE, 103),
('bloqueo', 'Bloqueo', 'percent', 'secondary', 0, 0, 100, TRUE, TRUE, 104),
('aumento_danio_infligido', 'Aumento de daño infligido', 'percent', 'secondary', 0, NULL, NULL, TRUE, TRUE, 105),
('reduccion_danio_recibido', 'Reducción de daño recibido', 'percent', 'secondary', 0, NULL, NULL, TRUE, TRUE, 106),
('bono_protectivo', 'Bono protectivo', 'percent', 'secondary', 0, NULL, NULL, TRUE, TRUE, 107),
('agradecimiento', 'Agradecimiento', 'percent', 'secondary', 0, NULL, NULL, TRUE, TRUE, 108),
('omnivampirismo', 'Omnivampirismo', 'percent', 'secondary', 0, 0, 100, TRUE, TRUE, 109)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    value_kind = VALUES(value_kind),
    stat_category = VALUES(stat_category),
    default_base = VALUES(default_base),
    min_value = VALUES(min_value),
    max_value = VALUES(max_value),
    can_be_base = VALUES(can_be_base),
    is_active = VALUES(is_active),
    display_order = VALUES(display_order);

UPDATE stat_types
SET is_active = FALSE,
    stat_category = 'legacy',
    display_order = 999
WHERE stat_key IN (
    'aura',
    'recarga_recurso',
    'recarga_de_recurso',
    'recurso_mana',
    'mana_maximo'
);

DELETE FROM stat_growth_rules
WHERE source_type = 'base'
  AND source_key = 'global';

INSERT INTO stat_growth_rules (
    source_type,
    source_key,
    stat_type_id,
    growth_per_level,
    flat_bonus,
    description,
    is_active,
    display_order
)
SELECT 'base', 'global', id, 4.0000, 0, 'Crecimiento base por nivel para Vida.', TRUE, 1
FROM stat_types WHERE stat_key = 'vida'
UNION ALL
SELECT 'base', 'global', id, 0.6500, 0, 'Crecimiento base por nivel para Ataque.', TRUE, 2
FROM stat_types WHERE stat_key = 'ataque'
UNION ALL
SELECT 'base', 'global', id, 0.7000, 0, 'Crecimiento base por nivel para Poder mágico.', TRUE, 3
FROM stat_types WHERE stat_key = 'poder_magico'
UNION ALL
SELECT 'base', 'global', id, 0.4500, 0, 'Crecimiento base por nivel para Armadura.', TRUE, 4
FROM stat_types WHERE stat_key = 'armadura'
UNION ALL
SELECT 'base', 'global', id, 0.4500, 0, 'Crecimiento base por nivel para Resistencia mágica.', TRUE, 5
FROM stat_types WHERE stat_key = 'resistencia_magica'
UNION ALL
SELECT 'base', 'global', id, 0.1000, 0, 'Crecimiento base por nivel para Velocidad.', TRUE, 6
FROM stat_types WHERE stat_key = 'velocidad'
UNION ALL
SELECT 'base', 'global', id, 0.0500, 0, 'Crecimiento base por nivel para Probabilidad.', TRUE, 7
FROM stat_types WHERE stat_key = 'probabilidad'
UNION ALL
SELECT 'base', 'global', id, 0.2500, 0, 'Crecimiento base por nivel para Daño crítico.', TRUE, 8
FROM stat_types WHERE stat_key = 'danio_critico'
UNION ALL
SELECT 'base', 'global', id, 1.5000, 0, 'Crecimiento base por nivel para Mana.', TRUE, 9
FROM stat_types WHERE stat_key = 'mana';

INSERT INTO character_stats (
    character_id,
    stat_type_id,
    base_value,
    extra_value
)
SELECT
    characters.id,
    stat_types.id,
    stat_types.default_base,
    0
FROM characters
CROSS JOIN stat_types
LEFT JOIN character_stats
    ON character_stats.character_id = characters.id
   AND character_stats.stat_type_id = stat_types.id
WHERE stat_types.is_active = TRUE
  AND character_stats.id IS NULL;

UPDATE character_stats
INNER JOIN characters
    ON characters.id = character_stats.character_id
INNER JOIN stat_types
    ON stat_types.id = character_stats.stat_type_id
INNER JOIN nations
    ON nations.id = characters.nation_id
INNER JOIN roles
    ON roles.id = characters.role_id
INNER JOIN professions
    ON professions.id = characters.profession_id
SET character_stats.base_value = ROUND(
        stat_types.default_base
        + ((characters.level - 1) * COALESCE((
            SELECT SUM(stat_growth_rules.growth_per_level)
            FROM stat_growth_rules
            WHERE stat_growth_rules.stat_type_id = stat_types.id
              AND stat_growth_rules.is_active = TRUE
              AND (
                    (stat_growth_rules.source_type = 'base' AND stat_growth_rules.source_key = 'global')
                 OR (stat_growth_rules.source_type = 'nation' AND stat_growth_rules.source_key = nations.nation_key)
                 OR (stat_growth_rules.source_type = 'role' AND stat_growth_rules.source_key = roles.role_key)
                 OR (stat_growth_rules.source_type = 'profession' AND stat_growth_rules.source_key = professions.profession_key)
              )
        ), 0))
        + COALESCE((
            SELECT SUM(stat_growth_rules.flat_bonus)
            FROM stat_growth_rules
            WHERE stat_growth_rules.stat_type_id = stat_types.id
              AND stat_growth_rules.is_active = TRUE
              AND (
                    (stat_growth_rules.source_type = 'base' AND stat_growth_rules.source_key = 'global')
                 OR (stat_growth_rules.source_type = 'nation' AND stat_growth_rules.source_key = nations.nation_key)
                 OR (stat_growth_rules.source_type = 'role' AND stat_growth_rules.source_key = roles.role_key)
                 OR (stat_growth_rules.source_type = 'profession' AND stat_growth_rules.source_key = professions.profession_key)
              )
        ), 0),
        2
    ),
    character_stats.updated_at = CURRENT_TIMESTAMP
WHERE stat_types.is_active = TRUE;

COMMIT;

SELECT
    user_account_id,
    character_status,
    name_normalized,
    COUNT(*) AS total
FROM characters
GROUP BY user_account_id, character_status, name_normalized
HAVING COUNT(*) > 1;

SELECT
    user_account_id,
    character_status,
    nickname_normalized,
    COUNT(*) AS total
FROM characters
WHERE nickname_normalized IS NOT NULL
GROUP BY user_account_id, character_status, nickname_normalized
HAVING COUNT(*) > 1;

SELECT
    stat_key,
    name,
    stat_category,
    value_kind,
    default_base,
    display_order,
    is_active
FROM stat_types
ORDER BY
    CASE stat_category
        WHEN 'main' THEN 1
        WHEN 'secondary' THEN 2
        ELSE 3
    END,
    display_order;

SELECT
    stat_types.stat_key,
    stat_types.name,
    stat_types.default_base AS nivel_1,
    ROUND(stat_types.default_base + 99 * COALESCE(SUM(stat_growth_rules.growth_per_level), 0), 2) AS nivel_100_base_global
FROM stat_types
LEFT JOIN stat_growth_rules
    ON stat_growth_rules.stat_type_id = stat_types.id
   AND stat_growth_rules.source_type = 'base'
   AND stat_growth_rules.source_key = 'global'
   AND stat_growth_rules.is_active = TRUE
WHERE stat_types.stat_category = 'main'
  AND stat_types.is_active = TRUE
GROUP BY stat_types.id, stat_types.stat_key, stat_types.name, stat_types.default_base, stat_types.display_order
ORDER BY stat_types.display_order;
