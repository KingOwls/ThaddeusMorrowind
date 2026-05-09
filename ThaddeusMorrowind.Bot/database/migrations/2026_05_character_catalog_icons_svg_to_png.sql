-- ============================================================
-- Thaddeus Morrowind - Convertir iconos DiceBear SVG a PNG
--
-- Motivo:
-- Discord puede no mostrar SVG en thumbnails o embeds.
-- Si tus icon_url usan DiceBear con /svg?, este script los cambia a /png?.
-- ============================================================

USE thaddeus_morrowind_db;

START TRANSACTION;

UPDATE nations
SET icon_url = REPLACE(icon_url, '/svg?', '/png?')
WHERE icon_url LIKE '%api.dicebear.com%'
  AND icon_url LIKE '%/svg?%';

UPDATE roles
SET icon_url = REPLACE(icon_url, '/svg?', '/png?')
WHERE icon_url LIKE '%api.dicebear.com%'
  AND icon_url LIKE '%/svg?%';

UPDATE professions
SET icon_url = REPLACE(icon_url, '/svg?', '/png?')
WHERE icon_url LIKE '%api.dicebear.com%'
  AND icon_url LIKE '%/svg?%';

COMMIT;

SELECT 'nations' AS table_name, nation_key AS catalog_key, icon_url
FROM nations
WHERE icon_url LIKE '%api.dicebear.com%'
UNION ALL
SELECT 'roles', role_key, icon_url
FROM roles
WHERE icon_url LIKE '%api.dicebear.com%'
UNION ALL
SELECT 'professions', profession_key, icon_url
FROM professions
WHERE icon_url LIKE '%api.dicebear.com%';
