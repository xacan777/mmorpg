using System.Collections;
using UnityEngine;

namespace MiniMMORPG
{
    public class EnemySpawner : MonoBehaviour
    {
        private Transform _player;

        private const int InitialEnemies = 1;
        private const float RespawnDelay = 5f;
        private const float SpawnAreaHalfSize = 18f;

        public void Initialize(Transform player)
        {
            _player = player;
            for (int i = 0; i < InitialEnemies; i++)
            {
                SpawnEnemy();
            }
        }

        public void ScheduleRespawn()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(RespawnDelay);
            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            var pos = new Vector3(Random.Range(-SpawnAreaHalfSize, SpawnAreaHalfSize), 1f, Random.Range(-SpawnAreaHalfSize, SpawnAreaHalfSize));

            var enemyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyObj.name = "Monster";
            enemyObj.transform.position = pos;
            enemyObj.transform.localScale = new Vector3(1.4f, 1.8f, 1.4f);
            RuntimeVisuals.ApplyColor(enemyObj.GetComponent<Renderer>(), new Color(0.8f, 0.23f, 0.23f));

            var rb = enemyObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.mass = 100f;

            var enemy = enemyObj.AddComponent<EnemyCharacter>();
            enemy.Initialize(_player, this);
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    public class EnemyCharacter : MonoBehaviour
    {
        public float MaxHealth { get; private set; }
        public float Health { get; private set; }
        public bool IsAlive => Health > 0f;

        private Transform _player;
        private Rigidbody _rb;
        private EnemySpawner _spawner;

        private float _attackCooldown;

        public void Initialize(Transform player, EnemySpawner spawner)
        {
            _player = player;
            _spawner = spawner;
            _rb = GetComponent<Rigidbody>();
            MaxHealth = 15f;
            Health = 15f;
        }

        private void FixedUpdate()
        {
            if (_player == null || !IsAlive)
            {
                return;
            }

            var toPlayer = _player.position - transform.position;
            float distance = toPlayer.magnitude;

            if (distance > 1.7f)
            {
                var velocity = toPlayer.normalized * 2.6f;
                velocity.y = _rb.velocity.y;
                _rb.velocity = velocity;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(velocity.x, 0f, velocity.z)), Time.fixedDeltaTime * 8f);
            }
            else
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                _attackCooldown -= Time.fixedDeltaTime;
                if (_attackCooldown <= 0f)
                {
                    _attackCooldown = 1f;
                    var player = _player.GetComponent<PlayerCharacter>();
                    player.ReceiveDamage(Random.Range(1f, 5.1f));
                }
            }
        }

        private void OnGUI()
        {
            if (!IsAlive || Camera.main == null)
            {
                return;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
            if (screen.z < 0f)
            {
                return;
            }

            float x = screen.x - 40f;
            float y = Screen.height - screen.y;

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x, y, 80f, 10f), Texture2D.whiteTexture);
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(x + 1f, y + 1f, Mathf.Clamp(Health / MaxHealth, 0f, 1f) * 78f, 8f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x - 2f, y - 17f, 100f, 18f), $"HP {Health:0}/{MaxHealth:0}");
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive)
            {
                return;
            }

            Health -= damage;
            if (Health <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            var session = FindObjectOfType<GameSession>();
            int xp = Random.Range(35, 55);
            session.AddKillReward(xp);

            int gold = Random.Range(3, 12);
            int potionAmount = 1;

            LootPickup.Spawn(transform.position + new Vector3(0.4f, 0.25f, 0f), LootType.Gold, gold);
            LootPickup.Spawn(transform.position + new Vector3(-0.4f, 0.25f, 0f), LootType.Potion, potionAmount);

            _spawner.ScheduleRespawn();
            Destroy(gameObject);
        }
    }

    public enum LootType
    {
        Gold,
        Potion
    }

    public class LootPickup : MonoBehaviour
    {
        private LootType _type;
        private int _amount;

        public static void Spawn(Vector3 position, LootType type, int amount)
        {
            var lootObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lootObj.name = $"Loot_{type}";
            lootObj.transform.position = position;
            lootObj.transform.localScale = Vector3.one * 0.45f;

            var col = lootObj.GetComponent<SphereCollider>();
            col.isTrigger = true;

            RuntimeVisuals.ApplyColor(lootObj.GetComponent<Renderer>(), type == LootType.Gold ? new Color(1f, 0.84f, 0.1f) : new Color(0.3f, 0.95f, 0.95f));

            var pickup = lootObj.AddComponent<LootPickup>();
            pickup._type = type;
            pickup._amount = amount;
            Destroy(lootObj, 15f);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, 80f * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerCharacter>();
            if (player == null)
            {
                return;
            }

            var session = FindObjectOfType<GameSession>();
            if (_type == LootType.Gold)
            {
                player.AddGold(_amount);
                session.ShowLootMessage($"Подобрано золото: +{_amount}");
            }
            else
            {
                player.AddPotion(_amount);
                session.ShowLootMessage($"Подобрано зелье x{_amount}");
            }

            Destroy(gameObject);
        }
    }
}
