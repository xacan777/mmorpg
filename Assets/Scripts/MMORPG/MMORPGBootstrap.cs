using UnityEngine;

namespace MiniMMORPG
{
    public class MMORPGBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildGame()
        {
            if (FindObjectOfType<GameSession>() != null)
            {
                return;
            }

            var sessionObject = new GameObject("MMORPGSession");
            var session = sessionObject.AddComponent<GameSession>();
            session.Initialize();
        }
    }

    public class GameSession : MonoBehaviour
    {
        public PlayerCharacter Player { get; private set; }

        private EnemySpawner _spawner;
        private MMORPGHud _hud;

        public void Initialize()
        {
            SetupWorld();
            SpawnPlayer();
            SpawnCamera();
            SpawnLight();

            _spawner = gameObject.AddComponent<EnemySpawner>();
            _spawner.Initialize(Player.transform);

            _hud = gameObject.AddComponent<MMORPGHud>();
            _hud.Initialize(this);
        }

        private void SetupWorld()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            var renderer = ground.GetComponent<Renderer>();
            renderer.material.color = new Color(0.25f, 0.45f, 0.25f);
        }

        private void SpawnPlayer()
        {
            var playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player";
            playerObj.transform.position = new Vector3(0f, 1f, 0f);
            playerObj.GetComponent<Renderer>().material.color = new Color(0.3f, 0.55f, 0.95f);

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

        public void AddKillReward(int xp, int gold)
        {
            Player.AddXp(xp);
            Player.AddGold(gold);
            _hud.ShowMessage($"Убийство! +{xp} XP, +{gold} золота");
        }

        public void ShowLootMessage(string text)
        {
            _hud.ShowMessage(text);
        }
    }
}
