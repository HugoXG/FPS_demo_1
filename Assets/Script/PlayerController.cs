using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // [Header("玩家数值")] 在 Inspector 面板里显示一个加粗的标题"玩家数值"，
    // 让下面几个字段归类在一起，方便查看。
    [Header("玩家数值")]

    // Speed：当前实际使用的移动速度。每一帧在 Moving() 里被赋值为 walkSpeed。
    // private 表示不在 Inspector 里显示，外部脚本也无法访问。
    private float Speed;

    // [Tooltip("...")] 在 Inspector 里鼠标悬停到该字段上时，显示提示文字。
    [Tooltip("行走速度")] public float walkSpeed;    // 行走速度，默认在 Start() 里设为 7
    [Tooltip("奔跑速度")] public float runSpeed;      // 奔跑速度，预留（目前代码里未使用奔跑逻辑）
    [Tooltip("下蹲行走速度")] public float crouchSpeed; // 下蹲速度，预留（目前代码里未使用下蹲逻辑）

    // ──────────────────────── Unity 生命周期方法 ────────────────────────

    // Start：在脚本第一次启用时调用（仅调用一次）。
    // 适合做初始化工作——获取组件引用、设置默认值等。
    void Start() {
        // GetComponent<CharacterController>()：
        // 在当前 GameObject 上查找 CharacterController 组件并返回引用。
        // 如果找不到，返回 null，后面调用 .Move() 时会抛出 NullReferenceException。
        characterController = GetComponent<CharacterController>();

        // 初始化三个速度值。
        // 因为字段是 public 的，你可以在 Inspector 里覆盖这些初始值。
        walkSpeed = 7f;       // 行走：每秒 7 个单位
        runSpeed = 10f;       // 奔跑：每秒 10 个单位
        crouchSpeed = 5f;     // 下蹲：每秒 5 个单位
    }

    // Update：每帧调用一次。
    // 帧率不固定（取决于设备性能），所以移动计算需要用 Time.deltaTime 做帧率补偿。
    // 适合处理：玩家输入、非物理的逐帧逻辑。
    void Update() {
        // 每帧调用 Moving()，读取键盘输入并移动角色。
        Moving();
    }

    // FixedUpdate：以固定时间间隔调用（默认 0.02 秒/次，即每秒 50 次）。
    // 适合处理：物理相关计算（Rigidbody 操作等）。
    // 目前是空方法，因为你的移动放在了 Update 里（配合 CharacterController 是可以的）。
    private void FixedUpdate() {
        // 预留：未来如果需要用到 Rigidbody，可以把物理移动逻辑放在这里。
    }

    // ──────────────────────── 自定义方法 ────────────────────────

    /**
     * 人物移动 —— 读取水平/垂直输入轴，计算移动方向，执行移动。
     */
    public void Moving() {
        // Input.GetAxisRaw("Horizontal")：
        // 从 Input Manager 中读取名为 "Horizontal" 的轴当前值。
        // "Raw" 表示无平滑过滤，直接返回 -1、0 或 1（键盘）或摇杆原始值。
        // 返回 float：
        //   按 D / 右箭头 →  1
        //   按 A / 左箭头 → -1
        //   都不按       →  0
        float h = Input.GetAxisRaw("Horizontal");

        // Input.GetAxisRaw("Vertical")：
        // 同上，读取名为 "Vertical" 的轴。
        //   按 W / 上箭头 →  1
        //   按 S / 下箭头 → -1
        //   都不按       →  0
        float v = Input.GetAxisRaw("Vertical");

        // 将速度设为行走速度（目前没有根据 Shift 切换奔跑）。
        // 后续你可以加一个 if 判断，当玩家按住奔跑键时设 Speed = runSpeed。
        Speed = walkSpeed;

        // ─── 计算移动方向 ───
        // transform.right  : 角色自身坐标系的 X 轴方向（右边），世界坐标系下的向量。
        // transform.forward: 角色自身坐标系的 Z 轴方向（前面），世界坐标系下的向量。
        //
        // transform.right * h：
        //   h =  1 → 方向为角色右边
        //   h = -1 → 方向为角色左边
        //   h =  0 → 无水平分量
        //
        // transform.forward * v：
        //   v =  1 → 方向为角色前方
        //   v = -1 → 方向为角色后方
        //   v =  0 → 无前后分量
        //
        // 两个向量相加后，调用 .normalized：
        // 将向量长度缩放为 1（单位向量）。
        // 为什么要归一化？—— 如果同时按 W 和 D，向量长度会变成 √2 ≈ 1.414，
        // 导致斜向移动速度比正向快 41%。归一化后各方向速度一致。
        moveDirection = (transform.right * h + transform.forward * v).normalized;

        // ─── 执行移动 ───
        // characterController.Move(位移向量)：
        // 让角色按照给定的方向和距离移动。
        // 参数 = 方向 × 速度 × 帧间隔
        //
        // moveDirection：移动方向（单位向量，长度 = 1）
        // Speed：        移动速度（单位/秒）
        // Time.deltaTime：本帧消耗的时间（秒），通常在 0.016 左右（60 帧时）
        //
        // 为什么乘以 Time.deltaTime？
        // 因为 Update 每帧调用一次，但每帧时间不同（可能 0.01s 也可能 0.03s）。
        // 乘以 deltaTime 后，每秒移动的距离 = Speed，不受帧率影响。
        // 例如：Speed = 7, deltaTime = 0.016 → 每帧移动 7 × 0.016 = 0.112 单位
        characterController.Move(moveDirection * Speed * Time.deltaTime);
    }
}
