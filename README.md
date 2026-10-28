# 👾 Space Invaders (Unity 2D)

Videojuego estilo arcade inspirado en *Space Invaders*, hecho en **Unity** con C#. Controlas una nave que se desplaza por la parte baja de la pantalla, dispara a una formación de invasores que avanza y desciende, y se protege detrás de barreras destructibles.

> Este repositorio contiene la carpeta `Assets/` del proyecto (scripts, escenas, prefabs, audio y texturas).

---

## 🎮 Cómo se juega

| Acción | Tecla |
| --- | --- |
| Mover a la izquierda | `A` / `←` |
| Mover a la derecha | `D` / `→` |
| Disparar (mantener presionado) | `Espacio` |
| Pausar / reanudar | `Esc` |

**Reglas**

- Empiezas con **3 vidas**. Al recibir un disparo enemigo pierdes una vida y tienes ~1.5 s de invulnerabilidad (la nave parpadea).
- La formación de **24 invasores** (3 filas × 8 columnas) se mueve de lado a lado y **desciende** cada vez que toca un borde.
- Mientras menos invasores queden y más veces descienda la formación, **más rápido se mueve**.
- Solo disparan los invasores de la **fila inferior de cada columna**.
- Las **3 barreras** absorben disparos (12 impactos cada una) y se van dañando visualmente.
- **Puntaje:** cada invasor da 10, 20 o 30 puntos según su fila.

**Condiciones de fin**

- 🏆 **Victoria:** destruyes a todos los invasores.
- 💀 **Derrota:** te quedas sin vidas, o los invasores llegan a la línea de defensa.

---

## 🧰 Requisitos

- **Unity 6** (el código usa `Rigidbody2D.linearVelocity`, disponible desde Unity 6).
- Paquetes de Unity:
  - **Input System** (`UnityEngine.InputSystem`)
  - **TextMeshPro**
  - **Universal Render Pipeline (URP) 2D**

---

## 🚀 Cómo ejecutar el proyecto

1. Crea un proyecto nuevo en Unity Hub con la plantilla **Universal 2D** (Unity 6).
2. Copia el contenido de este repositorio dentro de la carpeta `Assets/` del proyecto (o reemplaza la carpeta `Assets/`).
3. Verifica que el **Input System** esté activo (*Edit → Project Settings → Player → Active Input Handling*: `Input System Package (New)` o `Both`).
4. Abre la escena `Assets/Scenes/MainMenu.unity` y presiona **Play**.

Las escenas del juego (ya incluidas en el repo) son:

| Escena | Descripción |
| --- | --- |
| `MainMenu` | Menú principal |
| `Game` | Partida |
| `Victory` | Pantalla de victoria con puntaje final |
| `GameOver` | Pantalla de derrota con puntaje final |

> Si las escenas aparecen vacías o con referencias rotas, puedes regenerarlas desde el menú del editor: **Tools → Generar Space Invaders**.

---

## 🛠️ Herramientas del editor

El proyecto incluye utilidades en el menú **Tools** de Unity:

- **Tools → Generar Space Invaders:** crea capas, sprites, prefabs, escenas y configura el *Build Settings* automáticamente.
- **Tools → Space Invaders → Validar escenas y referencias:** revisa que las escenas tengan sus referencias y objetos necesarios (por ejemplo, las 3 barreras).
- **Tools → Space Invaders → Compilar Windows x86-64:** genera un build para Windows.

Los sonidos `.wav` de `Assets/Audio/` se generan con `ArcadeAudioGenerator.cs`.

---

## 📁 Estructura del proyecto

````
Assets/
├── Audio/            # Música de fondo y efectos (disparo, explosión, daño, victoria, derrota)
├── Barrier/        # Fuente arcade, material y sprites generados (barreras, píxel)
├── Prefabs/          # Player, Invader1-3, PlayerShot, EnemyShot, Barrier, Explosion
├── Scenes/           # MainMenu, Game, Victory, GameOver
├── Scripts/
│   ├── Editor/       # Generadores y herramientas del editor
│   ├── Enemies/      # EnemyManager, EnemyController, Barrier
│   ├── Managers/     # GameManager, ScoreManager, AudioManager, SmokeTestRunner
│   ├── Player/       # PlayerController, PlayerHealth
│   ├── Projectiles/  # Projectile, ExplosionEffect
│   └── UI/           # UIManager, MainMenuController, AspectCamera
├── Settings/         # Configuración de URP 2D e Input System
├── TextMesh Pro/     # Recursos de TextMeshPro
└── Textures/         # Sprites (naves, enemigos, láseres, power-ups, UI, fondos)
````

---

## 🧠 Resumen técnico

- **`GameManager`**: controla el estado de la partida (pausa, fin de juego) y carga las escenas de victoria/derrota.
- **`EnemyManager`**: mueve la formación, acelera según los invasores restantes, hace descender la fila al tocar los bordes y elige qué invasor dispara.
- **`PlayerController` / `PlayerHealth`**: movimiento con límites horizontales, disparo con *cooldown*, vidas, invulnerabilidad y retroceso al recibir daño.
- **`Projectile`**: usa *casting* del collider para no atravesar objetos a alta velocidad; el disparo enemigo parte lento y acelera por gravedad.
- **`Barrier`**: 12 puntos de vida con 3 etapas de daño visual.
- **`AudioManager` / `ScoreManager` / `UIManager`**: sonido (con *throttling* de efectos), puntaje y HUD (puntaje, vidas, invasores restantes).
- **`AspectCamera`**: mantiene visible todo el campo de juego 16:9 en ventanas más angostas.
- Todo el código está bajo el *namespace* `SpaceInvaders`.

---

## 🙌 Créditos

- Sprites, fuentes (`kenvector_future`) y efectos de sonido incluidos en `Textures/`: pack **Space Shooter Redux** de [Kenney](https://kenney.nl) (licencia CC0).
- Fuente **Liberation Sans** (licencia OFL) incluida con TextMeshPro.

---

## 📄 Licencia

Agrega aquí la licencia de tu proyecto (por ejemplo, MIT).
````
````

Nota: en el bloque de "Estructura del proyecto" hay un bloque de código anidado; si al pegarlo en GitHub se ve mal, asegúrate de pegar solo el contenido interior (sin la línea ` ````markdown ` inicial ni la final).
