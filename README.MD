# Thaddeus Morrowind

**Thaddeus Morrowind** es un bot de Discord desarrollado en **C#/.NET** como base funcional para un MMORPG de escala pequeña. El proyecto busca construir, paso a paso, un sistema jugable dentro de Discord con usuarios, personajes, estadísticas, habilidades, dungeons, batallas por turnos, recompensas y progresión.

El enfoque actual del proyecto es crear primero una base sólida: arquitectura limpia, base de datos normalizada, comandos de Discord claros y una separación correcta entre lo que vive en SQL, lo que vive temporalmente en JSON y lo que debe ejecutar C#.

---

## Estado actual del proyecto

Actualmente el proyecto ya cuenta con:

- Estructura base del bot en C#.
- Conexión a Discord.
- Soporte para comandos Slash `/`.
- Soporte para comandos por prefijo `!`.
- Base de datos MySQL reconstruida desde cero para el MVP.
- Módulo inicial de usuarios funcional.
- Perfil público de usuario.
- Registro de usuario.
- Billetera básica del usuario.
- Sincronización de nombre e imagen desde Discord.
- Diseño base para personajes, estadísticas, habilidades, batallas y dungeons.

---

## Tecnologías usadas

| Área | Herramienta |
| -- | -- |
| Lenguaje principal | C# |
| Runtime | .NET |
| Bot de Discord | Discord.Net |
| Base de datos | MySQL |
| Acceso a datos | MySqlConnector / EF Core como contexto base |
| Editor principal | Visual Studio Code |
| Sistema operativo de desarrollo | Linux |
| Terminal / scripts | Bash `.sh` |
| Control de versiones | Git |
| Contenedores previstos | Docker / Docker Compose |

---

## Filosofía técnica del proyecto

La regla central del proyecto es:

```txt
SQL guarda lo estable.
JSON guarda lo vivo o generado.
C# calcula y ejecuta.
```

### SQL

La base de datos almacena datos persistentes y estructurados:

- Usuarios.
- Personajes.
- Billeteras.
- Catálogos de naciones, roles y profesiones.
- Estadísticas base.
- Reglas de crecimiento.
- Habilidades.
- Palabras clave.
- Estados de batalla.
- Plantillas de enemigos.
- Plantillas de dungeons.
- Misiones.
- Logs importantes.

### JSON

Los JSON se usan para estados temporales, generados o variables:

- Estado vivo de una batalla.
- Cola de prioridad.
- Vida y maná actual durante combate.
- Buffs y debuffs temporales.
- Cooldowns.
- Estado generado de una dungeon.
- Salas visitadas.
- Puertas abiertas.
- Eventos ocultos descubiertos.

### `C#`

C# se encarga de ejecutar la lógica:

- Registrar usuarios.
- Validar comandos.
- Calcular estadísticas.
- Resolver batallas.
- Aplicar efectos de habilidades.
- Generar dungeons.
- Entregar recompensas.
- Actualizar progreso.
- Formatear respuestas visuales para Discord.

---

## Estructura general del proyecto

```txt
ThaddeusMorrowind.Bot/
│
├── Common/
│   ├── Constants/
│   ├── Enums/
│   ├── Helpers/
│   └── Results/
│
├── Content/
│   ├── artifacts/
│   ├── dungeons/
│   ├── enemies/
│   ├── items/
│   ├── nations/
│   ├── quests/
│   └── skills/
│
├── Data/
│   ├── Configurations/
│   ├── Entities/
│   ├── Migrations/
│   └── GameDbContext.cs
│
├── Discord/
│   ├── Components/
│   ├── Handlers/
│   ├── InteractionHandlers/
│   ├── Modals/
│   ├── PrefixCommands/
│   ├── SlashCommands/
│   └── Views/
│
├── Features/
│   ├── Artifacts/
│   ├── Characters/
│   ├── Combat/
│   ├── Dungeons/
│   ├── Economy/
│   ├── Guilds/
│   ├── Inventory/
│   ├── Minigames/
│   ├── Pets/
│   ├── Quests/
│   ├── Skills/
│   ├── Stats/
│   └── Users/
│
├── appsettings.json
├── Program.cs
├── BotRuntime.cs
├── Worker.cs
└── ThaddeusMorrowind.Bot.csproj
```

---

## Módulo de usuarios

El módulo de usuarios es la primera base funcional del MVP.

### Objetivo

Permitir que una cuenta de Discord tenga un perfil dentro del bot, una billetera inicial y una identidad sincronizada con Discord.

### Tablas principales relacionadas

| Tabla | Propósito |
| --- | --- |
| `user_accounts` | Guarda el registro principal del usuario de Discord. |
| `user_wallets` | Guarda oro y moneda premium. |
| `user_activity_logs` | Guarda actividad básica del usuario. |
| `user_active_characters` | Guarda el personaje activo del usuario cuando exista. |

### Datos principales del usuario

- ID interno.
- ID de Discord.
- Username.
- Nombre público.
- Avatar de Discord.
- Estado de cuenta.
- Límite de roster de personajes.
- Fecha de registro.
- Última interacción.
- Oro.
- Moneda premium.
- Personaje activo.

---

## Comandos implementados actualmente

### Slash commands

| Comando | Estado | Descripción | Visibilidad |
| --- | ---: | --- | --- |
| `/registro` | Implementado | Registra al usuario o actualiza sus datos base desde Discord. | Privado |
| `/perfil` | Implementado | Muestra el perfil público del usuario. Si se menciona otro usuario, muestra el perfil de esa persona. | Público |
| `/perfil_actualizar` | Implementado | Sincroniza nombre, nickname e imagen del usuario desde Discord. | Privado |
| `/wallet` | Implementado | Muestra la billetera del usuario. | Privado |

### Uso de `/perfil`

```txt
/perfil
```

Muestra el perfil del usuario que ejecuta el comando.

```txt
/perfil usuario:@Usuario
```

Muestra el perfil público del usuario mencionado.

### Prefix commands

| Comando | Estado | Descripción |
| --- | ---: | --- |
| `!registro` | Implementado | Registra al usuario o actualiza sus datos base. |
| `!perfil` | Implementado | Muestra el perfil del usuario que ejecuta el comando. |
| `!perfil @Usuario` | Implementado | Muestra el perfil público del usuario mencionado. |
| `!perfil actualizar` | Implementado | Actualiza la identidad del usuario desde Discord. |
| `!wallet` | Implementado | Muestra la billetera del usuario. |

---

## Interfaz actual de perfil

El perfil muestra información básica del aventurero:

```txt
📜 Perfil de Aventurero

Usuario: Cat Wazza
Estado: Activa
Oro: 0
Moneda premium: 0
Personajes registrados: 0/20
Personaje activo: Ninguno
Registrado: fecha
Última interacción: fecha
```

Cuando el usuario consulta su propio perfil, se muestran botones:

```txt
[Ver personajes] [Crear personaje] [Actualizar perfil] [Ayuda]
```

Cuando se consulta el perfil de otro usuario, el perfil se muestra sin botones de gestión.

---

## Base de datos MVP

La base de datos fue rediseñada para evitar mezclar conceptos y reducir errores de versiones anteriores.

### Principios aplicados

- Usuario y personaje son entidades diferentes.
- El usuario puede tener muchos personajes registrados.
- El límite de 4 aplica para party de dungeon, no necesariamente para el total de personajes.
- El personaje activo es solo el personaje por defecto para comandos rápidos.
- Las estadísticas permanentes viven en SQL.
- Los cambios temporales de batalla viven en JSON.
- Las habilidades deben tener efectos ejecutables, no solo texto.
- Las dungeons fijas viven como plantillas.
- Las dungeons generadas viven como snapshots temporales.

### Núcleos principales de la base

| Módulo | Tablas principales |
| --- | --- |
| Usuarios | `user_accounts`, `user_wallets`, `user_activity_logs` |
| Personajes | `characters`, `user_active_characters`, `character_experience_logs` |
| Estadísticas | `stat_types`, `character_stats`, `stat_growth_rules`, `character_stat_modifiers` |
| Habilidades | `skill_templates`, `skill_keywords`, `skill_template_keywords`, `skill_effects` |
| Árbol de habilidades | `skill_slot_unlocks`, `character_skill_unlocks`, `character_skill_loadout_slots` |
| Batallas | `battle_sessions`, `battle_action_logs`, `status_effect_templates` |
| Dungeons | `dungeon_templates`, `dungeon_room_templates`, `dungeon_runs`, `dungeon_run_logs` |
| Misiones | `quest_templates`, `quest_objectives`, `character_quests` |
| Recompensas | `reward_tables`, `reward_table_entries` |

---

## Árbol de habilidades definido

El árbol oficial del MVP queda con 20 espacios:

```txt
8 habilidades activas
6 habilidades pasivas
4 habilidades de exploración / rol
2 habilidades de estadísticas complementarias
```

Distribución por nivel:

| Slot | Categoría | Nivel |
| ---: | --- | ---: |
| 1 | Activa | 1 |
| 2 | Activa | 5 |
| 3 | Pasiva | 10 |
| 4 | Exploración | 15 |
| 5 | Activa | 20 |
| 6 | Pasiva | 25 |
| 7 | Estadística | 30 |
| 8 | Activa | 35 |
| 9 | Exploración | 40 |
| 10 | Pasiva | 45 |
| 11 | Activa | 50 |
| 12 | Pasiva | 55 |
| 13 | Estadística | 60 |
| 14 | Exploración | 65 |
| 15 | Activa | 70 |
| 16 | Pasiva | 75 |
| 17 | Exploración | 80 |
| 18 | Activa | 85 |
| 19 | Pasiva | 90 |
| 20 | Activa | 95 |

---

## Diseño de batalla

Las batallas se plantean como sistemas por turnos.

### Acciones previstas

- Atacar.
- Defender.
- Usar habilidad.
- Usar item.
- Usar equipable.
- Huir.

### Prioridad de acciones

La prioridad se calcula en C# usando una fórmula conceptual:

```txt
prioridad =
velocidad efectiva
+ prioridad de acción
+ prioridad de habilidad
+ modificadores temporales
+ desempate
```

### Regla de cambios temporales

Los cambios temporales como buffs, debuffs o alteraciones de velocidad no modifican directamente `character_stats`.

En cambio, se guardan en el JSON vivo de la batalla:

```txt
battle_sessions.snapshot_json
```

Al terminar la batalla, el snapshot puede limpiarse y los eventos importantes quedan en:

```txt
battle_action_logs
```

---

## Diseño de dungeons

Las dungeons usan una mezcla de SQL y JSON.

### Plantillas

Las dungeons fijas o de misión viven en SQL:

```txt
dungeon_templates
dungeon_room_templates
dungeon_room_connections
```

### Ejecuciones

Cada intento o run vive temporalmente en:

```txt
dungeon_runs.snapshot_json
```

Ahí se guarda:

- Sala actual.
- Salas visitadas.
- Puertas abiertas.
- Eventos activados.
- Enemigos pendientes.
- Recompensas generadas.
- Flags temporales.

### Política de limpieza

- Dungeons aleatorias, semanales o de evento pueden limpiar su snapshot al terminar.
- Dungeons de misión fija conservan plantilla y progreso de misión.
- Los logs importantes quedan en SQL.

---

## Versionamiento de base de datos

La recomendación del proyecto es manejar la base en scripts separados:

```txt
database/
  schema/
    001_initial_schema.sql

  seeds/
    001_core_catalogs.sql
    002_basic_skills.sql
    003_basic_enemies.sql

  migrations/
    2026_05_06_001_add_dungeon_system.sql
    2026_05_06_002_add_battle_rewards.sql

  json_templates/
    battle_snapshot_template.json
    dungeon_run_snapshot_template.json
```

### Qué debe ir a Git

- Scripts de estructura.
- Seeds de catálogos.
- Migraciones.
- Plantillas JSON.
- Documentación.

### Qué no debe ir a Git

- Backups completos con usuarios reales.
- Tokens.
- Contraseñas.
- `.env`.
- Dumps con datos personales.

---

## Instalación y ejecución local

### Requisitos

- .NET instalado.
- MySQL instalado y corriendo.
- Bot de Discord configurado en el portal de Discord.
- Token del bot guardado de forma segura.
- Base de datos `thaddeus_morrowind_db` creada.

### Restaurar paquetes

```bash
dotnet restore
```

### Compilar

```bash
dotnet build
```

### Ejecutar

```bash
dotnet run
```

---

## Configuración de base de datos

La conexión debe apuntar a MySQL.

Ejemplo conceptual:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=thaddeus_morrowind_db;user=Arcane;password=TU_PASSWORD;"
  }
}
```

Para desarrollo se recomienda usar `user-secrets` o variables de entorno para no subir credenciales al repositorio.

---

## Estado por módulos

| Módulo | Estado |
| --- | ---: |
| Conexión Discord | Funcional |
| Slash commands | Funcional |
| Prefix commands | Funcional |
| Base de datos MVP | Creada |
| Usuarios | Base funcional |
| Perfil público | Funcional |
| Wallet básica | Funcional |
| Personajes | Diseñado, pendiente de reconstrucción sobre nueva base |
| Estadísticas | Diseñado, pendiente de integración limpia |
| Habilidades | Diseñado, pendiente de integración limpia |
| Batalla | Diseñado, pendiente de implementación |
| Dungeons | Diseñado, pendiente de implementación |
| Inventario | Pendiente |
| Artefactos | Pendiente |
| Mascotas | Pendiente |
| Misiones | Pendiente |
| Minijuegos | Pendiente |

---

## Próximas fases recomendadas

### Fase 1: Usuarios

- Registro.
- Perfil.
- Wallet.
- Sincronización con Discord.

Estado: **en progreso funcional**.

### Fase 2: Personajes

- Crear personaje.
- Listar personajes.
- Seleccionar personaje activo.
- Ver ficha de personaje.
- Editar personaje.
- Eliminar o archivar personaje.
- Validar roster de personajes.

### Fase 3: Estadísticas y experiencia

- Curva de experiencia.
- Recalcular nivel.
- Recalcular estadísticas base.
- Separar base, extra y total.

### Fase 4: Habilidades

- Catálogo de habilidades.
- Aprender habilidad.
- Equipar habilidad.
- Validar slots del árbol.
- Definir efectos ejecutables.

### Fase 5: Batalla MVP

- 1 personaje contra 1 enemigo.
- Acciones básicas.
- Prioridad.
- Daño.
- Curación.
- Estados simples.
- Logs.

### Fase 6: Dungeon MVP

- Dungeon de prueba.
- Salas.
- Puertas.
- Evento simple.
- Batalla.
- Recompensa final.

---

## Notas de desarrollo

Este proyecto se trabaja de forma incremental. Cada módulo debe probarse antes de pasar al siguiente.

La prioridad actual es evitar parches caóticos y mantener una arquitectura clara:

```txt
Primero modelo.
Luego SQL.
Luego C#.
Luego Discord UI.
Luego pruebas.
```

---

## Autoría y propósito

Este proyecto nace como una exploración académica y técnica para construir un MMORPG ligero dentro de Discord, usando C#, MySQL y una arquitectura modular.

La meta no es crear todo de golpe, sino formar una base sólida y escalable para ir agregando sistemas sin romper lo anterior.

---

## Nombre del bot

```txt
Thaddeus Morrowind
```

Un archivo de aventureros, dungeons y gloria cuestionablemente legal.
