﻿using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
//using static Unity.Cinemachine.InputAxisControllerBase<T>;
using System.Collections;
using HUD;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(MyPlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Player health")]
    public float MaxHealth = 100f;
    public float Health = 100f;
    [Tooltip("Player is Dead")]
    public bool IsDead = false;
    private bool IsInFluid = false;
    private float SinkSpeed = 0f;
    private Vector3 Velocity;
    [Tooltip("Spawning")]
    private Vector3 SpawnPosition;
    private Quaternion SpawnRotation;
    public HUDController HUDController;

    [Header("Sounds")]
    public AudioClip DeathAudioClip;
    public AudioClip DamageAudioClip;
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    private AudioSource damageAudioSource;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    //[Tooltip("Weapon")]
    //public GameObject Weapon;

    [Header("Movement")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private MyPlayerInput _playerInput;
    private Animator _animator;
    private CharacterController _controller;
    private MyPlayerInput _input;
    //private StarterAssetsInputs _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    private bool _hasAnimator;

    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        SpawnPosition = transform.position;
        SpawnRotation = transform.rotation;

        damageAudioSource = GetComponentInChildren<AudioSource>();

        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<MyPlayerInput>();
        _input = _playerInput;

        AssignAnimationIDs();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;

        //Weapon.SetActive(true);
    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);

        if (IsDead) return;

        if (!IsInFluid)
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
            //Gun();
        }
        else
        {
            FluidDynamics();
        }
    }

    /*private void Gun()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _animator.SetBool("Weapon", true);
            Weapon.SetActive(true);
        }
        if (Input.GetMouseButtonUp(0))
        {
            _animator.SetBool("Weapon", false);
            Weapon.SetActive(false);
        }
    }*/

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move the player
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                            new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // update animator if using character
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // if we are not grounded, do not jump
            _input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.2f)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        Health -= damage;
        Health = Mathf.Clamp(Health, 0, MaxHealth);
        Debug.Log($"💥 Player Takes Damage! Health: {Health}, Damage taken: -{damage}");

        if (Health <= 0)
        {
            PlayerDie();
        }
        else
        {
            if (damageAudioSource != null && !damageAudioSource.isPlaying)
            {
                damageAudioSource.PlayOneShot(DamageAudioClip, 0.5f);
            }
        }
    }

    private void PlayerDie()
    {
        IsDead = true;
        Debug.Log("!!! DEAD !!!");
        if (damageAudioSource != null)
        {
            Debug.Log("Playing death sound");
            damageAudioSource.PlayOneShot(DeathAudioClip, 1.5f);
        }

        if (HUDController != null)
            HUDController.FadeInDeathMessage();

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // Wait before respawn
        yield return new WaitForSeconds(5f); 

        Respawn();
    }

    // Respawn method to reset player state:
    private void Respawn()
    {
        // Reset all AcidPools in the scene
        ResetAllAcidPools();

        // Reset health and dead state
        Health = MaxHealth;
        IsDead = false;

        // Move player back to spawn point
        _controller.enabled = false; // temporarily disable to teleport safely
        transform.SetPositionAndRotation(SpawnPosition, SpawnRotation);
        _controller.enabled = true;

        if (HUDController != null)
            HUDController.ResetMessage();

        Debug.Log("Player Respawned!");
    }

    private void ResetAllAcidPools()
    {
        IsInFluid = false;
        this.SinkSpeed = 0;

        AcidPoolController[] pools = Object.FindObjectsByType<AcidPoolController>(FindObjectsSortMode.None);
        foreach (AcidPoolController pool in pools)
        {
            pool.ResetPool();
        }
    }

    private void FluidDynamics()
    {
        if (IsDead) return;

        if (IsInFluid)
        {
            // Apply sinking effect up to a maximum depth
            if (transform.position.y > -1.5)
            {
                Velocity.y = -SinkSpeed;

                // Apply movement
                _controller.Move(Velocity * Time.deltaTime);
            }
            else
            {
                Velocity.y = 0;
                //_controller.Move(Velocity * Time.deltaTime);
            }
        }
    }

    public void SetInFluid(bool inFluid, float sinkSpeed)
    {
        IsInFluid = inFluid;
        this.SinkSpeed = sinkSpeed;
    }
}
