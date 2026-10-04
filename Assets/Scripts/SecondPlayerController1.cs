using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SecondPlayerController : MonoBehaviour
{
    private float horizontal;
    private float vertical;

    private float rotationSpeed = 155;
    public Camera camera;
    private Vector2 rotation;
    private float speed = 15;
    private float gravity = -28.81f;
    private float verticalVelocity;

    [Header("Прыжок")]
    public float jumpHeight = 2f;      // высота прыжка в метрах
    public KeyCode jumpKey = KeyCode.Space;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (SwitchCharacters.svitch == 1)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            Vector3 camForward = camera.transform.forward;
            Vector3 camRight = camera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * vertical + camRight * horizontal;

            // Гравитация
            if (controller.isGrounded && verticalVelocity < 0)
                verticalVelocity = -2f;

            // Прыжок
            if (controller.isGrounded && Input.GetKeyDown(jumpKey))
            {
                // Формула: v = sqrt(2 * h * (-g))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 move = moveDir * speed + Vector3.up * verticalVelocity;

            // ВАЖНО: Move вместо transform.position
            controller.Move(move * Time.deltaTime);

            // Поворот камеры
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            mouseDelta *= rotationSpeed * Time.deltaTime;
            rotation.y += mouseDelta.x;
            rotation.x = Math.Clamp(rotation.x - mouseDelta.y, -90, 90);
            camera.transform.localEulerAngles = rotation;
        }
    }
}