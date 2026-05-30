using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/**
 *  玩家控制器 —— 负责处理玩家的移动输入，并驱动角色在场景中移动。
 *  挂在带有 CharacterController 组件的胶囊体（或其他 GameObject）上运行。
 */
public class PlayerController : MonoBehaviour {

    // ──────────────────────── 字段（成员变量）────────────────────────

    // CharacterController 是 Unity 内置的角色控制器组件，
    // 提供 Move() 方法来移动角色，并自动处理与墙壁、地面的碰撞。
    // 这里声明一个变量，稍后在 Start() 里从当前 GameObject 上获取这个组件。
    private CharacterController characterController;

    // moveDirection：每一帧计算出的移动方向向量（世界坐标系下的单位向量）。
    // public 意味着你可以在 Inspector 窗口实时看到它的值，方便调试。
    public Vector3 moveDirection;
    private float verticalVelocity;

    // [Header("玩家数值")] 在 Inspector 面板里显示一个加粗的标题"玩家数值"，
    // 让下面几个字段归类在一起，方便查看。
    [Header("玩家数值")]

    // Speed：当前实际使用的移动速度。每一帧在 Moving() 里被赋值为 walkSpeed。
    // private 表示不在 Inspector 里显示，外部脚本也无法访问。
    public float Speed;

    // [Tooltip("...")] 在 Inspector 里鼠标悬停到该字段上时，显示提示文字。
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
    public int jumpCountsSum;
    public bool isGround;
    public bool isSky;
    public bool isWall;
    
    public bool isCanCrouch;
    public bool isCrouching;

    public LayerMask crouchLayerMask; 

    void Start() {
        characterController = GetComponent<CharacterController>();

        walkSpeed = 7f;       // 行走：每秒 7 个单位
        runSpeed = 14f;       // 奔跑：每秒 10 个单位
        crouchSpeed = 5f;     // 下蹲：每秒 5 个单位
        jumpForce = 10f;
        fallForce = 25f; 
        setJumpCountsSum(2); // 跳跃次数
        jumpCounts = jumpCountsSum;
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
        if (isRun && isGround) {
            state = MovementState.running;
            Speed = runSpeed;
        } else {
            state = MovementState.walking;
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
            jumpCounts = jumpCountsSum;

            
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

    public void setJumpCountsSum(int count) {
        this.jumpCountsSum = count;
    }

    public enum MovementState {
        walking,
        running,
        crouching,
    }
}
