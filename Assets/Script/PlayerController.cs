using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.AI;

/**
 *  玩家控制器 —— 负责处理玩家的移动输入，并驱动角色在场景中移动。
 *  挂在带有 CharacterController 组件的胶囊体（或其他 GameObject）上运行。
 */
public class PlayerController : MonoBehaviour {

    private CharacterController characterController;
    public Vector3 moveDirection;
    private AudioSource audioSource; // 音效源
    private float verticalVelocity;

    [Header("玩家数值")]

    public float Speed;

    [Tooltip("行走速度")] public float walkSpeed;    // 行走速度，默认在 Start() 里设为 7
    [Tooltip("奔跑速度")] public float runSpeed;      // 奔跑速度，预留（目前代码里未使用奔跑逻辑）
    [Tooltip("下蹲行走速度")] public float crouchSpeed; // 下蹲速度，预留（目前代码里未使用下蹲逻辑）
    [Tooltip("跳跃的力")] public float jumpForce;
    [Tooltip("下落的力")] public float fallForce;
    [Tooltip("下蹲时玩家高度")] public float crouchHeight;
    [Tooltip("站立时玩家高度")] public float standHeight;
    

    [Header("按键设置")]
    [Tooltip("奔跑")] public KeyCode runInputName = KeyCode.LeftShift;
    [Tooltip("跳跃")]public KeyCode jumpInputName = KeyCode.Space;
    [Tooltip("下蹲")]public KeyCode crouchInputName = KeyCode.LeftControl;

    [Header("玩家属性判断")]
    public MovementState state;
    private CollisionFlags collisionFlags;
    public bool isWalk;
    public bool isRun;
    public bool isJump;
    private int jumpCounts;
    public int jumpMaxCounts;
    public bool isGround;
    public bool isSky;
    public bool isWall;
    
    public bool isCanCrouch;
    public bool isCrouching;

    bool stateIsChanged = false;

    public LayerMask crouchLayerMask; 
    [Header("音效")]
    [Tooltip("行走音效")]public AudioClip walkSound;
    [Tooltip("奔跑音效")]public AudioClip runSound;

    void Start() {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        walkSpeed = 7f;       // 行走：每秒 7 个单位
        runSpeed = 14f;       // 奔跑：每秒 10 个单位
        crouchSpeed = 5f;     // 下蹲：每秒 5 个单位
        jumpForce = 10f;
        fallForce = 25f; 
        setJumpCountsSum(2); // 跳跃次数
        jumpCounts = jumpMaxCounts;
        characterController.height = 2f;
        standHeight = characterController.height;
        crouchHeight = characterController.height / 2;
        isGround = false;
    }


    void Update() {
        moveDirection = Vector3.zero;

        GiveGravity();
        CanCrouch();

        Jump();
        Crouch();
        Moving();

        ExecuteMovement(); // 统一处理移动
        UpdateMovementState(); // 更新移动状态

        PlayeAudio();

    }

    /**
     * 添加重力
     */
    private void GiveGravity() {
        verticalVelocity -= fallForce * Time.deltaTime;  // 改：改变速度，不是直接移动
    }

    /**
     * 人物移动 —— 读取水平/垂直输入轴，计算移动方向，执行移动。
     */
    public void Moving() {

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        isRun = Input.GetKey(runInputName);
        isWalk = (Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0) ? true : false;
        if (isRun) {
            if (isCrouching) {
                Speed = runSpeed * 0.7f;
            } else if (isJump) {
                Speed = runSpeed * 1.3f;
            } else {
                Speed = runSpeed;
            }
        } else {
            Speed = walkSpeed;
        }

        Vector3 horizontalMotion = (transform.right * h + transform.forward * v).normalized;
        moveDirection += horizontalMotion * Speed * Time.deltaTime;
    }

    /**
     * 人物跳跃 —— 检测是否可以跳跃，并执行跳跃。
     */
    public void Jump () {
        isJump = Input.GetKeyDown(jumpInputName);

        if (jumpCounts == 0) return;

        if (isJump) {
            verticalVelocity = jumpForce;
            jumpCounts--;
        }
    }

    /**
     * 人物蹲下 —— 检测是否可以蹲下，并设置蹲下状态。
     */
    public void CanCrouch() {
        if (isGround) {
            isCanCrouch = true;
        } else {
            isCanCrouch = false;
        }
    }


    /**
     * 人物蹲下 —— 蹲下。
     */
    public void Crouch() {
        bool isCrouchPressed = Input.GetKey(crouchInputName);
        if (isCanCrouch && isCrouchPressed) {
            characterController.height = crouchHeight;
            characterController.center = new Vector3(0, crouchHeight * 0.5f, 0);
            isCrouching = true;
        } else if (isCrouching && !isCrouchPressed) {
            if (CanStandUp()) {
                characterController.height = standHeight;
                characterController.center = new Vector3(0, standHeight * 0.5f, 0);
                isCrouching = false;
            }
        }
    }

    /**
     * 检测是否可以站起来
     */
    public bool CanStandUp() {
        Vector3 headPositon = transform.position + Vector3.up * characterController.height;
        Collider[] hits = Physics.OverlapSphere(headPositon, characterController.radius, crouchLayerMask);
        return hits.Length == 0;
    }

    /**
     * 统一执行移动，并检测碰撞
     */
    void ExecuteMovement() {
        isGround = false;
        isSky = false;
        isWall = false;

        // 统一执行移动
        // 把垂直速度加到 moveDirection 上
        moveDirection += Vector3.up * verticalVelocity * Time.deltaTime;
        collisionFlags = characterController.Move(moveDirection);

        if(collisionFlags == CollisionFlags.None) {
            isSky = true;
        }
        
        if ((collisionFlags & CollisionFlags.Below) != 0) {
            if (verticalVelocity < 0) {
                verticalVelocity = -2f;  // 贴地
            }

            isGround = true;
            jumpCounts = jumpMaxCounts;
        }

        if ((collisionFlags & CollisionFlags.Sides) != 0) {
            isWall = true;
        }

        if ((collisionFlags & CollisionFlags.Above) != 0) {
            if (verticalVelocity > 0) {
            verticalVelocity = 0;  // 立即停止上升
            }
        }

    }

    /**
     * 更新移动状态
     */
    void UpdateMovementState() {
        MovementState newState;
        // 优先级最高：空中
        if (!isGround) {
            newState = MovementState.flying;
        } else if (moveDirection.x == 0 && moveDirection.z == 0) {
            newState = MovementState.standing;
        } else if (isCrouching) {
            newState = MovementState.crouching;
        } else if (isRun) {
            newState = MovementState.running;
        } else {
            newState = MovementState.walking;
        }

        stateIsChanged = (newState != state);
        state = newState;

        if (stateIsChanged) {
            Debug.Log("玩家状态：" + state);
        }
    }


    /**
     * 设置跳跃次数
     */
    public void setJumpCountsSum(int count) {
        this.jumpMaxCounts = count;
    }

    /**
     * 播放音效
     */
    public void PlayeAudio() {
        bool shouldPlaySound = (state == MovementState.walking ||
                                state == MovementState.running);

        bool shouldStopSound = (state == MovementState.standing ||
                                state == MovementState.crouching ||
                                state == MovementState.flying);

        if (stateIsChanged) {
            if (shouldPlaySound) {
                switch (state) {
                    case MovementState.walking:
                        Debug.Log("播放行走音效");
                        audioSource.clip = walkSound;
                        break;
                    case MovementState.running:
                        Debug.Log("播放奔跑音效");
                        audioSource.clip = runSound;
                        break;
                    case MovementState.crouching:
                        Debug.Log("播放行走音效");
                        audioSource.clip = walkSound;
                        break;
                }
                audioSource.Play();
            } else if (shouldStopSound && audioSource.isPlaying) {
                Debug.Log("停止播放音效");
                audioSource.Stop();
            }    
        }
    }

    /**
     * 移动状态枚举
     */
    public enum MovementState {
        standing,
        walking,
        running,
        crouching,
        flying,
    }
}
