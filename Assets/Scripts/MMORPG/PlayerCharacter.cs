using UnityEngine;

namespace MiniMMORPG
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCharacter : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int Gold { get; private set; }
        public int Potions { get; private set; }

        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; } = 100f;

        private CharacterController _controller;
        private GameSession _session;
        private float _attackCooldown;

        private const float Speed = 6.5f;
        private const float Gravity = -20f;
        private const float AttackRange = 2.25f;

        private float _verticalVelocity;

        public void Initialize(GameSession session)
        {
            _session = session;
            _controller = GetComponent<CharacterController>();
            _controller.height = 2f;
            _controller.radius = 0.45f;
            _controller.center = new Vector3(0f, 1f, 0f);
        }

        private void Update()
        {
            HandleMovement();
            HandleCombat();
            HandleConsumables();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            var inputDirection = new Vector3(horizontal, 0f, vertical);
            if (inputDirection.sqrMagnitude > 1f)
            {
                inputDirection.Normalize();
            }

            var camForward = Camera.main.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            var camRight = Camera.main.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            var move = (camForward * inputDirection.z + camRight * inputDirection.x) * Speed;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            move.y = _verticalVelocity;
            _controller.Move(move * Time.deltaTime);

            if (inputDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(move.x, 0f, move.z)), Time.deltaTime * 12f);
            }
        }

        private void HandleCombat()
        {
            _attackCooldown -= Time.deltaTime;
            if (!Input.GetMouseButtonDown(0) || _attackCooldown > 0f)
            {
                return;
            }

            _attackCooldown = 0.5f;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 100f))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyCharacter>();
                if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= AttackRange)
                {
                    enemy.TakeDamage(20f);
                }
            }
        }

        private void HandleConsumables()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && Potions > 0 && Health < MaxHealth)
            {
                Potions--;
                Health = Mathf.Min(MaxHealth, Health + 45f);
                _session.ShowLootMessage("Использовано зелье: +45 HP");
            }
        }

        public void ReceiveDamage(float damage)
        {
            Health -= damage;
            if (Health <= 0f)
            {
                Health = MaxHealth;
                transform.position = new Vector3(0f, 1f, 0f);
                _session.ShowLootMessage("Вы погибли и возродились в лагере");
            }
        }

        public void AddXp(int xp)
        {
            Xp += xp;
            int need = Level * 100;
            while (Xp >= need)
            {
                Xp -= need;
                Level++;
                MaxHealth += 20f;
                Health = MaxHealth;
                need = Level * 100;
                _session.ShowLootMessage($"Новый уровень: {Level}");
            }
        }

        public void AddGold(int amount) => Gold += amount;

        public void AddPotion(int amount) => Potions += amount;

        public float XpProgress01()
        {
            return Mathf.Clamp01(Xp / (float)(Level * 100));
        }
    }
}
