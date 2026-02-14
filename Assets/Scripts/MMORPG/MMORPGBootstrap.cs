using UnityEngine;

namespace MiniMMORPG
{
    public class MMORPGBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildGame()
        {
            var session = FindObjectOfType<GameSession>();
            if (session == null)
            {
                var sessionObject = new GameObject("MMORPGSession");
                session = sessionObject.AddComponent<GameSession>();
            }

            session.InitializeIfNeeded();
        }
    }

    public class GameSession : MonoBehaviour
    {
        public PlayerCharacter Player { get; private set; }

        private EnemySpawner _spawner;
        private MMORPGHud _hud;
        private bool _isInitialized;
        private Vector3 _spawnPosition = new Vector3(0f, 1f, 0f);

        public void InitializeIfNeeded()
        {
            if (_isInitialized && Player != null)
            {
                return;
            }

            RemoveLegacyFirstPersonController();
            _spawnPosition = ResolveSpawnPosition();
            SetupWorld();
            SpawnPlayer();
            SpawnCamera();
            SpawnLight();

            _spawner = GetComponent<EnemySpawner>() ?? gameObject.AddComponent<EnemySpawner>();
            _spawner.Initialize(Player.transform);

            _hud = GetComponent<MMORPGHud>() ?? gameObject.AddComponent<MMORPGHud>();
            _hud.Initialize(this);

            _isInitialized = true;
        }

        public Vector3 GetSpawnPosition() => _spawnPosition;

        private static void RemoveLegacyFirstPersonController()
        {
            var legacy = GameObject.Find("First Person Controller");
            if (legacy != null)
            {
                legacy.SetActive(false);
            }
        }

        private Vector3 ResolveSpawnPosition()
        {
            var marker = GameObject.Find("SpawnPoint");
            if (marker != null)
            {
                return marker.transform.position;
            }

            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                var pos = terrain.transform.position;
                float y = terrain.SampleHeight(pos) + 1f;
                return new Vector3(pos.x, y, pos.z);
            }

            return new Vector3(0f, 1f, 0f);
        }

        private void SetupWorld()
        {
            if (Terrain.activeTerrain != null || GameObject.Find("Ground") != null)
            {
                return;
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            RuntimeVisuals.ApplyColor(ground.GetComponent<Renderer>(), new Color(0.25f, 0.45f, 0.25f));
        }

        private void SpawnPlayer()
        {
            if (Player != null)
            {
                return;
            }

            var existing = FindObjectOfType<PlayerCharacter>();
            if (existing != null)
            {
                Player = existing;
                Player.Initialize(this);
                return;
            }

            var playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player";
            playerObj.transform.position = _spawnPosition;
            RuntimeVisuals.ApplyColor(playerObj.GetComponent<Renderer>(), new Color(0.3f, 0.55f, 0.95f));

            Destroy(playerObj.GetComponent<CapsuleCollider>());
            playerObj.AddComponent<CharacterController>();

            Player = playerObj.AddComponent<PlayerCharacter>();
            Player.Initialize(this);
        }

        private void SpawnCamera()
        {
            var cameraObj = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.GetComponent<Camera>() ?? cameraObj.AddComponent<Camera>();
            camera.fieldOfView = 68f;

            if (cameraObj.GetComponent<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
            }

            var follow = cameraObj.GetComponent<CameraFollow>() ?? cameraObj.AddComponent<CameraFollow>();
            follow.Initialize(Player.transform);
        }

        private void SpawnLight()
        {
            var light = FindObjectOfType<Light>();
            if (light == null)
            {
                var lightObj = new GameObject("Directional Light");
                light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.ambientLight = new Color(0.52f, 0.52f, 0.58f);
        }

        public void AddKillReward(int xp)
        {
            Player.AddXp(xp);
            _hud.ShowMessage($"Убийство! +{xp} XP");
        }

        public void ShowLootMessage(string text)
        {
            _hud.ShowMessage(text);
        }
    }
}
