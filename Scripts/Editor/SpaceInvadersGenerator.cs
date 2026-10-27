using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpaceInvaders.Editor
{
    public static class SpaceInvadersGenerator
    {
        const string Generated = "Assets/Generated";
        static Material spriteMaterial;
        static TMP_FontAsset font;
        static GameObject ship, enemyShot, playerShot, barrier, explosion;
        static readonly GameObject[] Enemies = new GameObject[3];
        static readonly string[] SceneNames = { "MainMenu", "Game", "Victory", "GameOver" };
        static readonly Color Cyan = new Color(0.3f, 0.9f, 1);

        [MenuItem("Tools/Generar Space Invaders")]
        public static void Generate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { Generated, "Assets/Prefabs", "Assets/Scenes", "Assets/Audio" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            Configure();
            CreateArt();
            ArcadeAudioGenerator.Generate();
            CreatePrefabs();
            foreach (string scene in SceneNames) CreateScene(scene);
            EditorBuildSettings.scenes = SceneNames.Select(n => new EditorBuildSettingsScene($"Assets/Scenes/{n}.unity", true)).ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Validate();
            Debug.Log("Space Invaders generado: cuatro escenas, prefabs, URP 2D y audio completos.");
        }

        static void Configure()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                string resources = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).First();
                UnityEditor.AssetPackage.Package.Import(resources, false);
                AssetDatabase.Refresh();
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            if (pipeline == null) throw new InvalidOperationException("No se encontró el pipeline URP inspeccionado.");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(currentQuality, false);
            pipeline.msaaSampleCount = 1;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            string[] names = { "Ship", "Invader", "PlayerShot", "EnemyShot", "Barrier" };
            foreach (string name in names)
            {
                if (LayerMask.NameToLayer(name) >= 0) continue;
                for (int i = 8; i < 32; i++)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    { layers.GetArrayElementAtIndex(i).stringValue = name; break; }
                tags.ApplyModifiedProperties();
            }
            foreach (string a in names)
                for (int b = 0; b < 32; b++) Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(a), b, true);
            var sorting = tags.FindProperty("m_SortingLayers");
            string[] sortingNames = { "Space Background", "Arcade Actors", "Arcade Effects" };
            for (int i = 0; i < sortingNames.Length; i++)
            {
                bool exists = false;
                for (int j = 0; j < sorting.arraySize; j++)
                    if (sorting.GetArrayElementAtIndex(j).FindPropertyRelative("name").stringValue == sortingNames[i]) exists = true;
                if (exists) continue;
                sorting.InsertArrayElementAtIndex(sorting.arraySize);
                var item = sorting.GetArrayElementAtIndex(sorting.arraySize - 1);
                item.FindPropertyRelative("name").stringValue = sortingNames[i];
                item.FindPropertyRelative("uniqueID").intValue = 231700 + i;
            }
            tags.ApplyModifiedProperties();
            Allow("PlayerShot", "Invader"); Allow("PlayerShot", "Barrier");
            Allow("EnemyShot", "Ship"); Allow("EnemyShot", "Barrier");
            Time.fixedDeltaTime = 1f / 60;
            PlayerSettings.companyName = "INACAP"; PlayerSettings.productName = "Space Invaders";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{Generated}/ArcadeSprite.mat");
            if (spriteMaterial == null)
            {
                spriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(spriteMaterial, $"{Generated}/ArcadeSprite.mat");
            }
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{Generated}/ArcadeFont.asset");
            if (font == null)
            {
                // Fuente arcade proporcionada; respaldo TMP para signos y acentos ausentes.
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Textures/Bonus/kenvector_future.ttf");
                font = TMP_FontAsset.CreateFontAsset(source);
                AssetDatabase.CreateAsset(font, $"{Generated}/ArcadeFont.asset");
                font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ¡!¿?áéíóúÁÉÍÓÚñÑ:.-/→", out string missing);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                EditorUtility.SetDirty(font);
                if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Caracteres no presentes en la fuente: " + missing);
            }
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> {
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") };
            EditorUtility.SetDirty(font);
        }
        static void Allow(string a, string b) => Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(a), LayerMask.NameToLayer(b), false);
        static Sprite Sprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void CreateArt()
        {
            for (int stage = 0; stage < 3; stage++)
            {
                var texture = new Texture2D(120, 56, TextureFormat.RGBA32, false);
                for (int y = 0; y < 56; y++) for (int x = 0; x < 120; x++)
                {
                    bool solid = !(y > 43 && (x < 12 || x > 107)) && !(y < 19 && x > 43 && x < 76);
                    bool crack = stage > 0 && Math.Abs(x - (42 + (y / 5 % 2) * 5)) < 3;
                    crack |= stage > 1 && Math.Abs(x - (87 - y / 4)) < 4;
                    var color = y > 49 || x < 5 || x > 114 ? new Color(0.65f, 1, 1) : new Color(0.2f, 0.66f, 0.7f);
                    texture.SetPixel(x, y, solid && !crack ? color : Color.clear);
                }
                texture.Apply(); File.WriteAllBytes($"{Generated}/Barrier{stage}.png", texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            var pixel = new Texture2D(1, 1); pixel.SetPixel(0, 0, Color.white); pixel.Apply();
            File.WriteAllBytes($"{Generated}/Pixel.png", pixel.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(pixel);
            AssetDatabase.Refresh();
        }
        static GameObject Visual(string name, Sprite sprite, Vector3 position, float scale = 1, int order = 0)
        {
            var go = new GameObject(name); go.transform.position = position; go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = spriteMaterial; renderer.sortingOrder = order;
            renderer.sortingLayerName = order < 0 ? "Space Background" : order >= 10 ? "Arcade Effects" : "Arcade Actors";
            return go;
        }
        static void PhysicsBody(GameObject go, string layer, RigidbodyType2D type, Vector2 size, bool trigger = false)
        {
            go.layer = LayerMask.NameToLayer(layer);
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = type; body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = go.AddComponent<BoxCollider2D>(); collider.size = size; collider.isTrigger = trigger;
        }
        static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Number(UnityEngine.Object target, string name, int value)
        {
            var so = new SerializedObject(target); so.FindProperty(name).intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        static GameObject SavePrefab(GameObject go, string name)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/{name}.prefab"); UnityEngine.Object.DestroyImmediate(go); return saved;
        }
        static void CreatePrefabs()
        {
            var go = Visual("Explosion", Sprite("Assets/Textures/PNG/Effects/star3.png"), Vector3.zero, 2, 20);
            go.AddComponent<ExplosionEffect>(); explosion = SavePrefab(go, "Explosion");
            for (int i = 0; i < 2; i++)
            {
                bool player = i == 0;
                go = Visual(player ? "PlayerShot" : "EnemyShot", Sprite($"Assets/Textures/PNG/Lasers/laser{(player ? "Blue" : "Red")}01.png"), Vector3.zero, 0.75f, 10);
                PhysicsBody(go, player ? "PlayerShot" : "EnemyShot", RigidbodyType2D.Dynamic, new Vector2(0.09f, 0.48f), true);
                var projectile = go.AddComponent<Projectile>();
                var so = new SerializedObject(projectile); so.FindProperty("fromPlayer").boolValue = player;
                so.FindProperty("speed").floatValue = player ? 14 : 5; so.ApplyModifiedPropertiesWithoutUndo();
                var prefab = SavePrefab(go, go.name); if (player) playerShot = prefab; else enemyShot = prefab;
            }
            go = Visual("Player", Sprite("Assets/Textures/PNG/playerShip1_blue.png"), Vector3.zero, 0.85f, 5);
            PhysicsBody(go, "Ship", RigidbodyType2D.Dynamic, new Vector2(0.78f, 0.58f));
            go.GetComponent<Rigidbody2D>().constraints |= RigidbodyConstraints2D.FreezePositionY;
            Set(go.AddComponent<PlayerController>(), "projectile", playerShot.GetComponent<Projectile>());
            go.AddComponent<PlayerHealth>(); ship = SavePrefab(go, "Player");
            string[] art = { "enemyGreen1", "enemyBlue2", "enemyRed3" };
            for (int row = 0; row < 3; row++)
            {
                go = Visual("Invader" + (row + 1), Sprite($"Assets/Textures/PNG/Enemies/{art[row]}.png"), Vector3.zero, 0.75f, 5);
                PhysicsBody(go, "Invader", RigidbodyType2D.Kinematic, new Vector2(0.88f, 0.7f));
                var enemy = go.AddComponent<EnemyController>(); Number(enemy, "points", (row + 1) * 10);
                Set(enemy, "explosion", explosion.GetComponent<ExplosionEffect>());
                Enemies[row] = SavePrefab(go, go.name);
            }
            var stages = Enumerable.Range(0, 3).Select(i => Sprite($"{Generated}/Barrier{i}.png")).ToArray();
            go = Visual("Barrier", stages[0], Vector3.zero, 1.6f, 4);
            go.layer = LayerMask.NameToLayer("Barrier");
            var left = go.AddComponent<BoxCollider2D>(); left.size = new Vector2(0.44f, 0.55f); left.offset = new Vector2(-0.38f, 0);
            var right = go.AddComponent<BoxCollider2D>(); right.size = left.size; right.offset = new Vector2(0.38f, 0);
            var top = go.AddComponent<BoxCollider2D>(); top.size = new Vector2(0.34f, 0.36f); top.offset = new Vector2(0, 0.1f);
            var barrierScript = go.AddComponent<Barrier>(); var serialized = new SerializedObject(barrierScript);
            var array = serialized.FindProperty("damageStages"); array.arraySize = 3;
            for (int i = 0; i < 3; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); barrier = SavePrefab(go, "Barrier");
        }
        static GameObject Instance(GameObject prefab, Vector3 position, Transform parent = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.position = position; go.transform.SetParent(parent); return go;
        }
        static void CreateScene(string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Main Camera"); cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.055f); camera.clearFlags = CameraClearFlags.SolidColor;
            cameraGo.transform.position = new Vector3(0, 0, -10); cameraGo.AddComponent<AudioListener>(); cameraGo.AddComponent<AspectCamera>();
            camera.GetUniversalAdditionalCameraData().SetRenderer(0);
            Sprite background = Sprite("Assets/Textures/Backgrounds/darkPurple.png");
            var backdrop = new GameObject("Space background • tiled original texture");
            for (int x = -4; x <= 4; x++) for (int y = -2; y <= 2; y++)
                Visual("Stars", background, new Vector3(x * 2.56f, y * 2.56f), 1, -100).transform.SetParent(backdrop.transform);
            var pixel = Sprite($"{Generated}/Pixel.png");
            foreach (float x in new[] { -8.55f, 8.55f })
            {
                var line = Visual("Playfield edge", pixel, new Vector3(x, 0), 1, -5);
                line.transform.localScale = new Vector3(3, 850, 1); line.GetComponent<SpriteRenderer>().color = new Color(0.15f, 0.5f, 0.6f, 0.5f);
            }
            CreateAudio();
            var canvasGo = new GameObject("Arcade Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var menu = new GameObject("Scene Navigation").AddComponent<MainMenuController>();
            Transform canvas = canvasGo.transform;
            if (name == "Game") CreateLevel(canvas, menu);
            else
            {
                Text(canvas, "INACAP  /  ORBITAL DEFENSE", 23, new Vector2(0, 420), new Vector2(1400, 60), Cyan);
                bool main = name == "MainMenu", win = name == "Victory";
                Text(canvas, main ? "SPACE INVADERS" : win ? "¡VICTORIA!" : "GAME OVER", 94, new Vector2(0, 230), new Vector2(1700, 150), win ? Cyan : Color.white);
                Text(canvas, main ? "Defiende la Tierra de la invasión extraterrestre" : win ? "¡Has derrotado a todos los invasores!" : "Los invasores han ganado.", 30, new Vector2(0, 115), new Vector2(1700, 90), Color.white);
                if (!main) Set(menu, "finalScore", Text(canvas, "PUNTAJE FINAL", 36, new Vector2(0, 10), new Vector2(1500, 70), Cyan));
                Button(canvas, main ? "JUGAR" : win ? "VOLVER A JUGAR" : "REINTENTAR", new Vector2(0, -130), menu.Play);
                Button(canvas, main ? "SALIR" : "MENÚ PRINCIPAL", new Vector2(0, -245), main ? (UnityAction)menu.Quit : menu.Menu);
                Text(canvas, "A / D  o  FLECHAS: MOVER     ESPACIO: DISPARAR     ESC: PAUSA", 23, new Vector2(0, -420), new Vector2(1750, 70), Cyan);
                for (int i = 0; i < 3; i++)
                    Visual("Invader display", Enemies[i].GetComponent<SpriteRenderer>().sprite, new Vector3(-2 + i * 2, 0.15f), 0.6f, 0);
                if (!main)
                    foreach (var display in scene.GetRootGameObjects().Where(g => g.name == "Invader display")) UnityEngine.Object.DestroyImmediate(display);
            }
            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{name}.unity");
        }
        static void CreateLevel(Transform canvas, MainMenuController menu)
        {
            new GameObject("GameManager").AddComponent<GameManager>(); new GameObject("ScoreManager").AddComponent<ScoreManager>();
            var player = Instance(ship, new Vector3(0, -4));
            var formation = new GameObject("Enemy Formation • 3 x 8").AddComponent<EnemyManager>();
            Set(formation, "projectile", enemyShot.GetComponent<Projectile>());
            for (int row = 0; row < 3; row++) for (int col = 0; col < 8; col++)
                Instance(Enemies[row], new Vector3(-5.25f + col * 1.5f, 1.25f + row * 0.95f), formation.transform);
            foreach (float x in new[] { -5f, 0f, 5f }) Instance(barrier, new Vector3(x, -2.35f));
            var line = Visual("Defense line", Sprite($"{Generated}/Pixel.png"), new Vector3(0, -2.85f), 1, -5);
            line.transform.localScale = new Vector3(1650, 2, 1); line.GetComponent<SpriteRenderer>().color = new Color(0.2f, 0.8f, 0.9f, 0.4f);
            var hud = Panel(canvas, "HUD", new Vector2(0, 470), new Vector2(1830, 105), new Color(0.01f, 0.025f, 0.05f, 0.94f));
            var ui = canvas.gameObject.AddComponent<UIManager>(); Set(ui, "player", player.GetComponent<PlayerHealth>()); Set(ui, "formation", formation);
            Set(ui, "score", Text(hud, "PUNTAJE", 30, new Vector2(-570, 0), new Vector2(580, 70), Cyan));
            Set(ui, "lives", Text(hud, "VIDAS", 30, Vector2.zero, new Vector2(450, 70), Color.white));
            Set(ui, "remaining", Text(hud, "INVASORES", 30, new Vector2(570, 0), new Vector2(580, 70), Cyan));
            Text(canvas, "A / D: MOVER     ESPACIO: DISPARAR     ESC: PAUSA", 20, new Vector2(0, -500), new Vector2(1700, 45), Cyan);
            var pause = Panel(canvas, "Pause", Vector2.zero, new Vector2(1920, 1080), new Color(0.01f, 0.02f, 0.05f, 0.96f));
            Text(pause, "PAUSA", 85, new Vector2(0, 200), new Vector2(1200, 140), Cyan);
            Button(pause, "REANUDAR", new Vector2(0, 0), ui.Resume);
            Button(pause, "MENÚ PRINCIPAL", new Vector2(0, -130), menu.Menu);
            Set(ui, "pausePanel", pause.gameObject); pause.gameObject.SetActive(false);
        }
        static void CreateAudio()
        {
            var go = new GameObject("AudioManager"); var manager = go.AddComponent<AudioManager>();
            var music = go.AddComponent<AudioSource>(); music.playOnAwake = false; music.spatialBlend = 0;
            var effects = go.AddComponent<AudioSource>(); effects.playOnAwake = false; effects.spatialBlend = 0;
            Set(manager, "music", music); Set(manager, "effects", effects);
            foreach (string field in new[] { "bgm", "shot", "explosion", "damage", "victory", "defeat" })
            {
                string file = field == "bgm" ? "BGM" : char.ToUpperInvariant(field[0]) + field.Substring(1);
                Set(manager, field, AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{file}.wav"));
            }
        }
        static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(parent, name, position, size); rect.gameObject.AddComponent<Image>().color = color; return rect;
        }
        static TMP_Text Text(Transform parent, string content, int size, Vector2 position, Vector2 bounds, Color color)
        {
            var rect = Rect(parent, content, position, bounds); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = content; text.fontSize = size; text.color = color; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap; return text;
        }
        static void Button(Transform parent, string title, Vector2 position, UnityAction action)
        {
            var rect = Panel(parent, title, position, new Vector2(600, 86), new Color(0.04f, 0.23f, 0.3f));
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(0.45f, 1, 1); colors.pressedColor = new Color(0.2f, 0.65f, 0.8f); button.colors = colors;
            Text(rect, title, 32, Vector2.zero, new Vector2(580, 78), Color.white);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }
        [MenuItem("Tools/Space Invaders/Validar escenas y referencias")]
        public static void Validate()
        {
            int sceneCount = 0;
            foreach (string name in SceneNames)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity");
                foreach (var root in scene.GetRootGameObjects()) foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0) throw new Exception("Script faltante: " + transform.name);
                    foreach (var component in transform.GetComponents<MonoBehaviour>())
                    {
                        if (component == null || component.GetType().Namespace != "SpaceInvaders") continue;
                        var iterator = new SerializedObject(component).GetIterator();
                        while (iterator.NextVisible(true))
                            if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null
                                && !(component is MainMenuController && iterator.name == "finalScore"))
                                throw new Exception($"Referencia vacía: {name}/{component.name}/{iterator.propertyPath}");
                    }
                }
                if (name == "Game")
                {
                    if (UnityEngine.Object.FindObjectsByType<EnemyController>().Length != 24) throw new Exception("La formación no tiene 24 enemigos.");
                    if (UnityEngine.Object.FindObjectsByType<Barrier>().Length != 3) throw new Exception("Faltan barreras.");
                }
                sceneCount++;
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/SpaceInvadersValidation.txt", $"{DateTime.Now:u}\n{sceneCount} escenas verificadas; 24 enemigos; 3 barreras; sin scripts ni referencias de juego faltantes.\n");
        }
        [MenuItem("Tools/Space Invaders/Compilar Windows x86-64")]
        public static void BuildWindows()
        {
            Validate(); Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = SceneNames.Select(n => $"Assets/Scenes/{n}.unity").ToArray(),
                target = BuildTarget.StandaloneWindows64, locationPathName = "Builds/Windows/Space Invaders.exe", options = BuildOptions.None });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("La build Windows falló: " + report.summary.result);
            Debug.Log("BUILD WINDOWS OK: " + report.summary.totalSize + " bytes");
        }
        public static void GenerateAndBuild() { Generate(); BuildWindows(); }
    }
}
