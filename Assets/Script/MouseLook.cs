using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 鼠标控制视角
 */
public class MouseLook : MonoBehaviour {

    private CharacterController characterController;
    [Tooltip("摄像机初始高度:人物高度")] public float cameraHeight; 
    private float interpolationSpeed = 12f; // 摄像机缓动速度
    [Tooltip("守望先锋灵敏度（填你的 OW 设置）")] public float owSensitivity = 3.45f;
    
    // 守望先锋的 yaw 系数（每灵敏度单位对应的角度）
    // 这个 0.022 是社区测出来的经验值
    private const float OW_YAW = 0.022f;
    
    private Transform playerBody;
    private float yRotation = 0f;

    void Start()
    {
        characterController = GetComponentInParent<CharacterController>();
        playerBody = transform.GetComponentInParent<PlayerController>().transform;

        cameraHeight = characterController.height;
        transform.localPosition = new Vector3(0, cameraHeight, 0);

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        move();
        crouchCameraFollow();

        // float tagetHeight = characterController.height * 0.5f;
        // cameraHeight = Mathf.Lerp(
        //  cameraHeight, tagetHeight, interpolationSpeed * Time.deltaTime);
        // transform.localPosition = Vector3.up * cameraHeight;
    }

    /**
     * 移动视角
     */
    public void move() {
        // 不要乘 Time.deltaTime！鼠标输入本身已包含帧间隔
        float mouseX = Input.GetAxis("Mouse X") * owSensitivity * OW_YAW;
        float mouseY = Input.GetAxis("Mouse Y") * owSensitivity * OW_YAW;

        yRotation -= mouseY;
        yRotation = Mathf.Clamp(yRotation, -89f, 89f);
        
        transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);
        playerBody.Rotate(mouseX * Vector3.up);
    }

    /**
     * 人物下蹲时，摄像机跟随
     */
    public void crouchCameraFollow() {
       float targetHeight = characterController.height;
       cameraHeight = Mathf.Lerp(
        cameraHeight, targetHeight, interpolationSpeed * Time.deltaTime);
       transform.localPosition = new Vector3(0, cameraHeight, 0);
    }
}
