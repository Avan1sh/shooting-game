// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]
        [SerializeField] private AudioClip audioClipWalking;
        [SerializeField] private AudioClip audioClipRunning;
        [SerializeField] private AudioClip audioClipJump;

        [Header("Speeds")]
        [SerializeField] private float speedWalking = 5.0f;
        [SerializeField] private float speedRunning = 9.0f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 5.0f;
        [SerializeField] private float jumpCooldown = 0.2f;

        #endregion

        #region PROPERTIES

        private Vector3 Velocity
        {
            get => rigidBody.linearVelocity;
            set => rigidBody.linearVelocity = value;
        }

        #endregion

        #region FIELDS

        private Rigidbody rigidBody;
        private CapsuleCollider capsule;
        private AudioSource audioSource;
        private bool grounded;
        private bool canJump = true;

        private CharacterBehaviour playerCharacter;
        private WeaponBehaviour equippedWeapon;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        #endregion

        #region UNITY FUNCTIONS

        protected override void Awake()
        {
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        protected override void Start()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            capsule = GetComponent<CapsuleCollider>();
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;
        }

        private void OnCollisionStay()
        {
            Bounds bounds = capsule.bounds;
            float radius = bounds.extents.x - 0.01f;
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down, groundHits, bounds.extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
            grounded = groundHits.Any(hit => hit.collider != null && hit.collider != capsule);
        }

        protected override void FixedUpdate()
        {
            MoveCharacter();
            grounded = false;
        }

        protected override void Update()
        {
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();
            PlayFootstepSounds();
            if (playerCharacter.GetInputJump() && grounded && canJump)
                Jump();
        }

        #endregion

        #region METHODS

        private void MoveCharacter()
        {
            Vector2 frameInput = playerCharacter.GetInputMovement();
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);
            movement *= playerCharacter.IsRunning() ? speedRunning : speedWalking;
            movement = transform.TransformDirection(movement);
            Velocity = new Vector3(movement.x, rigidBody.linearVelocity.y, movement.z);
        }

        private void Jump()
        {
            canJump = false;
            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (audioClipJump)
                audioSource.PlayOneShot(audioClipJump);
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        private void ResetJump()
        {
            canJump = true;
        }

        private void PlayFootstepSounds()
        {
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        #endregion
    }
}
