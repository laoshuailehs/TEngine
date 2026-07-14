# RaceOstrichSolo 项目框架文档

## 1. 项目概述

**项目名称**: RaceOstrichSolo（赛马大师/鸵鸟赛跑）  
**Unity 版本**: 2020.3.17f1  
**开发公司**: ChenFeng  
**产品名称**: RaceOstrichSoloMain  
**项目类型**: 街机博彩类赛马游戏（SBox 硬件平台）  
**热更新方案**: HybridCLR（原 Huatuo）  
**架构模式**: MVC + 双FSM有限状态机 + 事件驱动  

### 1.1 项目特点

- 运行在 **SBox 街机硬件** 上，通过 SBox SDK 与硬件交互（投币、退票、打印、按键灯控制等）
- 支持 **IoT 支付系统**（阿里云 MQTT 协议），可实现扫码支付
- 支持 **多机联网**（WebSocket C/S 架构）：一台主机 + 多台分机同时游戏
- 支持 **网络奖池（Net Jackpot）**，多台机器共享累计奖池
- 使用 **HybridCLR** 实现代码热更新，无需重新安装 APK
- 使用 **AssetBundle** 管理游戏资源，支持资源热更新
- 集成了 **Spine** 骨骼动画系统
- 内置 **打印机模块**（SkiaSharp + CryPrinter）
- 内置 **Bugly** 崩溃上报

---

## 2. 目录结构

```
RaceOstrichSolo/
├── Assets/
│   ├── Scripts/                        # 核心脚本代码
│   │   ├── AOT/                        # AOT 层（不参与热更）
│   │   │   ├── AssetBundleMgr/        # AssetBundle 管理模块
│   │   │   ├── Common/                # 公共工具类（路径、热更数据、加密、场景加载）
│   │   │   ├── ConfigMgr/             # 配置管理器
│   │   │   ├── Helper/                # 辅助工具（文件操作、日志）
│   │   │   ├── Version/               # 热更新流程（下载、校验、拷贝）
│   │   │   ├── BaseManager.cs         # 普通单例基类
│   │   │   ├── MonoSingleton.cs       # Mono单例基类
│   │   │   ├── Singleton.cs           # C#单例基类
│   │   │   └── MonoController.cs      # Mono代理（协程/Update代理）
│   │   │
│   │   ├── Base/                       # 基础框架层
│   │   │   ├── DataBase/              # SQLite 数据库
│   │   │   ├── IO/                    # IO 画布系统（设置界面/账单界面）
│   │   │   ├── IoTPay/                # IoT 支付（MQTT + 阿里云IoT）
│   │   │   ├── Net/                   # WebSocket 网络层（Server/Client）
│   │   │   ├── SBox/                  # SBox 硬件抽象层（事件/IO流/沙盒）
│   │   │   ├── Timer/                 # 定时器系统
│   │   │   ├── UnitySBox/             # Unity-SBox 桥接层
│   │   │   ├── AudioController.cs     # 音频控制器
│   │   │   ├── AudioMgr.cs            # 音频管理器
│   │   │   ├── AudioPool.cs           # 音频对象池
│   │   │   ├── BasePanel.cs           # UI面板基类
│   │   │   ├── EventCenter.cs         # 事件中心（观察者模式）
│   │   │   ├── EventHandle.cs         # 事件句柄常量
│   │   │   ├── FPS.cs                 # 帧率显示
│   │   │   ├── FSMSystem.cs           # 老版FSM有限状态机
│   │   │   ├── FSMState.cs            # FSM状态基类
│   │   │   ├── InputMgr.cs            # 输入管理器
│   │   │   ├── Loom.cs                # 多线程工具
│   │   │   ├── MonoMgr.cs             # Mono管理器
│   │   │   ├── MusicMgr.cs            # 音乐管理器
│   │   │   ├── PoolMgr.cs             # 对象池管理器
│   │   │   ├── PopTips.cs             # 弹窗提示
│   │   │   ├── ResMgr.cs              # 资源管理器（旧版/Resources）
│   │   │   ├── ScenesMgr.cs           # 场景管理器
│   │   │   ├── TweenUtils.cs          # Tween工具
│   │   │   ├── UIMgr.cs               # UI管理器
│   │   │   └── Utils.cs               # 通用工具类
│   │   │
│   │   └── Modules/                    # 业务模块层（热更新）
│   │       ├── BigPrize/              # 大奖表现模块
│   │       │   ├── AllPrize.cs        # 全中
│   │       │   ├── Coin.cs            # 金币特效
│   │       │   ├── DoubleLucky.cs     # 幸运翻倍
│   │       │   ├── LuckNumber.cs      # 幸运数字
│   │       │   ├── MosaicGold.cs      # 马赛克金币
│   │       │   ├── PrizeBase.cs       # 大奖基类
│   │       │   ├── SpotLightPrize.cs  # 射灯大奖
│   │       │   ├── SwitchNumber.cs    # 数字切换
│   │       │   ├── ThunderHorse.cs    # 雷电马
│   │       │   └── ThunderLine.cs     # 雷电连线
│   │       │
│   │       ├── Camara/                # 摄像机系统
│   │       │   ├── CameraManager.cs   # 摄像机管理器
│   │       │   ├── CameraCurve.cs     # 曲线摄像机
│   │       │   ├── CameraFinal.cs     # 结算摄像机
│   │       │   ├── CameraFollow.cs    # 跟随摄像机
│   │       │   ├── CameraFree.cs      # 自由摄像机
│   │       │   └── CameraStart.cs     # 开场摄像机
│   │       │
│   │       ├── Common/                # 通用模块
│   │       │   ├── ExtendMethod/      # 扩展方法
│   │       │   ├── FadeInOut.cs       # 淡入淡出
│   │       │   ├── Mirror.cs          # 镜像
│   │       │   ├── MotionGhost.cs     # 残影效果
│   │       │   └── Tags.cs            # 标签常量
│   │       │
│   │       ├── Container/             # 数据容器
│   │       ├── Environment/           # 场景环境
│   │       │   ├── Door.cs            # 门控制
│   │       │   ├── SceneControl.cs    # 场景控制
│   │       │   └── SkyBoxManager.cs   # 天空盒管理
│   │       │
│   │       ├── Horse/                 # 马匹系统
│   │       │   ├── HorseController.cs      # 马匹主控制器
│   │       │   ├── HorseAnimatorController.cs  # 马匹动画
│   │       │   ├── HorseAnimatorEvent.cs   # 马匹动画事件
│   │       │   ├── HorseState.cs           # 马匹状态
│   │       │   ├── HorseStartBehavior.cs   # 马匹起跑
│   │       │   ├── PointController.cs      # 分数控制器
│   │       │   ├── RankControl.cs          # 排名控制
│   │       │   └── WalkToFence.cs          # 走向围栏
│   │       │
│   │       ├── Manager/               # 游戏管理器
│   │       │   ├── AnimatorManager.cs # 动画管理器
│   │       │   ├── AudioManager.cs    # 音频管理器
│   │       │   ├── BigPrice.cs        # 大奖管理器
│   │       │   ├── GameData.cs        # 游戏数据
│   │       │   ├── GameMain.cs        # 游戏主入口
│   │       │   ├── GameObjectPool.cs  # GameObject对象池
│   │       │   ├── GameRes.cs         # 游戏资源
│   │       │   ├── HorseStateManager.cs # 马匹状态管理
│   │       │   ├── ParticleManager.cs # 粒子管理器
│   │       │   ├── ReplayTrigger.cs   # 回放触发器
│   │       │   ├── ReporterMgr.cs     # 上报管理器
│   │       │   ├── TriggerManager.cs  # 触发器管理
│   │       │   ├── UIManager.cs       # UI管理器
│   │       │   └── WinTrigger.cs      # 中奖触发器
│   │       │
│   │       ├── MVC/                   # MVC核心层
│   │       │   ├── Controller.cs      # ★ 主控制器（核心中枢）
│   │       │   ├── Model.cs           # ★ 主数据模型
│   │       │   ├── View.cs            # 主视图
│   │       │   ├── Player.cs          # 玩家实体
│   │       │   ├── PlayerMgr.cs       # 玩家管理器
│   │       │   ├── SandboxController.cs  # SBox硬件控制器
│   │       │   ├── NetMessageController.cs  # 网络消息控制器
│   │       │   ├── NetJackpotView.cs  # 网络奖池视图
│   │       │   ├── HostNetCheck.cs    # 主机网络检测
│   │       │   ├── MacInfoView.cs     # 机器信息视图
│   │       │   ├── SpineAnimationSequence.cs  # Spine动画序列
│   │       │   ├── Launcher/          # 启动器模块
│   │       │   │   ├── LauncherBase.cs       # 启动器基类
│   │       │   │   ├── Launcher_Bugly.cs     # Bugly启动器
│   │       │   │   ├── Launcher_Machine.cs   # ★ 机器初始化启动器
│   │       │   │   └── Launcher_IOT.cs       # IoT启动器
│   │       │   └── FSM/               # 老版FSM状态
│   │       │       ├── BetManager.cs       # 下注状态
│   │       │       ├── ErrorManager.cs     # 错误状态
│   │       │       ├── PrepareManager.cs   # 准备状态
│   │       │       ├── RewardManager.cs    # 奖励状态
│   │       │       └── SettingManager.cs   # 设置状态
│   │       │
│   │       ├── New/                   # 新版游戏系统
│   │       │   ├── GameModel.cs       # 游戏全局状态
│   │       │   └── GameSceneMgr.cs    # 游戏场景管理
│   │       │
│   │       ├── NewFSM/               # ★ 新版FSM游戏状态机
│   │       │   ├── GameStateMachine.cs    # 状态机主控制器
│   │       │   ├── GameStateEnterGame.cs  # 进入游戏状态
│   │       │   ├── GameStateCountDown.cs  # 倒计时状态
│   │       │   ├── GameStateStartRun.cs   # 比赛开始状态
│   │       │   ├── GameStateEndRun.cs     # 比赛结束状态
│   │       │   ├── GameStateReplay.cs     # 回放状态
│   │       │   ├── GameStateShowRoom.cs   # 展示厅状态
│   │       │   └── GameStateChangeScene.cs # 切换场景状态
│   │       │
│   │       ├── Printer/               # 打印机模块
│   │       │   ├── CryPrinter/        # Cry打印机SDK
│   │       │   └── SkiaSharp/         # SkiaSharp图形库
│   │       │
│   │       ├── Room/                  # 房间/展示模块
│   │       ├── UI/                    # UI模块
│   │       │   ├── Bet/               # 下注面板
│   │       │   ├── Running/           # 比赛界面
│   │       │   ├── ShowRoom/          # 展示厅界面
│   │       │   ├── UIPanel_History.cs # 历史记录面板
│   │       │   └── VirtualMouse.cs    # 虚拟鼠标
│   │       │
│   │       ├── Dll/                   # DLL加载
│   │       ├── Test/                  # 测试模块
│   │       ├── Main.cs                # ★★ 游戏主入口脚本
│   │       └── View.cs                # 主视图脚本
│   │
│   ├── GameRes/                       # 游戏资源（按场景组织）
│   │   ├── 0_Main/                    # 主场景资源
│   │   ├── 1_Lobby/                   # 大厅场景资源
│   │   └── 2_Game/                    # 游戏场景资源
│   │
│   ├── StreamingAssets/               # 流式资源（随包发布）
│   │   ├── AOT/                       # AOT元数据DLL
│   │   └── Hotfix/                    # 热更资源
│   │       ├── GameRes/               # AssetBundle 资源
│   │       ├── GameDll/               # 热更DLL
│   │       ├── version.json           # 版本文件
│   │       └── total_version.json     # 总版本文件
│   │
│   ├── Scenes/                        # Unity场景
│   │   ├── Hotfix.unity               # ★ 热更启动场景（Loader入口）
│   │   ├── Main.unity                 # ★ 主游戏场景
│   │   ├── Racecourse01.unity         # 赛道01场景
│   │   └── Test.unity                 # 测试场景
│   │
│   ├── Resources/                     # Unity Resources资源
│   ├── Plugins/                       # 原生插件
│   ├── Spine/                         # Spine动画系统
│   └── ...
│
├── HybridCLRData/                     # HybridCLR 热更配置
│   ├── HotUpdateDlls/                 # 热更DLL存放目录
│   ├── LocalIl2CppData-WindowsEditor/ # IL2CPP数据
│   ├── hybridclr_repo/                # HybridCLR仓库
│   └── il2cpp_plus_repo/              # IL2CPP增强仓库
│
├── HybridCLRTools/                    # HybridCLR工具
├── BuildProject/                      # 构建项目
└── ProjectSettings/                   # Unity项目设置
```

---

## 3. 架构层次

项目采用严格的分层架构，从上到下依次为：

```
┌──────────────────────────────────────┐
│        Modules 层（业务逻辑层）         │  ← 可热更新
│   MVC、新FSM、Manager、UI、Horse 等    │
├──────────────────────────────────────┤
│        Base 层（基础框架层）            │  ← 可热更新
│   网络、Timer、EventCenter、音频、IO   │
├──────────────────────────────────────┤
│        AOT 层（核心不走热更层）          │  ← 打包后不可更新
│   Singleton、AssetBundleMgr、热更流程   │
│   VersionCheck、PathHelper、Config    │
├──────────────────────────────────────┤
│        Unity Engine 层                │
│   Unity 2020.3.17f1                  │
├──────────────────────────────────────┤
│        硬件层 / 平台层                  │
│   SBox SDK、Android、IoT MQTT         │
└──────────────────────────────────────┘
```

### 3.1 AOT 层（Ahead-Of-Time）

**职责**: 提供不参与热更新的核心框架代码，是整个应用的基石。

| 组件 | 文件 | 说明 |
|------|------|------|
| 单例模式基类 | `Singleton.cs` | 线程安全的懒汉式C#单例 |
| Mono单例基类 | `MonoSingleton.cs` | 自动创建GameObject的Mono单例 |
| 管理器单例基类 | `BaseManager.cs` | 简化版单例，用于Manager类 |
| AssetBundle管理器 | `AssetBundleMgr.cs` | AB包加载/卸载/依赖管理 |
| 热更新流程 | `VersionCheck.cs` | 远程版本检测、下载、校验 |
| 启动加载器 | `Loader.cs` | 热更场景入口，调度整个启动流程 |
| 路径工具 | `PathHelper.cs` | 统一管理所有文件路径 |
| 配置管理 | `ConfigMgr.cs` | 读取Resources/Configs下的配置 |
| 场景加载器 | `SceneLoader.cs` | AssetBundle场景加载 |
| 文件工具 | `FileUtils.cs` | 文件读写/拷贝/删除操作 |
| 热更数据 | `HotfixData.cs` | 热更版本信息的静态存储 |
| 加密工具 | `CryptoHelper.cs` | MD5等加密操作 |
| 日志接口 | `ILog.cs`, `LogHelper.cs` | 统一日志输出接口 |

### 3.2 Base 层（基础框架层）

**职责**: 提供游戏通用基础能力，可参与热更新。

| 组件 | 文件 | 说明 |
|------|------|------|
| **事件中心** | `EventCenter.cs` | 观察者模式，支持泛型和无参事件分发 |
| **FSM状态机** | `FSMSystem.cs` | 老版有限状态机（全局业务状态） |
| **定时器系统** | `Timer.cs` 等 | 支持延迟/循环/帧延迟/条件循环 |
| **网络层** | `NetMgr.cs` | WebSocket Server/Client 管理器 |
| **SQLite** | `SQLite.cs`, `SQLiteHelper.cs` | 本地数据库存储 |
| **IOT支付** | `IoTPayment.cs` | 阿里云IoT MQTT支付集成 |
| **SBox桥接** | `SBoxInit.cs` 等 | SBox硬件SDK的Unity封装 |
| **资源管理** | `ResMgr.cs` | Resources加载 + AssetBundle加载 |
| **场景管理** | `ScenesMgr.cs` | 场景异步加载 |
| **音频管理** | `AudioMgr.cs`, `MusicMgr.cs` | 音效/音乐播放控制 |
| **UI管理** | `UIMgr.cs`, `BasePanel.cs` | UI面板生命周期管理 |
| **对象池** | `PoolMgr.cs` | 通用对象池 |
| **输入管理** | `InputMgr.cs` | 键盘/硬件按键输入检测 |
| **线程工具** | `Loom.cs` | 子线程与主线程通信 |
| **Tween** | `TweenUtils.cs` | 补间动画工具 |

### 3.3 Modules 层（业务逻辑层）

**职责**: 游戏核心业务逻辑，全部可热更新。

---

## 4. 启动流程

### 4.1 完整启动链路

```
Unity启动
  │
  ├── Hotfix.unity 场景加载
  │     └── Loader.Start()                       # 入口协程
  │           │
  │           ├── [Step 1] VersionCheck.DoHotfix() # ★ 热更新检查
  │           │     ├── StreamingAssets → Persistent   # 首次安装拷贝
  │           │     ├── Temp → Persistent              # 续传处理
  │           │     ├── Remote → Temp                  # 下载热更文件
  │           │     └── Temp → Persistent              # 拷贝到正式目录
  │           │
  │           ├── [Step 2] LoadMetadataForAOTAssemblies()  # 加载AOT元数据
  │           │     └── HybridCLR RuntimeApi.LoadMetadataForAOTAssembly()
  │           │
  │           ├── [Step 3] Assembly.Load()            # 加载热更DLL
  │           │     └── Base.dll, Modules.dll
  │           │
  │           ├── [Step 4] 加载AssetBundle             # game + io
  │           │
  │           └── [Step 5] SceneLoader.LoadSceneCoroutine("mainscene", "Main")
  │                 └── 加载 Main.unity 场景
  │
  ├── Main.unity 场景加载
  │     └── Main.Start()                            # 主入口协程
  │           │
  │           ├── [Step 1] Launcher_Bugly.Launch()    # 初始化崩溃上报
  │           ├── [Step 2] Launcher_Machine.Launch()  # ★ 初始化SBox硬件
  │           │     ├── SBoxInit.Init("192.168.3.57")
  │           │     ├── 等待 SBox 就绪
  │           │     ├── SBoxIdea.ReadConf()           # 读取机器配置
  │           │     ├── NetMgr.SetNetAutoConnect()    # 启动WebSocket服务
  │           │     ├── PlayerMgr.GetPlayerInfos()    # 获取玩家信息
  │           │     └── Controller.Init()             # ★ 初始化主控制器
  │           │
  │           ├── [Step 3] Launcher_IOT.Launch()     # 初始化IoT支付
  │           │
  │           ├── [Step 4] ResMgr.LoadAssetBundle()  # 加载重要资源
  │           │     ├── "game_prefab" → ImportantItemObj
  │           │     └── "io" → macInfo
  │           │
  │           └── [Step 5] ScenesMgr.LoadScene("Racecourse01")
  │                 └── GameStateMachine.Instance    # ★ 触发状态机初始化
  │
  └── 游戏主循环开始
        └── GameStateMachine.Update()
              └── fsm.OnLogic()
```

### 4.2 两个关键入口场景

| 场景 | 入口脚本 | 职责 |
|------|---------|------|
| `Hotfix.unity` | `Loader.cs` | 热更检测、AOT元数据加载、热更DLL加载、引导启动 |
| `Main.unity` | `Main.cs` | 硬件初始化、网络启动、场景加载、游戏状态机启动 |

---

## 5. 核心架构详解

### 5.1 MVC 架构

项目主体采用 MVC（Model-View-Controller）模式：

```
┌──────────────────────────────────────────────────────┐
│                    Controller                         │
│   - 事件监听与分发                                    │
│   - SBox消息处理 (SBoxEventHandle)                    │
│   - 游戏流程控制                                      │
│   - 网络消息处理                                      │
│   - 状态机调用                                        │
│   - 打印机/下注/奖励调度                              │
├──────────────────┬───────────────────────────────────┤
│      Model       │           View                    │
│   - curGame      │   - 界面初始化                     │
│   - curRound     │   - 结果显示                       │
│   - curRecord    │   - 大奖表现触发                   │
│   - records      │   - 网络奖池显示                   │
│   - errorCodeList│   - 投注更新                       │
│   - totalBets    │   - 中奖动画                       │
│   - printerState │   - 倒计时显示                     │
└──────────────────┴───────────────────────────────────┘
```

**Controller** 是全局中枢，负责：
1. 监听 SBox SDK 的回调事件（下注倒计时、回合开始/结束等）
2. 监听硬件按键事件（打印、设置、退票等）
3. 监听网络消息（分机连接/断开、网络奖池等）
4. 调度 FSM 状态转换
5. 协调 Model 数据更新和 View 表现

### 5.2 双FSM状态机架构

项目中有**两套**状态机，各自负责不同层面：

#### 老版 FSMSystem（全局业务状态）
```
                    ┌─────────┐
          ┌────────→│ Prepare │─────────┐
          │         └─────────┘         │
          │                             ↓
     ┌────┴────┐                   ┌─────────┐
     │ Setting │←────Error────────│   Bet   │
     └─────────┘                   └────┬────┘
          ↑                             │
          │         ┌─────────┐         │
          └─────────│ Reward  │←────────┘
                    └─────────┘
```
- 管理员面板设置
- 游戏准备/下注/开奖/错误处理

#### 新版 GameStateMachine（游戏场景状态）
```
EnterGame → CountDown → StartRun → EndRun → Replay → ShowRoom → ChangeScene
    ↑                                                              │
    └──────────────────────────────────────────────────────────────┘
```
- 使用 **UnityHFSM** 状态机库
- 每个状态继承 `CoState<GameState>`，支持协程式生命周期
- 管理比赛场景的视觉表现流程

### 5.3 事件系统

项目中有**两套**事件系统并存：

| 事件系统 | 实现 | 用途 |
|---------|------|------|
| `EventCenter` | 自定义观察者模式 | SBox回调、游戏内事件 |
| `Messenger` (Message) | 消息系统 | 网络层消息分发 |

**EventCenter** 核心用法：
```csharp
// 注册监听
EventCenter.Instance.AddEventListener<int>(EventHandle.CHANGE_JACKPOT, OnChangeJackpot);
// 触发事件
EventCenter.Instance.EventTrigger(EventHandle.CHANGE_JACKPOT, jackpotValue);
// 移除监听
EventCenter.Instance.RemoveEventListener<int>(EventHandle.CHANGE_JACKPOT, OnChangeJackpot);
```

**重要事件句柄（EventHandle）**：
| 事件 | 说明 |
|------|------|
| `CHANGE_JACKPOT` | 奖池金额变化 |
| `TIMERS_START` | 倒计时开始（触发下注） |
| `REWARD` | 触发奖励表现 |
| `ERROR` | 触发错误处理 |
| `BET_FINISH` | 下注结束 |
| `ACTIVE` | 错误解除后激活 |
| `KEY_DOWN` | 键盘按下（调试用） |
| `HARDWARE_KEY_CLICK` | 硬件按键点击 |
| `HARDWARE_KEY_LONG_PRESS` | 硬件按键长按 |
| `PLAYER_DISCONNECT` | 玩家断线 |
| `NETWORK_STATUS` | 网络状态变化 |

**SBox事件句柄（SBoxEventHandle）**：
| 事件 | 说明 |
|------|------|
| `SBOX_BATS_COUNT_DOWN` | 下注倒计时 |
| `SBOX_BETS_STOP` | 下注停止 |
| `SBOX_BATTLE_LEAD_START` | 引导开始（出结果） |
| `SBOX_BATTLE_LEAD_STOP` | 引导停止 |
| `SBOX_BATTLE_END_ROUND` | 回合结束 |
| `SBOX_BATTLE_NEW_ROUND` | 新回合开始 |
| `SBOX_BATTLE_GET_STATE` | 获取状态 |
| `SBOX_BATTLE_PLAYER_OUT_STATE` | 玩家输出状态 |
| `SBOX_READ_CONF` | 读取配置完成 |
| `SBOX_IDEA_VERSION` | SBox版本号 |

### 5.4 网络架构

```
┌──────────────────────┐     WebSocket      ┌──────────────────────┐
│    主机 (Host)        │←──────────────────→│    分机 (Client)      │
│                      │                    │                      │
│  ServerWS (端口33653) │                    │  ClientWS            │
│  广播 (端口10115)     │                    │  UDP广播发现 (10999)  │
│                      │                    │                      │
│  Controller          │                    │  投注界面             │
│  Model               │                    │  结果展示             │
│  物理硬件交互         │                    │                      │
└──────────┬───────────┘                    └──────────────────────┘
           │
           │ TCP WebSocket
           ↓
┌──────────────────────┐
│   网络奖池服务器       │
│  (Net Jackpot Server) │
└──────────────────────┘
```

**网络消息结构**：
```csharp
MsgInfo {
    cmd: int,      // 命令号 (C2S_CMD / S2C_CMD)
    id: int,       // 玩家ID / 机器ID
    jsonData: string // JSON数据
}
```

**消息流向**：
- 分机 → 主机：`ClientWS.SendToServer()` → `ServerWS` → `Messenger` → `Controller` 处理
- 主机 → 分机：`NetMgr.SendToClient()` → `ServerWS` → `ClientWS` → 分机处理
- 主机 → 所有分机：`NetMgr.SendToAllClient()` → 广播
- 主机 → 奖池服务器：`NetMgr.SendToServer()` → `ClientWS` → 远程服务器

### 5.5 玩家管理

**Player 实体** (`Player.cs`)：
- 唯一标识 `macId`
- 在线状态 `IsOnline`
- WebSocket连接 `client`
- 投币/退币计数（`coinInCount`/`coinOutCount`）
- SBox状态同步（`inState`/`outState`）
- 断线处理

**PlayerMgr** (`PlayerMgr.cs`)：
- 玩家字典 `playerDic`
- 从SBox获取玩家信息
- 玩家断线/重连管理
- 本地玩家标识

### 5.6 热更新系统

```
                                       ┌──────────────────┐
                                       │   远程资源服务器    │
                                       │  version.json    │
                                       │  *.dll.bytes     │
                                       │  AssetBundles    │
                                       └────────┬─────────┘
                                                │ 下载
                                                ↓
┌──────────────────────┐    拷贝      ┌──────────────────────┐
│   StreamingAssets    │─────────────→│   PersistentData     │
│   (随APK发布)        │              │   (持久化目录)        │
│                      │              │                      │
│  Hotfix/version.json │              │  Hotfix/version.json │
│  Hotfix/GameDll/*    │              │  Hotfix/GameDll/*    │
│  Hotfix/GameRes/*    │              │  Hotfix/GameRes/*    │
│  AOT/*.bytes         │              │                      │
└──────────────────────┘              └──────────┬───────────┘
                                                 │
                                                 │ 加载
                                                 ↓
                                       ┌──────────────────────┐
                                       │   HybridCLR Runtime  │
                                       │   加载热更DLL         │
                                       │   补充AOT元数据       │
                                       └──────────────────────┘
```

**热更流程（VersionCheck.DoHotfix）**：
1. **Streaming → Persistent**: 如果APP内版本 > 本地持久化版本，拷贝StreamingAssets到持久化目录
2. **Temp → Persistent**: 如果上次下载中断，续传完成拷贝
3. **Remote → Temp**: 检测远程新版本，下载到临时目录
4. **Temp → Persistent**: 下载完成后拷贝到正式目录
5. 删除多余文件

**version.json 结构**：
```json
{
  "hotfix_version": "0.0.122",
  "hotfix_key": "hf_release_android_20250819154301",
  "asset_bundle": {
    "manifest": { "hash": "..." },
    "bundle_hash": {
      "game": "...",
      "io": "...",
      ...
    }
  },
  "hotfix_dll": {
    "Base": { "hash": "..." },
    "Modules": { "hash": "..." }
  }
}
```

### 5.7 AssetBundle 系统

**AssetBundle 目录结构**：
```
Hotfix/GameRes/
├── GameRes                  # ★ Manifest AssetBundle
├── game                     # 游戏主AB
├── io                       # IO界面AB
├── game_prefab              # 预制体AB
├── game_animtor             # 动画控制器AB
├── game_fonts               # 字体AB
├── game_material            # 材质AB
├── game_model               # 模型AB
├── game_rendertexture       # RT AB
├── game_sound               # 音频AB
├── game_texture             # 贴图AB
├── mainscene                # 主场景AB
├── main                     # Main场景AB
└── *.manifest               # 各AB的manifest
```

**加载模式**：
| 模式 | 条件 | 加载方式 |
|------|------|---------|
| AssetDatabase | UNITY_EDITOR 且未定义 USE_ASSETBUNDLE | 直接读取AssetDatabase |
| AssetBundle-Simulation | 编辑器模拟模式 | 模拟AB加载 |
| AssetBundle | 真机 / USE_ASSETBUNDLE定义 | 真实AB加载 |

**AssetBundleMgr 核心API**：
```csharp
// 单个AB加载
AssetBundleMgr.Instance.LoadAssetBundleAsync("game")
// AB+依赖加载
AssetBundleMgr.Instance.LoadAssetBundleAndDepAsync("game")
// 批量加载
AssetBundleMgr.Instance.LoadAssetBundleAndDepAsync(new[] { "game", "io" })
// 加载资源
AssetBundleMgr.Instance.LoadAsset<GameObject>("game_prefab", "assetName")
```

### 5.8 定时器系统

位于 `Base/Timer/`，提供丰富的定时器类型：

| 定时器类型 | 文件 | 说明 |
|-----------|------|------|
| DelayTimer | `DelayTimer.cs` | 延迟执行一次 |
| LoopTimer | `LoopTimer.cs` | 循环执行 |
| LoopCountTimer | `LoopCountTimer.cs` | 循环指定次数 |
| LoopUntilTimer | `LoopUntilTimer.cs` | 循环到条件满足 |
| DelayFrameTimer | `DelayFrameTimer.cs` | 延迟帧数执行 |
| Timer | `Timer.cs` | 静态工厂方法 |

```csharp
// 延迟执行
Timer.DelayAction(2f, () => { /* 2秒后执行 */ });

// 循环执行
Timer.LoopAction(0.25f, (loopTimes) => { /* 每0.25秒 */ });

// 延迟后循环
Timer.DelayLoopAction(1f, 0.5f, (_) => { /* 1秒后开始每0.5秒 */ });
```

---

## 6. 游戏核心玩法

### 6.1 游戏状态流转

```
                    新回合开始
                        │
                        ↓
               ┌─────────────────┐
               │   EnterGame      │  ← 进场动画、Logo展示
               │   (进场状态)      │
               └────────┬────────┘
                        │
                        ↓
               ┌─────────────────┐
               │   CountDown      │  ← 下注倒计时
               │   (倒计时状态)    │     玩家投币/下注
               └────────┬────────┘
                        │ 倒计时结束
                        ↓
               ┌─────────────────┐
               │   StartRun       │  ← 赛马比赛开始
               │   (比赛状态)      │     马匹奔跑动画
               └────────┬────────┘
                        │ 比赛结束
                        ↓
               ┌─────────────────┐
               │   EndRun         │  ← 揭晓结果
               │   (结束状态)      │     中奖判定
               └───┬──────┬──────┘
                   │      │
           普通结果│      │特殊奖项
                   │      │
                   ↓      ↓
          ┌──────────┐ ┌──────────┐
          │  Replay  │ │ ShowRoom │ ← 大奖展示/回放
          └────┬─────┘ └────┬─────┘
               │            │
               └─────┬──────┘
                     │
                     ↓
          ┌───────────────────┐
          │   ChangeScene      │ ← 切换场景/清理
          │   (切换场景状态)    │
          └────────┬──────────┘
                   │
                   └──→ EnterGame (循环)
```

### 6.2 大奖类型

| luckyId | 奖项名称 | 对应类 | 说明 |
|---------|---------|--------|------|
| 0 | 普通 | — | 无特殊表现 |
| 4 | 幸运翻倍 | `DoubleLucky` | 积分翻倍 |
| 5 | 加倍 | — | 额外加分 |
| 6 | 明牌 | — | 提前揭晓结果 |
| 7 | 全中 | `AllPrize` | 统统有奖 |
| 8 | 彩金 | `MosaicGold`/`SpotLightPrize` | 进入小游戏赢彩金 |

### 6.3 错误码系统

| 错误码 | 说明 |
|--------|------|
| 0 | 按键档机（回合结束） |
| 1 | 本轮爆机 |
| 2 | 全台爆机（总输分爆机） |
| 3 | 盈余档机 |
| 4 | 保单箱未连接 |
| 5 | 打印机缺纸 |
| 6 | 非法开箱1 |
| 7 | 非法开箱2 |
| 8 | 运行时间到（时间打码） |
| 9 | 历史数据过大 |
| 10 | 正在打印 |
| 11 | 打印机切刀故障 |
| 12 | 打印机门未关闭 |

---

## 7. 硬件交互（SBox）

### 7.1 SBox SDK 交互流程

```
Unity (C#) 层
    │
    ├── SBoxInit.Init(matchIp)      # 初始化连接
    ├── SBoxIdea.*                  # 发送指令到硬件
    │     ├── ReadConf()            # 读取机器配置
    │     ├── BattleGetState()      # 获取战斗状态
    │     ├── BattleGameNumber()    # 获取游戏号码
    │     ├── BattleLeadStart()     # 开始引导
    │     ├── BattleEndRound()      # 结束回合
    │     ├── BattleNewRound()      # 新回合
    │     ├── BattleResetGame()     # 重置游戏
    │     ├── BattLuckPrize()       # 触发彩金
    │     ├── BattlePlayerInState() # 同步玩家状态
    │     ├── BattlePlayerOutStateListener() # 监听输出状态
    │     ├── Jackpot()             # 获取奖池
    │     ├── PrinterStateCustom()  # 获取打印机状态
    │     └── WinLockBalance()      # 获取赢锁余额
    │
    ├── SBoxSandboxListener.*       # 监听硬件事件
    │     ├── AddButtonClick()      # 按键点击
    │     ├── AddButtonDown()       # 按键按下
    │     ├── AddButtonUp()         # 按键抬起
    │     └── ...
    │
    └── SBoxSandbox.*               # 直接控制硬件
          ├── SwitchOutStateOn()    # 开灯
          ├── SwitchOutStateOff()   # 关灯
          └── Reset()               # 重置
```

### 7.2 关键硬件按键

| 按键常量 | 说明 |
|---------|------|
| `SWITCH_ENTER` | 打印/确定键 |
| `SWITCH_SET` | 设置/账单键 |
| `SWITCH_KEYBOARD_CANCLE` | 取消键 |
| `SWITCH_KEYBOARD_LITTLE_GAME` | 小游戏键 |
| `SWITCH_PAYOUT` | 退票键 |

---

## 8. IoT 支付系统

支付系统使用阿里云 IoT 平台，通过 MQTT 协议通信：

```
┌──────────────┐   MQTT    ┌──────────────┐   HTTP    ┌──────────────┐
│  SBox 机器   │←─────────→│ 阿里云IoT平台 │←────────→│   微信/支付宝  │
│  MqttSign    │           │ 设备影子      │          │   扫码支付     │
│  IoTPayment  │           │ 服务端订阅    │          │              │
└──────────────┘           └──────────────┘          └──────────────┘
```

**核心组件**：
| 组件 | 文件 | 说明 |
|------|------|------|
| MQTT客户端 | `M2MqttUnityClient.cs` | MQTT连接管理 |
| 支付管理 | `IoTPayment.cs` | 投币/退票/设备上报 |
| 数据模型 | `IOTModel.cs` | 支付数据缓存 |
| MQTT签名 | `MqttSign.cs` | 阿里云IoT签名算法 |
| 设备配置 | `BrokerSettings.cs` | MQTT Broker配置 |

---

## 9. 单例模式体系

项目中有三种单例基类，根据使用场景选择：

| 基类 | 特点 | 使用场景 |
|------|------|---------|
| `Singleton<T>` | 纯C#单例，线程安全 | 不需要MonoBehaviour的工具类 |
| `BaseManager<T>` | 纯C#单例，无锁 | Manager类（FSMSystem、Model等） |
| `MonoSingleton<T>` | GameObject单例，DontDestroyOnLoad | 需要Update/协程的组件 |

```csharp
// 使用示例
public class Model : BaseManager<Model> { }
public class AssetBundleMgr : MonoSingleton<AssetBundleMgr> { }
public class ConfigMgr : Singleton<ConfigMgr> { }
```

---

## 10. 启动器（Launcher）模式

用于游戏启动时的模块化初始化：

```csharp
// Main.cs 中的启动流程
LauncherBase[] launchers = new LauncherBase[] {
    new Launcher_Bugly(),        // 1. 崩溃上报
    new Launcher_Machine(),      // 2. SBox硬件初始化
    new Launcher_IOT(),          // 3. IoT支付初始化
};

foreach (var item in launchers) {
    yield return item.Launch(this);
    if (item.IsErr) {
        Debug.LogError(item.ErrMsg);
        yield break;            // 任一失败则终止启动
    }
}
```

每个 Launcher 支持：
- `Timeout` 超时时间
- `OnStart()` / `OnEnd()` 生命周期回调
- `OnProgress()` 异步等待
- 错误信息传递 `ErrMsg`

---

## 11. 关键技术栈

| 技术 | 用途 | 版本/说明 |
|------|------|----------|
| Unity | 游戏引擎 | 2020.3.17f1 |
| HybridCLR | 代码热更新 | 原 Huatuo |
| Newtonsoft.Json | JSON序列化 | JObject/JArray |
| UnityWebSocket | WebSocket通信 | 自定义封装 |
| Spine | 2D骨骼动画 | spine-unity |
| SkiaSharp | 2D图形渲染（打印） | 跨平台图形库 |
| CryPrinter | 热敏打印机驱动 | 自定义打印模板 |
| SQLite | 本地数据库 | 账单/记录存储 |
| UnityHFSM | 状态机库 | 新版游戏状态机 |
| M2Mqtt | MQTT客户端 | IoT支付通信 |
| Bugly | 崩溃上报 | 腾讯Bugly |
| DOTween / TweenUtils | UI动画 | 补间动画 |

---

## 12. 启动方法

### 12.1 环境要求

| 环境 | 版本/说明 |
|------|----------|
| Unity Editor | **2020.3.17f1** |
| 操作系统 | Windows 10+ (开发) / Android (部署) |
| .NET | .NET Standard 2.0 / .NET 4.x |
| JDK | 需要安装用于Android打包 |
| Android SDK/NDK | 需要配置用于IL2CPP编译 |

### 12.2 打开项目

1. 安装 **Unity Hub**，通过 Hub 安装 **Unity 2020.3.17f1**
2. 打开 Unity Hub → 点击「打开」→ 选择 `G:\RaceOstrichSolo` 目录
3. 等待 Unity 导入资源完成（首次打开可能需要较长时间）

### 12.3 编辑器运行（调试模式）

1. 确保 SBox 调试机器和电脑在同一局域网
2. 打开场景 `Assets/Scenes/Hotfix.unity`
3. 检查 `Loader.cs` 中 `Loader.Start()` 的启动流程
4. 检查 `Launcher_Machine.cs` 中 `OnStart()` 的 SBox IP 地址：
   ```csharp
   SBoxInit.Instance.Init("192.168.3.116", OnInitSBox);
   ```
   将其修改为实际调试机器的 IP 地址
5. 点击 Unity 顶部的 **Play ▶** 按钮运行

### 12.4 Android 打包

1. **先构建 HybridCLR 热更 DLL**：
   - 菜单栏 → `HybridCLR` → `Generate` → `All`
   - 将生成的 DLL 拷贝到 `HybridCLRData/HotUpdateDlls/` 目录

2. **构建 AssetBundle**：
   - 使用项目自带的 AssetBundle 构建工具
   - 将 AB 文件放到 `Assets/StreamingAssets/Hotfix/GameRes/` 目录

3. **配置 version.json**：
   - 更新 `Assets/StreamingAssets/Hotfix/version.json` 中的版本号和文件 hash

4. **Build Settings**：
   - File → Build Settings → Android
   - 确保 `Hotfix.unity` 在 Scenes In Build 的第一个位置
   - 点击 **Build** 生成 APK

### 12.5 热更新资源部署

1. 准备热更服务器（支持 HTTP 文件下载）
2. 在服务器上创建如下目录结构：
   ```
   {hotfix_url}/
   ├── version.json
   ├── GameRes/
   │   ├── GameRes              # Manifest文件
   │   ├── game
   │   ├── io
   │   └── ... (其他AB包)
   └── GameDll/
       ├── Base.dll.bytes
       └── Modules.dll.bytes
   ```
3. 更新远程 `version.json`，修改版本号使其大于本地版本
4. 更新 `Resources/Configs/AppConfig.json` 中的 `appKey` 和 `resourceServerBase`
5. 配置 `total_version.json` 指向正确的资源服务器地址

### 12.6 关键配置说明

| 配置文件 | 路径 | 用途 |
|---------|------|------|
| AppConfig | `Resources/Configs/AppConfig.json` | appKey、资源服务器地址、平台名 |
| HotfixConfig | `Resources/Configs/HotfixConfig.json` | 热更开关、热更地址等 |
| version.json | `StreamingAssets/Hotfix/version.json` | 当前包的版本和文件hash |
| total_version.json | `StreamingAssets/Hotfix/total_version.json` | 所有appKey对应的热更地址 |

### 12.7 常见问题

> ⚠️ 编辑器运行时会先走热更流程。如果 StreamingAssets 中没有 manifest AB 文件，系统会记录警告但**不会崩溃**（已修复），仅跳过 AB 拷贝，继续后续流程。

> ⚠️ 如果在编辑器中使用 `USE_ASSETBUNDLE` 宏，需要确保 StreamingAssets/Hotfix/GameRes/GameRes manifest 文件存在，否则 AssetBundleMgr 初始化时会记录警告并跳过。

> ⚠️ SBox 硬件未连接时编辑器也可以运行，但无法获取机器配置和进行游戏逻辑，相关 SBox 回调不会触发。

> ⚠️ 热更 DLL（Base.dll、Modules.dll）在编辑器模式下**不会**通过文件加载，而是直接从当前 AppDomain 获取已编译的程序集。

---

## 13. 代码规范

项目遵循以下编码规范（来自 README.md）：

- 统一使用**驼峰命名法**（camelCase）
- 使用**见名知意**的命名方式
- 类名使用 PascalCase
- 私有字段使用 `_` 前缀或直接 camelCase
- 常量使用 UPPER_SNAKE_CASE 或 PascalCase

```csharp
// 推荐写法
private DelayTimer showTimer;
private void ShowTimer(float time) {
    if (showTimer == null)
        showTimer = Timer.DelayAction(time, HideTips);
    else
        showTimer.Restart();
}
```

---

> 📅 文档生成时间：2026年7月13日  
> 📝 基于代码版本：commit `7379f58` (main 分支)  
> 🔧 最新修复：StreamingAssets 中 manifest 文件缺失时的优雅降级处理
