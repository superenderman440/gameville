using UnityEngine;

namespace Gameville
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float lookSensitivity = 2f;
        public Transform cameraTransform;
        public GameObject blockPrefab;
        public Transform worldRoot;
        public MultiplayerManager multiplayerManager;
        public bool isLocalPlayer = true;
        public int networkId = -1;
        public int skinIndex = 0;
        public float sendStateInterval = 0.1f;
        public float maxMana = 100f;
        public float manaRegenRate = 10f; // mana per second

        private CharacterController controller;
        private float pitch;
        private float sendTimer;
        private SkinManager skinController;
        private float currentMana;
        private AbilityTracker abilityTracker;
        private int[] abilityKeybinds = new int[6] { 1, 2, 3, 4, 5, 6 }; // ability IDs for Q, E, F, R, T, Y

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            skinController = GetComponent<SkinManager>();
            if (skinController == null)
            {
                skinController = gameObject.AddComponent<SkinManager>();
            }

            // Initialize ability system
            AbilityManager.Initialize();
            abilityTracker = new AbilityTracker();
            currentMana = maxMana;
        }

        private void Start()
        {
            if (skinController != null)
            {
                skinController.ApplySkin(skinIndex);
            }

            if (!isLocalPlayer && controller != null)
            {
                controller.enabled = false;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            UpdateLook();
            UpdateMove();
            UpdateSkinSelection();
            UpdatePlaceBlock();
            UpdateAbilities();
            UpdateMana();

            if (multiplayerManager != null)
            {
                sendTimer += Time.deltaTime;
                if (sendTimer >= sendStateInterval)
                {
                    sendTimer = 0f;
                    multiplayerManager.RequestSendPlayerState(this);
                }
            }
        }

        private void UpdateSkinSelection()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && skinController != null)
            {
                skinIndex = skinController.NextSkin();
                if (multiplayerManager != null)
                {
                    multiplayerManager.RequestSendPlayerState(this);
                }
            }
        }

        private void UpdateLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -85f, 85f);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void UpdateMove()
        {
            Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            Vector3 direction = transform.TransformDirection(input);
            controller.SimpleMove(direction * moveSpeed);
        }

        private void UpdatePlaceBlock()
        {
            if (Input.GetMouseButtonDown(0) && blockPrefab != null && worldRoot != null && cameraTransform != null)
            {
                Ray ray = cameraTransform.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 8f))
                {
                    Vector3 normal = hit.normal;
                    Vector3 placePosition = hit.point + normal * 0.5f;
                    placePosition = new Vector3(Mathf.Round(placePosition.x), Mathf.Round(placePosition.y), Mathf.Round(placePosition.z));

                    if (multiplayerManager != null)
                    {
                        multiplayerManager.RequestSpawnBlock(placePosition);
                    }
                    else
                    {
                        Instantiate(blockPrefab, placePosition, Quaternion.identity, worldRoot);
                    }
                }
            }
        }

        private void UpdateMana()
        {
            if (currentMana < maxMana)
            {
                currentMana += manaRegenRate * Time.deltaTime;
                currentMana = Mathf.Min(currentMana, maxMana);
            }

            abilityTracker.Update(Time.deltaTime);
        }

        private void UpdateAbilities()
        {
            // Q = Ability 1 (Dash)
            if (Input.GetKeyDown(KeyCode.Q))
                TryCastAbility(0);

            // E = Ability 2 (Fireball)
            if (Input.GetKeyDown(KeyCode.E))
                TryCastAbility(1);

            // F = Ability 3 (Makeshift Muffin)
            if (Input.GetKeyDown(KeyCode.F))
                TryCastAbility(2);

            // R = Ability 4 (Ambush)
            if (Input.GetKeyDown(KeyCode.R))
                TryCastAbility(3);

            // T = Ability 5 (Rage of Ao)
            if (Input.GetKeyDown(KeyCode.T))
                TryCastAbility(4);

            // Y = Ability 6 (Blade of Retribution)
            if (Input.GetKeyDown(KeyCode.Y))
                TryCastAbility(5);
        }

        private void TryCastAbility(int abilitySlot)
        {
            int abilityId = abilityKeybinds[abilitySlot];
            AbilityDefinition ability = AbilityManager.GetAbility(abilityId);

            if (ability == null)
            {
                Debug.LogWarning($"Ability {abilityId} not found");
                return;
            }

            // Check cooldown
            if (!abilityTracker.IsAbilityReady(abilityId))
            {
                Debug.Log($"{ability.name} is on cooldown for {abilityTracker.GetRemainingCooldown(abilityId):F1}s");
                return;
            }

            // Check mana
            if (currentMana < ability.cost)
            {
                Debug.Log($"Not enough mana for {ability.name}. Need {ability.cost}, have {currentMana:F0}");
                return;
            }

            // Cast ability
            CastAbility(ability);
            currentMana -= ability.cost;
            abilityTracker.UseAbility(abilityId, ability.cooldown);

            Debug.Log($"Cast {ability.name}! Remaining mana: {currentMana:F0}/{maxMana:F0}");
        }

        private void CastAbility(AbilityDefinition ability)
        {
            switch (ability.type)
            {
                case "damage":
                    HandleDamageAbility(ability);
                    break;
                case "heal":
                    HandleHealAbility(ability);
                    break;
                case "utility":
                    HandleUtilityAbility(ability);
                    break;
                case "buff":
                    HandleBuffAbility(ability);
                    break;
                default:
                    Debug.LogWarning($"Unknown ability type: {ability.type}");
                    break;
            }
        }

        private void HandleDamageAbility(AbilityDefinition ability)
        {
            // Raycast from camera to find target
            Ray ray = cameraTransform.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, ability.range))
            {
                // Check if we hit a mob
                Mob mob = hit.collider.GetComponent<Mob>();
                if (mob != null)
                {
                    mob.TakeDamage(ability.damage);
                    Debug.Log($"{ability.name} hit {mob.definition.name} for {ability.damage} damage!");
                }
                else
                {
                    Debug.Log($"{ability.name} cast but hit nothing valuable");
                }
            }
        }

        private void HandleHealAbility(AbilityDefinition ability)
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Heal((int)ability.healing);
                Debug.Log($"{ability.name} restored {ability.healing} health!");
            }
        }

        private void HandleUtilityAbility(AbilityDefinition ability)
        {
            // Dash: move player forward quickly
            Vector3 dashDirection = transform.forward;
            controller.Move(dashDirection * ability.range * 2f);
            Debug.Log($"{ability.name} dashed {ability.range * 2f} units forward!");
        }

        private void HandleBuffAbility(AbilityDefinition ability)
        {
            // Rage of Ao: temporary damage boost (implementation would go here)
            Debug.Log($"{ability.name} activated! Damage increased for {ability.duration}s");
            // TODO: Apply damage buff to this player
        }

        public void SetLocal(bool value)

        {
            isLocalPlayer = value;
            if (!value && controller != null)
            {
                controller.enabled = false;
            }
            else if (value)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void ApplyNetworkState(Vector3 position, Quaternion rotation, int newSkinIndex)
        {
            if (isLocalPlayer)
                return;

            transform.position = position;
            transform.rotation = rotation;

            if (newSkinIndex != skinIndex)
            {
                skinIndex = newSkinIndex;
                skinController?.ApplySkin(skinIndex);
            }
        }

        public void ApplySkinIndex(int index)
        {
            skinIndex = index;
            skinController?.ApplySkin(index);
        }

        public void SetMultiplayerManager(MultiplayerManager manager)
        {
            multiplayerManager = manager;
        }
    }
}
