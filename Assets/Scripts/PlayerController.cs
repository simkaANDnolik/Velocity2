using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public int playerId = 0; // 0 для первого персонажа, 1 для второго

    private float horizontal;
    private float vertical;

    private float rotationSpeed = 155;
    public Camera camera;
    private Vector2 rotation;
    private float speed = 15;
    private float gravity = -40.81f;
    private float verticalVelocity;

    [Header("Прыжок")]
    public float jumpHeight = 3f;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Скольжение (Slide)")]
    public KeyCode slideKey = KeyCode.LeftControl;
    public float slideInitialBoost = 3f;
    public float slideMinSpeed = 20f;
    public float slideFriction = 4f;
    public float flatFriction = 2f;
    public float flatSlideMinSpeed = 8f;
    public float slopeStickForce = 35f;
    public float maxSlopeAngle = 60f;
    public float minSlopeAngle = 5f;
    public float slideCooldown = 0.3f;

    [Header("Разгон на склоне")]
    public float slopeSlideAcceleration = 160f;
    public float slopeSlideMaxSpeed = 10000f;

    [Header("Прыжок от стены (Wall Jump)")]
    public float wallCheckDistance = 0.7f;
    public float wallJumpHeightMultiplier = 0.5f;
    public float wallJumpSpeedMultiplier = 2.5f;
    public float wallJumpCooldown = 0.05f;
    public LayerMask wallLayers = ~0;
    public float wallSidePush = 0.6f;
    public float wallJumpForwardPush = 1.2f;

    // Slide state
    private bool isSliding;
    private float slideCooldownTimer;
    private Vector3 slideDirection;
    private float currentSlideSpeed;
    private bool onSlope;
    private Vector3 slopeNormal = Vector3.up;

    // Wall jump
    private bool isTouchingWall;
    private Vector3 wallNormal;
    private float wallJumpCooldownTimer;
    private Collider currentWall;
    private Vector3 wallSideDirection;
    private Collider lastWallJumpedFrom;

    private Vector3 wallJumpImpulse;
    private float wallJumpImpulseTimer;
    public float wallJumpImpulseDuration = 0.3f;

    private RaycastHit groundHit;
    private float characterHeight;
    private Vector3 characterCenter;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        characterHeight = controller.height;
        characterCenter = controller.center;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (SwitchCharacters.svitch == playerId)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            CheckGround();
            CheckWall();
            HandleSlide();

            if (wallJumpCooldownTimer > 0)
                wallJumpCooldownTimer -= Time.deltaTime;

            Vector3 camForward = camera.transform.forward;
            Vector3 camRight = camera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * vertical + camRight * horizontal;

            if (controller.isGrounded && verticalVelocity < 0 && !isSliding)
                verticalVelocity = -2f;

            if (controller.isGrounded && Input.GetKeyDown(jumpKey) && !isSliding)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else if (!controller.isGrounded && isTouchingWall && Input.GetKeyDown(jumpKey)
                     && !isSliding && wallJumpCooldownTimer <= 0)
            {
                PerformWallJump(moveDir);
            }

            Vector3 move;

            if (isSliding)
            {
                move = CalculateSlideMovement();
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
                move = moveDir * speed + Vector3.up * verticalVelocity;
            }

            if (wallJumpImpulseTimer > 0f)
            {
                wallJumpImpulseTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(wallJumpImpulseTimer / wallJumpImpulseDuration);
                move += wallJumpImpulse * t;
            }

            controller.Move(move * Time.deltaTime);

            if (isSliding)
            {
                StickToSurface();
            }

            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            mouseDelta *= rotationSpeed * Time.deltaTime;
            rotation.y += mouseDelta.x;
            rotation.x = Math.Clamp(rotation.x - mouseDelta.y, -90, 90);
            camera.transform.localEulerAngles = rotation;
        }
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f) + 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out groundHit, rayLength))
        {
            slopeNormal = groundHit.normal;
            float slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
            onSlope = slopeAngle > minSlopeAngle && slopeAngle <= maxSlopeAngle;
        }
        else
        {
            slopeNormal = Vector3.up;
            onSlope = false;
        }
    }

    private void CheckWall()
    {
        isTouchingWall = false;
        wallNormal = Vector3.zero;
        currentWall = null;

        if (controller.isGrounded) return;

        Vector3 origin = transform.position + controller.center;
        float radius = controller.radius;
        float castDistance = radius + wallCheckDistance;

        Vector3[] directions =
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        foreach (var dir in directions)
        {
            if (Physics.SphereCast(origin, radius * 0.9f, dir, out RaycastHit hit, castDistance, wallLayers, QueryTriggerInteraction.Ignore))
            {
                float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
                if (slopeAngle > 60f && slopeAngle < 120f)
                {
                    isTouchingWall = true;
                    wallNormal = hit.normal;
                    currentWall = hit.collider;

                    Vector3 toWall = -hit.normal;
                    toWall.y = 0f;
                    wallSideDirection = toWall.normalized;
                    break;
                }
            }
        }
    }

    private void PerformWallJump(Vector3 moveDir)
    {
        float wallJumpHeight = jumpHeight * wallJumpHeightMultiplier;
        verticalVelocity = Mathf.Sqrt(wallJumpHeight * -2f * gravity);

        Vector3 awayFromWall = wallNormal.normalized;
        awayFromWall.y = 0f;
        awayFromWall.Normalize();

        Vector3 camRight = camera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 sideDir = Vector3.Cross(Vector3.up, awayFromWall).normalized;
        if (Vector3.Dot(sideDir, camRight) < 0f)
            sideDir = -sideDir;

        Vector3 camForward = camera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 inputDir = moveDir.normalized;

        Vector3 jumpDir = (awayFromWall
                           + sideDir * wallSidePush
                           + camForward * wallJumpForwardPush
                           + inputDir * 0.3f).normalized;

        float wallJumpSpeed = speed * wallJumpSpeedMultiplier;

        wallJumpImpulse = jumpDir * wallJumpSpeed;
        wallJumpImpulseTimer = wallJumpImpulseDuration;

        wallJumpCooldownTimer = wallJumpCooldown;

        if (isSliding)
            StopSlide();
    }

    private void HandleSlide()
    {
        if (slideCooldownTimer > 0)
            slideCooldownTimer -= Time.deltaTime;

        bool slideHeld = Input.GetKey(slideKey);

        // Начало — по нажатию
        if (!isSliding && controller.isGrounded && Input.GetKeyDown(slideKey) && slideCooldownTimer <= 0)
        {
            StartSlide();
        }

        // Конец — когда отпустил кнопку
        if (isSliding && !slideHeld)
        {
            StopSlide();
        }
    }

    private void StartSlide()
    {
        isSliding = true;
        slideCooldownTimer = 0f;

        Vector3 camForward = camera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 inputDir = (camForward * vertical + camera.transform.right * horizontal).normalized;

        if (inputDir.sqrMagnitude < 0.01f)
            slideDirection = camForward;
        else
            slideDirection = inputDir;

        currentSlideSpeed = speed * slideInitialBoost * 2f;
        verticalVelocity = 0f;

        controller.height = characterHeight * 0.5f;
        controller.center = new Vector3(characterCenter.x, characterCenter.y * 0.5f, characterCenter.z);
    }

    private void StopSlide()
    {
        isSliding = false;
        slideCooldownTimer = slideCooldown;

        controller.height = characterHeight;
        controller.center = characterCenter;
    }

    private Vector3 CalculateSlideMovement()
    {
        Vector3 move;

        if (onSlope)
        {
            // СИЛЬНЫЙ РАЗГОН НА СКЛОНЕ — до бесконечности
            float slopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
            float slopeFactor = Mathf.Clamp01(slopeAngle / maxSlopeAngle);
            float accel = slopeSlideAcceleration * (0.5f + slopeFactor);

            currentSlideSpeed = Mathf.MoveTowards(
                currentSlideSpeed,
                slopeSlideMaxSpeed,
                accel * Time.deltaTime
            );

            Vector3 projected = Vector3.ProjectOnPlane(slideDirection, slopeNormal);
            if (projected.sqrMagnitude > 0.01f)
                slideDirection = projected.normalized;

            move = slideDirection * currentSlideSpeed;

            verticalVelocity = -2f;
        }
        else
        {
            // НА РОВНОЙ — скорость падает из-за трения
            currentSlideSpeed = Mathf.MoveTowards(
                currentSlideSpeed,
                flatSlideMinSpeed,
                flatFriction * Time.deltaTime
            );

            move = slideDirection * currentSlideSpeed;

            move += Vector3.down * slopeStickForce;
            verticalVelocity = -2f;
        }

        return move;
    }

    private void StickToSurface()
    {
        Vector3 origin = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f) + 0.6f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength))
        {
            float distanceToSurface = hit.distance - (controller.height * 0.5f);

            if (distanceToSurface > 0.01f)
            {
                float stickSpeed = Mathf.Min(distanceToSurface * 30f, 25f);
                controller.Move(Vector3.down * stickSpeed * Time.deltaTime);
            }

            slopeNormal = hit.normal;

            Vector3 projected = Vector3.ProjectOnPlane(slideDirection, slopeNormal);
            if (projected.sqrMagnitude > 0.01f)
                slideDirection = projected.normalized;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (controller == null) return;

        Vector3 origin = transform.position + controller.center;
        float rayLength = (controller.height * 0.5f) + 0.5f;

        Gizmos.color = onSlope ? Color.yellow : Color.green;
        Gizmos.DrawRay(origin, Vector3.down * rayLength);

        if (onSlope && groundHit.collider != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(groundHit.point, slopeNormal * 2f);
        }

        if (isTouchingWall)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(origin, wallNormal * 2f);
        }

        if (wallJumpImpulseTimer > 0f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(origin, wallJumpImpulse.normalized * 3f);
        }
    }
}