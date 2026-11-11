# 项目状态（STATUS）

> 最后更新：2026-11-11
> 用途：**新对话 / 隔天回来时先读这份**，再按需查 `VISION.md` 和 `MVP-CHECKLIST.md`。
> 写法：只写"现在是什么状态"，历史交给 `git log`。

---

## 一句话

Unity 6 射击/武器沙盒项目，当前处于 **MVP 第 1–2 周**：正在搭军火库白盒 + 武器拾取。

---

## 当前阶段

**MVP 第 1–2 周：军火库 + 第一把枪**

目标（来自 `MVP-CHECKLIST.md`）：

```
□ 军火库白盒 ✅ 已完成
□ 墙上有一把枪，能拿起来（E 键）    ← 下一步
□ 拿起枪有手感反馈
□ 门口有靶子，能开火打中
□ 开火音效 + 视觉反馈
□ 击中计分
□ 能放下枪、再拿起
```

---

## 最近完成（按时间倒序）

| 提交 | 内容 |
|---|---|
| `e85b9c9` | 补充 Git 工作流约定 |
| `5e01c35` | 重建 Hub 白盒场景（30×30，打靶距离约 26.5 米） |
| `214b540` | 军火库纵深加到 30 米，参数化靶距/间距/出生点 |
| `47a1ce6` | 移除"重建玩家预制体"菜单，预制体成为唯一真身 |
| `f1507af` | 重建预制体前加确认 |
| `376862e` | 一键注册 Hub 为启动场景 |
| `bf793d2` | 生成 Hub 场景与 Player.prefab |
| `2fad5c0` | 玩家改为预制体主路径 |
| `4303710` | 军火库白盒生成器 + 玩家工厂 |
| `4e46ef1` | 可扩展骨架（内容注册表 / 武器行为 / 角色定义 / 移动修正器） |
| `882e29c` | 相机重构为 CameraRig（自由视角）、移动支持飞行/冲刺/外部速度 |

---

## 进行中

无（拾取系统尚未开始）。

---

## 下一步（按顺序）

1. **`WeaponPickup`**：展位上的武器（关联 WeaponData）
2. **`WeaponSocket`**：玩家手上的武器挂点（装备/卸下）
3. **`PlayerInteractor`**：靠近检测 + 屏幕提示 + `E` 拿起
4. 基础反馈：枪口焰、命中火花、音效占位、命中计分
5. 放下武器（`Q` 或交互键）

---

## 项目现状

### 技术栈

- Unity **6000.6.0f1**（Unity 6）+ URP + Input System（新版）
- 语言：C#；工程在 `D:\un\demo\1`

### 场景

| 场景 | 用途 | 状态 |
|---|---|---|
| `Assets/Scenes/Hub.unity` | 军火库白盒（30 × 30 × 6） | 主场景，由菜单生成 |
| `Assets/Scenes/SampleScene.unity` | 旧射击原型（野外靶场） | 保留作回归测试 |

### 预制体

- `Assets/Prefabs/Player.prefab` —— 玩家唯一模板（**改玩家就改它**）
- `Assets/Prefabs/Bullet.prefab` —— 子弹

### 脚本结构

```
Assets/Scripts/
├── Core/ContentRegistry.cs        内容注册表（内置与 mod 同源）
├── Combat/                        Health / IDamageable / DamageInfo / TargetHealth
├── Player/                        CameraRig / CharacterDefinition / PlayerFactory / PlayerHealth
├── Weapons/                       WeaponData / WeaponController / WeaponSwitcher
│                                  IWeaponBehavior / BuiltInWeaponBehaviors / WeaponBehaviorRegistry
├── Bullet.cs                      通用弹体
├── PlayerMovement.cs              地面 + 飞行 + 冲刺 + 修正器 + 外部速度
└── Editor/                        ArmorySetup / PlayerPrefabBuilder / PlayerSetup / WeaponSetup
```

### 操作键位（当前）

| 键 | 功能 |
|---|---|
| WASD | 移动（按镜头方向） |
| Shift | 冲刺 |
| Space | 跳跃 / 飞行上升 |
| Left Ctrl | 飞行下降 |
| F | 切换飞行模式 |
| V | 第一/第三人称切换 |
| 按住 Alt | 自由视角 |
| 鼠标左键 | 开火（Hub 里暂时无武器） |
| R | 换弹 |
| 1/2/3 | 切换武器（SampleScene 里可用） |

---

## 已知问题 / 技术债

| # | 问题 | 影响 | 计划 |
|---|---|---|---|
| 1 | Hub 里玩家没有武器 | 暂时不能开火 | 拾取系统解决 |
| 2 | 相机无碰撞避让 | 第三人称贴墙穿模 | MVP 后 |
| 3 | 无音效 / 枪口焰 / 命中粒子 | 手感偏"空" | 第 3–4 周 |
| 4 | 无计分 | 打靶没有反馈闭环 | 第 1–2 周末 |
| 5 | 技能系统未实现 | `CharacterDefinition.abilityIds` 只是占位 | MVP 后 |
| 6 | Build Settings 里 Hub 的登记需确认 | 打包时可能漏场景 | 跑一次菜单即可 |
| 7 | Unity AI 助手网络报错（`Curl error 35`） | 刷屏、与项目无关 | 可禁用该包 |
| 8 | `PlayerSetup` 与 `PlayerFactory` 结构有重叠 | 两处维护成本 | 后续合并 |

---

## 给新对话的开场提示

把下面这段直接发给新会话：

> 这是我的 Unity 项目（`D:\un\demo\1`），先读
> `Docs/STATUS.md`、`Docs/VISION.md`、`Docs/MVP-CHECKLIST.md`、`Docs/WORKFLOW.md`，
> 了解现状后继续 MVP 第 1–2 周：军火库武器拾取系统。
> 提交/推送规则按 `WORKFLOW.md`，动手前先问我。

---

## 更新方式

每次收工或完成一个阶段时，更新本文档的这三节：

1. `最近完成` —— 加一行
2. `进行中` / `下一步` —— 改状态
3. `已知问题` —— 增删

其余内容（技术栈、场景、键位）只在结构变化时更新。
