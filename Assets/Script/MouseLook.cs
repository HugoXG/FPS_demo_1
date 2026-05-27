using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 鼠标控制视角
 */
public class MouseLook : MonoBehaviour 
{
    [Tooltip("守望先锋灵敏度（填你的 OW 设置）")]
    public float owSensitivity = 3.45f;
    
    // 守望先锋的 yaw 系数（每灵敏度单位对应的角度）
    // 这个 0.022 是社区测出来的经验值
    private const float OW_YAW = 0.022f;
    
    private Transform playerBody;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerBody = transform.GetComponentInParent<PlayerController>().transform;
    }

    void Update()
    {
        // 不要乘 Time.deltaTime！鼠标输入本身已包含帧间隔
        float mouseX = Input.GetAxis("Mouse X") * owSensitivity * OW_YAW;
        float mouseY = Input.GetAxis("Mouse Y") * owSensitivity * OW_YAW;

        yRotation -= mouseY;
        yRotation = Mathf.Clamp(yRotation, -89f, 89f);
        
        transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);
        playerBody.Rotate(mouseX * Vector3.up);
    }
}
