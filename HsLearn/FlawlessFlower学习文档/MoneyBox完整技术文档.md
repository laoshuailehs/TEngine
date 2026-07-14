# MoneyBox 项目完整技术文档

> **项目**: 游戏机台自助服务终端 (Kiosk)
> **引擎**: Unity + SBox 硬件框架 + HybridCLR 热更新
> **分支**: blizz | **日期**: 2026-06-19

---

## 目录

1. [项目概述与架构](#1-项目概述与架构)
2. [SBox 框架详解与使用指南](#2-sbox-框架详解与使用指南)
3. [网络模块详解](#3-网络模块详解)
4. [HTTP 请求/响应逻辑](#4-http-请求响应逻辑)
5. [MQTT 通信逻辑](#5-mqtt-通信逻辑)
6. [完整 API 接口文档](#6-完整-api-接口文档)
7. [如何用 SBox 框架搭建同类项目](#7-如何用-sbox-框架搭建同类项目)
8. [项目策划书模板](#8-项目策划书模板)

---

## 1. 项目概述与架构

### 1.1 项目是什么

这是一个运行在 Android 硬件机台上的**自助服务终端 (Kiosk)** 项目。主要功能：

- 玩家投入纸币 → 充值到虚拟账户 → 打印充值凭条
- 扫描二维码 → 现金兑换 → 机台自动出钞 → 打印兑换凭条
- 店员登录管理后台 → 查账/设参/打报表/注册玩家

### 1.2 整体架构

```
┌──────────────────────────────────────────────────────────────────┐
│                       Unity 应用层                                │
│                                                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  UI 面板     │  │ 游戏逻辑    │  │  热更新 DLL (HotFix)     │  │
│  │ MainGameView │  │ SandBox     │  │  Main.cs / Net.cs / ... │  │
│  │ CashExchange │  │ Printer     │  │  可远程更新, 不用重装APK │  │
│  │ BGMgr_Menu   │  │ Cassette    │  └─────────────────────────┘  │
│  └──────┬───────┘  └──────┬──────┘                               │
│         │                │                                       │
├─────────┴────────────────┴────────────────────────────────────────┤
│                     SBox 硬件中间件 (Android JNI)                   │
│  ┌──────────┐  ┌───────────┐  ┌──────────┐  ┌────────────────┐  │
│  │ SBoxIdea │  │SBoxSandbox│  │SBoxIOStream│ │  SBoxIOEvent   │  │
│  │ (算法卡) │  │(底板硬件) │  │(JNI读写)  │  │  (事件分发)    │  │
│  └────┬─────┘  └─────┬─────┘  └─────┬─────┘  └───────┬────────┘  │
│       │              │              │                 │           │
├───────┴──────────────┴──────────────┴─────────────────┴───────────┤
│                       网络通信层                                    │
│  ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐    │
│  │ HTTP POST    │  │  MQTT/WebSocket  │  │  局域网 WebSocket │    │
│  │ /login       │  │  30+ Topics      │  │  (机台间主从通信) │    │
│  │ /activate    │  │  AES加密传输     │  │  端口 7789       │    │
│  └──────┬───────┘  └────────┬─────────┘  └────────┬─────────┘    │
│         │                   │                     │              │
├─────────┴───────────────────┴─────────────────────┴──────────────┤
│                        云端服务器                                   │
│  https://cbapi.playmarsfortunes.com/machine                       │
└──────────────────────────────────────────────────────────────────┘
```

### 1.3 项目目录结构

```
Assets/
├── Scenes/
│   └── MoneyBox10801920.unity     ← 主场景 (1080×1920 竖屏)
│
├── Scripts/                       ← AOT 层 (编译进 APK, 不热更)
│   ├── Base/
│   │   ├── Net/                   ← 局域网 WebSocket (自实现RFC6455)
│   │   ├── SBox/                  ← SBox SDK 封装
│   │   ├── UnitySBox/             ← SBox Unity 适配
│   │   ├── EventCenter.cs         ← 全局事件中心
│   │   ├── UIManager.cs           ← UI 管理器
│   │   └── Timer/                 ← 定时器
│   ├── DllLoder/LoadDll.cs        ← ⭐ 热更新加载器(反射调用入口)
│   ├── Printer/                   ← 热敏打印机驱动
│   └── VersionCheck/              ← 版本检查/资源更新
│
├── HotFix/                        ← ⭐ 热更新层 (可远程更新)
│   └── Game/
│       ├── Main.cs                ← 游戏入口
│       ├── MyScript/
│       │   ├── Net.cs             ← ⭐ 网络核心 (HTTP + MQTT)
│       │   ├── MainGameView.cs    ← 主界面逻辑
│       │   ├── SandBoxScript.cs   ← 投币流程控制
│       │   ├── CashExChangePanel.cs ← 现金兑换面板
│       │   ├── PrinterScript.cs   ← 打印控制
│       │   ├── AESScript.cs       ← AES加密工具
│       │   ├── LocalOrderState.cs ← 本地订单补发
│       │   └── ShowErrorScript.cs ← 错误提示
│       └── BGMgrScript/           ← 管理后台面板
│
├── GameRes/Prefab/Panels/         ← UI 预制体
│   ├── MainPanel.prefab           ← 主界面
│   ├── LCRechangePanel.prefab     ← 设备充值面板
│   ├── ExchangePanel.prefab       ← 兑换面板
│   └── ...
│
└── Plugins/Android/               ← Android 原生插件
```

---

## 2. SBox 框架详解与使用指南

### 2.1 SBox 是什么

SBox 是由**黄文 (Huang Wen)** 开发的游戏机台硬件抽象层 SDK。它封装了 Android 底层硬件通信，提供统一的 C# API。

### 2.2 核心组件

#### 三层架构

```
Unity C# 层
    │
    ├── SBoxIdea        → 算法板通信 (游戏逻辑、配置存储、账目记录)
    ├── SBoxSandbox     → 底板通信   (投币器、退币器、打印机、开关量IO)
    │
    ├── SBoxIOStream    → Android JNI 桥接 (JSON ↔ Android Native)
    └── SBoxIOEvent     → 事件分发层   (按 cmd 路由回调)
```

#### 设备地址枚举

| 设备 | 地址值 | 说明 |
|------|--------|------|
| `SBOX_DEVICE_IDEA` | 2 | 算法卡 — 游戏逻辑运算、配置存储、账目统计 |
| `SBOX_DEVICE_SANDBOX` | 4 | 底板 — IO控制、投币/退币、纸质器、打印机 |
| `SBOX_DEVICE_CONF` | 8 | 配置存储 |
| `SBOX_DEVICE_AUTH` | 16 | 授权验证 |

#### 核心数据包结构

```csharp
// 所有硬件通信的基础
public class SBoxPacket
{
    public int cmd;      // 命令字 (如 20000=算法复位, 40000=底板复位)
    public int source;   // 源设备地址
    public int target;   // 目标设备地址
    public int[] data;   // 数据载荷 (int数组)
}
```

### 2.3 SBox 初始化流程

```csharp
// 步骤1: 挂载 SBox MonoBehaviour 到场景
if (!transform.TryGetComponent(out SBox sbox))
    gameObject.AddComponent<SBox>();

// 步骤2: 初始化 SBox
SBox.Init();
// 内部流程:
//   ├── SBoxIOStream.Init()    → Android JNI sandboxInit()
//   ├── SBoxIdea.Init()        → 注册 cmd=20001 监听 (算法主动上报)
//   └── SBoxSandbox.Init()     → 注册 cmd=40001 监听 (底板主动上报)

// 步骤3: 等待硬件就绪 (轮询)
while (!SBoxIdea.Ready())
    yield return new WaitForSeconds(0.5f);

// 步骤4: 复位硬件
SBoxIdea.Reset();     // cmd=20000 → 算法复位
// 等待回调...
SBoxSandbox.Reset();  // cmd=40000 → 底板复位
```

### 2.4 SBox API 分类

#### 算法卡 API (SBoxIdea)

```csharp
// ── 配置管理 ──
SBoxIdea.ReadConf();              // 读取游戏配置 → 返回 SBoxConfData
SBoxIdea.WriteConf(configData);   // 写入游戏配置
SBoxIdea.CheckPassword(password); // 密码验证 (6位普通/8位管理员/9位超级)
SBoxIdea.ChangePassword(password);// 修改密码

// ── 游戏流程 ──
SBoxIdea.RequestStart();         // 请求开始新一局
SBoxIdea.BetsStart(seconds);     // 开始押注 (倒计时秒数)
SBoxIdea.BetsStop();             // 停止押注
SBoxIdea.SetPlayerBets(data);    // 设定玩家押分数据

// ── 账目 ──
SBoxIdea.GetAccount();           // 读取账目 (投币/退币/上分/下分)
SBoxIdea.MovePlayerScore(ids);   // 移分
SBoxIdea.GetSummary();           // 读取算法统计数据 (多包返回)

// ── 打码 ──
SBoxIdea.RequestCoder(flag);     // 请求打码 (获取校验码)
SBoxIdea.Coder(flag, code);      // 执行打码

// ── 状态 ──
SBoxIdea.Jackpot();              // 当前彩金值
SBoxIdea.RemainMinute();         // 剩余运行分钟数
SBoxIdea.NeedActivated();        // 是否需要激活 (非0=需要)
SBoxIdea.IsMachineIdReady();     // 机台号是否已设定
SBoxIdea.WinLockBalance();       // 盈利当机余额
```

#### 底板 API (SBoxSandbox)

```csharp
// ── 投币/退币 ──
int coins = SBoxSandbox.NumberOfCoinIn(0);   // 读取投币计数 (读取后自动清零)
SBoxSandbox.CoinOutStart(id, count, type);   // 启动退币
SBoxSandbox.CoinOutStop(id);                  // 停止退币
bool timeout = SBoxSandbox.IsCoinOutTimeout(0);// 退币是否超时

// ── 开关量 IO ──
ulong state = SBoxSandbox.SwitchInState();     // 读取所有输入端口状态
SBoxSandbox.SwitchOutStateOn(bitmask);         // 开启输出端口 (多个bit同时设)
SBoxSandbox.SwitchOutStateOff(bitmask);        // 关闭输出端口

// ── 纸币器 ──
SBoxSandbox.BillListGet();                     // 获取支持的纸币器型号列表
SBoxSandbox.BillSelect(index);                 // 选择纸币器型号
SBoxSandbox.BillApprove();                     // 核准收币
SBoxSandbox.BillReject();                      // 拒收纸币
int credit = SBoxSandbox.BillCredit();         // 当前纸币面额 (读取后清零)
bool stacked = SBoxSandbox.IsBillStacked();   // 纸币是否已存入钱箱 (读取后复位)
int billState = SBoxSandbox.BillState();       // 纸币器连接状态

// ── 打印机 ──
SBoxSandbox.PrinterListGet();                  // 获取支持的打印机型号列表
SBoxSandbox.PrinterSelect(index);              // 选择打印机
SBoxSandbox.PrinterMessage(text);              // 打印文字并切纸
SBoxSandbox.PrinterPaperCut();                 // 单独切纸
SBoxSandbox.PrinterFontSize(size);             // 设置字体大小
SBoxSandbox.PrinterReset();                    // 重置打印机
int state = SBoxSandbox.PrinterState();        // 打印机状态

// ── 码表 ──
SBoxSandbox.MeterSet(id, count, type);         // 走码表 (投币/退币/上分/下分码表)

// ── 电机 ──
SBoxSandbox.MotorTouch(milliseconds);          // 电机振动
bool busy = SBoxSandbox.IsMotorBusy();         // 电机是否忙
bool connected = SBoxSandbox.IsMotorConnected();// 电机是否连线

// ── 时间 ──
SBoxSandbox.GetDateTime();                     // 读取底板时间
SBoxSandbox.SetDateTime(dateTime);             // 设置底板时间

// ── 序列号 ──
SBoxSandbox.USN();         // 读取底板硬件唯一序列号
SBoxSandbox.Version();     // 读取底板软件版本
SBoxSandbox.DeviceId();    // 设备ID号

// ── 轮盘机芯 ──
SBoxSandbox.RouletteCtrl(id, data);     // 开始/停止押分/开奖结束
SBoxSandbox.RouletteMotorMode(mode);    // 电机模式 (加速-减速/恒速/减速)
SBoxSandbox.RouletteRun(isRun);         // 启停轮盘
SBoxSandbox.RouletteTouch();            // 开球
SBoxSandbox.RouletteResult();           // 查询开球结果
SBoxSandbox.RouletteState();            // 读取机芯状态
SBoxSandbox.RouletteLedMode(idx,bg,fg); // 跑灯
SBoxSandbox.RouletteResultColor(color); // 开奖颜色

// ── 顶球机芯 ──
SBoxSandbox.EjectState();               // 读取状态
SBoxSandbox.EjectOpen();                // 开牌
SBoxSandbox.EjectClose();               // 收牌
SBoxSandbox.EjectReset();               // 复位
SBoxSandbox.EjectResultNumber(num);     // 顶出指定开奖球号
SBoxSandbox.EjectNumberSet(num);        // 配置球号
```

### 2.5 SBox 主循环

```csharp
// SBox.cs 的 Update() — 每帧执行
void Update()
{
    // ① 处理 IO 流
    SBoxIOStream.Exec();

    // ② 处理底板事件 (开关状态变化检测)
    SBoxSandbox.Exec(millisecond);

    // ③ 处理算法卡事件
    SBoxIdea.Exec(millisecond);

    // ④ 消费事件队列 (最多 50 条/帧, 防止卡死)
    int counter = 0;
    while (counter++ < 50)
    {
        SBoxPacket packet = SBoxIOStream.Read();  // 从 Android JNI 读取
        if (packet != null)
            SBoxIOEvent.SendEvent(packet.cmd, packet);  // 按 cmd 分发回调
        else
            break;
    }
}
```

### 2.6 SBox 请求-响应模式

```csharp
// 标准请求-响应模式 (以读取算法配置为例)

// ① 发起请求
public static void ReadConf()
{
    // 构造请求包
    SBoxPacket packet = new SBoxPacket(cmd: 20002, source: 1, target: 2, size: 2);
    packet.data[0] = 0;
    packet.data[1] = 0;

    // 注册响应回调 (key = cmd)
    SBoxIOEvent.AddListener(packet.cmd, OnReadConfResponse);

    // 发送到硬件
    SBoxIOStream.Write(packet);
}

// ② 响应回调 (由主循环 SBoxIOEvent.SendEvent 触发)
private static void OnReadConfResponse(SBoxPacket packet)
{
    SBoxConfData conf = new SBoxConfData();
    conf.result = packet.data[0];
    conf.MachineId = packet.data[6];   // 机台编号
    conf.CoinValue = packet.data[11];  // 投币比例
    // ... 解析更多字段

    // ③ 通知应用层
    EventCenter.Instance.EventTrigger(SBoxEventHandle.SBOX_READ_CONF, conf);
}
```

---

## 3. 网络模块详解

### 3.1 网络架构总览

```
                         ┌──────────────────┐
                         │   云端服务器       │
                         │ cbapi.xxx.com     │
                         └───┬──────┬───────┘
                             │      │
                    HTTP POST│      │ MQTT/WebSocket
                      /login │      │ (wss://)
                    /activate│      │
                             │      │
                    ┌────────┴──────┴───────┐
                    │       机台 (Kiosk)     │
                    │                       │
                    │  Net.cs (网络总控)     │
                    │  ├─ HTTP登录          │
                    │  ├─ MQTT连接/订阅/发布│
                    │  └─ AES加密/解密      │
                    │                       │
                    │  NetMgr.cs (局域网)    │
                    │  ├─ WebSocket Server  │
                    │  ├─ WebSocket Client  │
                    │  └─ UDP 设备发现      │
                    └───────────────────────┘
```

### 3.2 网络连接状态机

```
               ┌──────────────────┐
               │    应用启动       │
               └────────┬─────────┘
                        │
               ┌────────▼─────────┐
               │  获取 MachineCode │
               │ USN→MAC→UUID     │
               └────────┬─────────┘
                        │
               ┌────────▼─────────┐
               │ HTTP POST /login │◄──── 失败 → 6秒后重试
               └────────┬─────────┘
                        │
            ┌───────────┼───────────┐
            │           │           │
        Code=0    Code=555/557  其他错误
            │      未激活          │
            │           │           │
    ┌───────▼──┐ ┌─────▼──────┐    │
    │MQTT连接  │ │打开激活界面│    │
    │Publish   │ └────────────┘    │
    │Token认证 │                   │
    │Subscribe │                   │
    └───────┬──┘                   │
            │                      │
    ┌───────▼──┐                   │
    │NetCompete│                   │
    │= true    │                   │
    └───────┬──┘                   │
            │                      │
    ┌───────▼──┐                   │
    │显示主界面│                   │
    │功能就绪  │                   │
    └──────────┘                   │
            │                      │
            └──────────────────────┘ (重试)
```

### 3.3 MachineCode 获取策略

```csharp
// 机台唯一标识，4 级降级策略
获取顺序:
  1️⃣ 本地缓存文件
     ↓ 不存在
  2️⃣ SBoxSandbox.USN() 硬件唯一序列号 (重试4次, 每次间隔1秒)
     ↓ 获取失败
  3️⃣ GetMacAddress() 网卡MAC地址
     ├── Windows: NetworkInterface.GetAllNetworkInterfaces()
     └── Android: CassetteScript.javaObject.Call("GetMacAddress")
     ↓ 获取失败
  4️⃣ Guid.NewGuid() 随机UUID
     格式: "u:" + uuid

// 获取成功后缓存到本地: MachineCodeBackup.json
```

### 3.4 加密体系

```
┌─────────────────────────────────────────────────────────────┐
│  两层密钥体系                                                 │
│                                                             │
│  第一层: 固定密钥 (PostBase64Key)                             │
│    "nSFQxn9+lZBLu1by7E9ibvZEPljhvMC3GQo9plFR9RI="         │
│    用途: HTTP 登录 / 激活 / MQTT 首次认证                    │
│    来源: 代码硬编码                                          │
│                                                             │
│  第二层: 动态密钥 (dynamicKey)                               │
│    用途: 所有后续 MQTT 业务通信                               │
│    来源: HTTP 登录响应中服务器下发                             │
│                                                             │
│  AES-256-CBC 加密流程:                                       │
│    ① 生成随机 IV (16字节)                                    │
│    ② AES加密: ciphertext = AES_CBC(plaintext, key, iv)     │
│    ③ 输出: Base64(IV + ciphertext)                          │
│                                                             │
│  解密流程:                                                    │
│    ① Base64解码                                              │
│    ② 提取前16字节作为IV                                       │
│    ③ AES解密: plaintext = AES_CBC(ciphertext, key, iv)     │
└─────────────────────────────────────────────────────────────┘
```

### 3.5 局域网 WebSocket 通信 (NetMgr)

```
主机-分机架构:

  主机 (Host)                           分机 (Client)
  ┌─────────────────┐                  ┌─────────────────┐
  │ UDP Server      │                  │ UDP Client      │
  │ :18000          │←── 广播发现 ────│ → 255.255.255.255
  │                 │  回复主机IP:端口 →│                 │
  │ WebSocket Server│                  │                 │
  │ :7789           │←══ WebSocket ═══│ 连接 ws://主机IP│
  │                 │   心跳每3秒      │                 │
  └─────────────────┘                  └─────────────────┘

  消息格式: { cmd: int, id: int, jsonData: string }
  命令字:
    C2S_HeartHeat = 200  (客户端心跳)
    C2S_Login = 201       (客户端登录)
    C2S_CassetteConmucate = 202 (钱箱通信)
    S2C_HeartHeat = 100   (服务器心跳响应)
```

---

## 4. HTTP 请求/响应逻辑

### 4.1 接口列表

| 接口 | 方法 | URL | 超时 | 用途 |
|------|------|-----|------|------|
| 登录 | POST | `{TargetIP}/login` | 5s | 机台登录，获取 MQTT 参数 |
| 激活 | POST | `{TargetIP}/activate` | 5s | 激活新机台 |

### 4.2 登录接口详解

#### 请求

```http
POST {TargetIP}/login
Content-Type: application/x-www-form-urlencoded
Timeout: 5s
SSL: 不验证证书 (BypassCertificate)

Body:
  ctxt={AES加密的Base64字符串}
  lang=en
```

#### 请求加密前原文

```json
{
  "machineCode": "10FFE00E46240",  // 机台唯一标识
  "lang": "en"
}
```

#### 加密过程

```csharp
// ① 序列化
string json = JsonConvert.SerializeObject(new PostDataStruct {
    machineCode = TargetMachineCode,
    lang = "en"
});

// ② AES-256-CBC 加密 (固定密钥)
byte[] iv = AESScript.GenerateIv();  // 随机16字节IV
string encrypted = AESScript.Encrypt(json, PostBase64Key, iv);

// ③ 构造表单
WWWForm form = new WWWForm();
form.AddField("ctxt", encrypted);
form.AddField("lang", "en");
```

#### 响应 (解密前)

```json
{
  "Code": 0,
  "ErrMsg": "",
  "Action": "",
  "Data": {
    "ctxt": "AES加密的Base64字符串...",
    "accessToken": "",
    "mqttBroker": "",
    "dynamicKey": "",
    "bankKey": ""
  }
}
```

#### 响应 ctxt 解密后

```json
{
  "accessToken": "eyJhbGciOi...",           // MQTT 登录 Token
  "mqttBroker": "wss://mqtt.xxx.com/mqtt",  // MQTT Broker 地址
  "dynamicKey": "abc123...",                 // 后续通信的 AES 密钥
  "bankKey": "def456..."                    // 银行卡二维码密钥
}
```

#### 响应 Code 处理

| Code | 含义 | 处理 |
|------|------|------|
| **0** | 登录成功 | 关闭加载提示 → 连接 MQTT → 显示主界面 |
| **555** | 机台未激活 | 打开 ActivateMachinePanel |
| **557** | 机台被禁用 | 打开 ActivateMachinePanel |
| **41801** | 激活码不存在 | 打开 ActivateMachinePanel |
| **其他** | 未知错误 | 显示错误弹窗，6秒后重试 |

### 4.3 激活接口详解

#### 请求

```http
POST {TargetIP}/activate
Body:
  ctxt={AES加密}
  lang=en
```

#### 加密前原文

```json
{
  "activationCode": "店员输入的激活码",
  "machineCode": "10FFE00E46240",
  "lang": "en"
}
```

#### 响应 Code

| Code | 含义 |
|------|------|
| **0** | 激活成功 → 自动重新登录 |
| **556** | 激活码无效 (错误的码) |
| **41801** | 激活码不存在 |
| 其他 | 显示错误消息 |

### 4.4 HTTP 响应处理流程图

```
UnityWebRequest.SendWebRequest()
              │
              ▼
    ┌─── result 判断 ───┐
    │                   │
ConnectionError    Success
ProtocolError       │
DataProcessingError  │
    │                ▼
    │          downloadHandler.text
    │                │
    ▼          JsonConvert.DeserializeObject<PostMesg>
显示网络错误           │
6秒后重试      ┌───────┴───────┐
               │               │
          ctxt != null     ctxt == null
               │               │
          AES解密ctxt      直接使用Data
               │               │
          ┌────┴────┐          │
          ▼         ▼          ▼
    Code处理分支:  解析 data    解析 data
    ┌──────┼──────┐
    0    555/557  其他
    │      │       │
  成功  跳激活   显示错误
```

---

## 5. MQTT 通信逻辑

### 5.1 MQTT 连接参数

| 参数 | 来源 | 说明 |
|------|------|------|
| Broker 地址 | `mesg.Data.mqttBroker` | HTTP /login 响应中获取 |
| 连接方式 | WebSocket | MQTT over WebSocket |
| 超时 | 3000ms | 连接超时时间 |
| Clean Session | true | 不保留离线消息 |
| 认证方式 | Token | Publish 到 `machine/HD_login/1` |
| Heartbeat | MQTT KeepAlive | 协议层心跳 |

### 5.2 MQTT 连接与认证流程

```csharp
public static async Task Connect_Client_Using_WebSockets()
{
    // ① 检查是否已连接
    if (mqttClient.IsConnected) return;

    // ② 建立 MQTT over WebSocket 连接
    var options = new MqttClientOptionsBuilder()
        .WithWebSocketServer(o => o.WithUri(mesg.Data.mqttBroker))
        .WithTimeout(TimeSpan.FromMilliseconds(3000))
        .WithCleanSession(true)
        .Build();
    await mqttClient.ConnectAsync(options);

    // ③ 发送 Token 认证
    await MQTTPublic("machine/HD_login/1",
        JsonConvert.SerializeObject(new { token = mesg.Data.accessToken }));

    // ④ 标记网络就绪
    NetCompete = true;
    CloseNetErrorPanel();

    // ⑤ 等3秒后补发本地订单
    await Task.Delay(3000);
    LocalOrderState.StartResendAction();

    // ⑥ 订阅登录响应 Topic
    await mqttClient.SubscribeAsync("machine/HD_login/1");
}
```

### 5.3 MQTT 消息发送流程 (MQTTPublic)

```
MQTTPublic(Topic, Payload)
        │
        ├── ① 检查 MQTT 是否连接
        │      └── 未连接 → 放弃发送
        │
        ├── ② 重置接收状态
        │      isReceived[Topic] = false
        │
        ├── ③ 选择加密密钥
        │      Topic == "machine/HD_login/1" → PostBase64Key
        │      其他 Topic → mesg.Data.dynamicKey
        │
        ├── ④ AES 加密 Payload
        │      IV = 随机生成 (每次不同)
        │      ctxt = AES_Encrypt(Payload, Key, IV)
        │      包装: { ctxt: "Base64密文", lang: "en" }
        │
        ├── ⑤ 构建 MQTT 消息
        │      MqttApplicationMessageBuilder()
        │        .WithTopic(Topic)
        │        .WithPayload(encryptedJson)
        │        .Build()
        │
        ├── ⑥ PublishAsync
        │      │
        │      ├── 成功 → 启动 "发布成功但接收失败" 守护任务
        │      │         PublicSuccessReciveFail(loomParm)
        │      │         等待15秒, 前6秒静默, 后9秒检查
        │      │
        │      └── 失败 → 显示发布失败错误
        │
        └── ⑦ 调用方等待接收
               await awaitReciveMsg(Topic) // 最多等3.6秒
```

### 5.4 MQTT 消息接收流程

```
mqttClient.ApplicationMessageReceivedAsync 事件触发
        │
        ├── ① 提取 Payload 字节 → UTF8 字符串
        │
        ├── ② JSON 反序列化为 ReturnRoot
        │      { Action, Code, Data: { ctxt }, ModuleType }
        │
        ├── ③ 解密 (如果 ctxt 非空)
        │      Key = Action=="HD_login" ? PostBase64Key : dynamicKey
        │      decrypted = AES_Decrypt(ctxt, Key)
        │      → 反序列化 → 合并到 retData.Data
        │
        ├── ④ 特殊 Action 处理
        │      "HD_login" → 保存充值/兑换限额、广告URL、机台编号
        │      "HD_FindWithdrawalOrder" → 反序列化 bankActionData
        │
        ├── ⑤ 取消守护任务
        │      CancelTask(ModuleType + "/" + Action + "/1")
        │
        ├── ⑥ 特殊错误码处理
        │      Code=11111 → 服务器繁忙 (重试)
        │      Code=42801 → 后台登录过期 (弹窗)
        │
        └── ⑦ 标记已接收
               isReceived[ModuleType + "/" + Action] = true
```

### 5.5 可靠传输机制

```
PublicWithRe(Topic, Payload) — 带重试的发布
        │
        ├── 重试循环 (最多 3 次)
        │     │
        │     ├── MQTTPublic(Topic, Payload)
        │     ├── await awaitReciveMsg(Topic)  ← 等待3.6秒
        │     │
        │     ├── 收到响应 → ✅ 关闭错误面板 → 退出
        │     │
        │     ├── Code=11111 → 服务器忙, 继续下一次重试
        │     │
        │     └── 最后一次重试仍失败:
        │           ├── 断网? → Code=99999
        │           ├── MQTT断连? → 无限循环重连 (HTTP登录→MQTT连接→5.6秒等待)
        │           └── 发布成功但未收到? → Code=88888
        │
        └── 守护任务: PublicSuccessReciveFail
              15秒内若未收到 → 通过主线程事件重新发送

特殊错误码:
  99999 = 断网
  88888 = 发布成功但没收到服务器响应
  11111 = 服务器繁忙
  42801 = 后台登录过期
```

### 5.6 消息加密密钥选择逻辑

```csharp
// MQTTPublic 中的加密逻辑
string Base64Key;
if (Topic == "machine/HD_login/1")
    Base64Key = PostBase64Key;         // ← 登录时: 固定密钥(硬编码)
else
    Base64Key = mesg.Data.dynamicKey;  // ← 登录后: 动态密钥(服务器下发)

// 接收解密时的逻辑
string Base64Key;
if (retData.Action == "HD_login")
    Base64Key = PostBase64Key;          // ← 登录响应: 固定密钥
else
    Base64Key = mesg.Data.dynamicKey;   // ← 其他响应: 动态密钥
```

### 5.7 完整 Topic 目录

```
                        MQTT Topics

machine (机台模块)
├── machine/HD_login/1                 ← ⭐ 机台登录/认证
├── machine/HD_NotifyDoorStatus/1      ← 上报钱箱门开关状态
├── machine/HD_ReportIcm/1             ← 上报纸币器配置(SetBill)
├── machine/HD_GetIcm/1                ← 获取纸币器配置(GetBill)
├── machine/HD_SetPrinter/1            ← 设置打印机
└── machine/HD_GetPlats/1              ← 获取平台/游戏列表

order (订单模块)
├── order/HD_NewPrcode/1               ← ⭐ 生成充值预订单号
├── order/HD_CPRO/1                    ← 取消充值订单
├── order/HD_CMRO/1                    ← 创建设备充值订单
├── order/HD_GetGameMachineList/1      ← 获取游戏机台列表
├── order/HD_FindWithdrawalOrder/1     ← ⭐ 验证兑换二维码
├── order/HD_ConfirmWithdrawalOrder/1  ← ⭐ 确认兑换(出钞)
├── order/HD_FinishWithdrawalOrder/1   ← ⭐ 完成兑换
├── order/HD_ConsumeMcBindCode/1       ← 绑定设备
├── order/HD_CheckPerCashIn/1          ← ⭐ 逐张纸币验证
├── order/HD_ConfirmPerCashIn/1        ← 确认收钞
├── order/HD_GetGameList/1             ← 获取游戏列表
└── order/HD_GetArcadeDevices/1        ← 获取街机设备

player (玩家模块)
├── player/HD_GetPlayer/1              ← 验证玩家账户
└── player/HD_CreateManyPlayers/1      ← 批量创建玩家

admin (管理后台模块)
├── admin/HD_Stats_Dashboard/1         ← 营收总览
├── admin/HD_Stats_GetRechargeList/1   ← 充值记录
├── admin/HD_Stats_GetWithdrawalList/1 ← 兑换记录
├── admin/HD_Login/1                   ← 后台登录
├── admin/HD_ChangeLoginPassword/1     ← 修改密码
├── admin/HD_ResetRemainingCashCount/1 ← 设置钱箱剩余数
├── admin/HD_ResetWarningCashCount/1   ← 设置钱箱预警数
├── admin/HD_GetStoreInfo/1            ← 获取店铺信息
├── admin/HD_SetStoreInfo/1            ← 设置店铺信息
├── admin/HD_NotifyCashOut/1           ← 测试退钞上报
├── admin/HD_GetCashBoxSettings/1      ← ⭐ 获取钱箱配置
├── admin/HD_GetPerms/1                ← 获取权限
├── admin/HD_SyncCashType/1            ← 同步钱箱面额
├── admin/HD_GetMicroCardList_v2/1     ← 获取小卡列表
├── admin/HD_UnbindMicroCard/1         ← 解绑小卡
├── admin/HD_Logout/1                  ← 退出登录
├── admin/HD_Stats_GetProfitReport/1   ← 利润报表
└── admin/HD_Stats_GetDoorStatusList/1 ← 门状态列表
```

---

## 6. 完整 API 接口文档

### 6.1 登录接口

```
POST {server}/login

请求 (AES加密前):
{
  "machineCode": "10FFE00E46240",
  "lang": "en"
}

响应 (AES解密后):
{
  "Code": 0,
  "Data": {
    "accessToken": "eyJ...",
    "mqttBroker": "wss://mqtt.xxx.com",
    "dynamicKey": "abc123...",
    "bankKey": "def456..."
  }
}
```

### 6.2 MQTT 核心 Topic 请求/响应格式

#### 生成充值预订单

```
Publish → order/HD_NewPrcode/1
Payload: {}  (空对象)

响应:
{
  "Code": 0,
  "Data": {
    "prcode": "PRC20240619...",       // 预订单号
    "rangeInfo": {
      "maxRecharge": 10000,             // 最大充值额
      "minRecharge": 10,                // 最小充值额
      "maxWithdraw": 5000,              // 最大兑换额
      "minWithdraw": 10,                // 最小兑换额
      "dailyRedeemLimit": 100000        // 每日兑换上限
    }
  }
}
```

#### 逐张纸币验证

```
Publish → order/HD_CheckPerCashIn/1
Payload:
{
  "platTag": "MF",
  "seq_num": 1,            // 序号(第几张纸币)
  "amount": 100,           // 面额
  "prcode": "PRC...",      // 预订单号
  "lang": "en",
  "playerId": "",
  "rechargeScene": "device",
  "gameMachineId": "",
  "gameMachineSeat": 0,
  "gameMachineName": "",
  "platformId": 0
}

响应: { "Code": 0 }  // 0=验证通过, 非0=拒收
```

#### 执行兑换 (确认出钞)

```
Publish → order/HD_ConfirmWithdrawalOrder/1
Payload:
{
  "amount": 500,
  "orderno": "ORD20240619...",
  "isLocal": "0",
  "lang": "en"
}

响应:
{
  "Code": 0,
  "Data": {
    "outMoneyMethod": [4, 0, 0, 0],     // 出钞方案: 每种面额出几张
    "methodCount": "1",
    "rangeInfo": {
      "maxRecharge": 10000,              // 更新后的充值上限
      "minRecharge": 10,
      "maxWithdraw": 5000,               // 更新后的兑换上限
      "minWithdraw": 10
    }
  }
}
```

#### 验证兑换二维码

```
Publish → order/HD_FindWithdrawalOrder/1
Payload:
{
  "codeContent": "qr_code:xxx...",   // 扫码枪读取的完整内容
  "lang": "en"
}

响应:
{
  "Code": 0,
  "Data": {
    "actionData": "{                    // BankActionData JSON
      \"orderno\": \"ORD...\",
      \"codeType\": 1,                  // 0=绑定, 1=标准兑换, 2=二次兑换
      \"totalMoney\": 500,
      \"availMoney\": 300,
      \"restMoney\": 200,
      \"presetAmount\": \"100;200;500;1000\",
      \"deviceID\": \"\"
    }",
    "rangeInfo": { "dailyRedeemLimit": 100000 }
  }
}
```

#### 完成兑换

```
Publish → order/HD_FinishWithdrawalOrder/1
Payload:
{
  "orderno": "ORD...",
  "cdmDispense": { ... },     // 出钞结果
  "cashBoxes": [ ... ],       // 钱箱状态快照
  "lang": "en"
}

响应:
{
  "Code": 0,
  "Data": {
    "isPartialWithdrawal": "2",        // "2"=部分出钞(钱不够)
    "remainingMoney": "200",           // 剩余未出金额
    "newOrder": {
      "codeContent": "qr_code:...",    // 新二维码(剩余金额)
      "orderno": "ORD2..."
    }
  }
}
```

#### 获取钱箱设置

```
Publish → admin/HD_GetCashBoxSettings/1
Payload: { "lang": "en" }

响应:
{
  "Code": 0,
  "Data": {
    "remainingCount": "[100, 50, 20, 0]",   // 4个钱箱的剩余钞票数
    "warningCount": "[20, 10, 5, 0]",        // 4个钱箱的预警阈值
    "denomination": "[100, 50, 20, 10]"      // 4个钱箱的面额
  }
}
```

---

## 7. 如何用 SBox 框架搭建同类项目

### 7.1 最小可运行项目结构

```
Assets/
├── Scenes/MainScene.unity
├── Scripts/
│   ├── Framework/
│   │   ├── SBox/                  # ← 从 SDK 复制: SBox.cs, SBoxIOStream.cs 等
│   │   ├── EventCenter.cs         # ← 事件系统
│   │   ├── MonoSingleton.cs       # ← Mono单例
│   │   └── UIManager.cs           # ← UI管理
│   ├── Game/
│   │   ├── Main.cs                # ← 入口
│   │   ├── NetManager.cs          # ← HTTP + MQTT
│   │   └── SandBoxHandler.cs      # ← 底板事件处理
│   └── UI/
│       └── MainPanel.cs           # ← 主界面
└── GameRes/Prefabs/
    └── MainPanel.prefab
```

### 7.2 第一步: 创建 SBox 初始化脚本

```csharp
// SBoxBootstrap.cs — 挂载到场景根节点
public class SBoxBootstrap : MonoSingleton<SBoxBootstrap>
{
    public void Init(UnityAction onComplete)
    {
        // 1. 挂载 SBox 组件
        if (!GetComponent<SBox>())
            gameObject.AddComponent<SBox>();

        // 2. 初始化 (Android 真机)
        if (Application.platform == RuntimePlatform.Android)
            SBox.Init();

        // 3. 等待硬件就绪
        StartCoroutine(WaitForHardware(onComplete));
    }

    IEnumerator WaitForHardware(UnityAction onComplete)
    {
        // 等待算法卡连接
        while (!SBoxIdea.Ready())
            yield return new WaitForSeconds(0.5f);

        // 复位算法卡
        SBoxIdea.Reset();
        yield return new WaitForSeconds(1);

        // 等待底板连接
        while (!SBoxSandbox.Ready())
            yield return new WaitForSeconds(0.5f);

        // 复位底板
        SBoxSandbox.Reset();
        yield return new WaitForSeconds(1);

        onComplete?.Invoke();
    }
}
```

### 7.3 第二步: 创建游戏入口

```csharp
// GameMain.cs
public class GameMain : MonoBehaviour
{
    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // 真机: 先初始化硬件
            SBoxBootstrap.Instance.Init(() =>
            {
                InitializeGame();
            });
        }
        else
        {
            // 编辑器: 直接启动
            InitializeGame();
        }
    }

    void InitializeGame()
    {
        // 打开主界面
        UIManager.Instance.OpenPanel("MainPanel", Vector2.zero);

        // 挂载业务组件
        gameObject.AddComponent<SandBoxHandler>();
        gameObject.AddComponent<NetManager>();
    }
}
```

### 7.4 第三步: 实现底板事件处理

```csharp
// SandBoxHandler.cs
public class SandBoxHandler : MonoBehaviour
{
    // ── 纸币器相关 ──
    void Start()
    {
        // 获取纸币器列表
        SBoxSandbox.BillListGet();
    }

    void Update()
    {
        // 每帧轮询硬件事件
        PollHardwareEvents();
    }

    void PollHardwareEvents()
    {
        // 检查投币
        int coinCount = SBoxSandbox.NumberOfCoinIn(0);
        if (coinCount > 0)
        {
            Debug.Log($"投币: {coinCount}个");
            // TODO: 处理投币业务
        }

        // 检查纸币
        int credit = SBoxSandbox.BillCredit();
        if (credit > 0)
        {
            Debug.Log($"收到纸币: {credit}元");
            OnBillReceived(credit);
        }

        // 检查纸币是否存入钱箱
        if (SBoxSandbox.IsBillStacked())
        {
            Debug.Log("纸币已存入钱箱");
            OnBillStacked();
        }

        // 检查硬件按钮
        ulong switchState = SBoxSandbox.SwitchInState();
        if ((switchState & SBOX_SWITCH.SWITCH_SET) != 0)
        {
            OnSettingButtonPressed();
        }
    }

    void OnBillReceived(int amount)
    {
        // ① 请求预订单号
        // ② 验证金额上限
        // ③ 逐张验证
        // ④ 核准收币
        // ...
    }

    void OnBillStacked()
    {
        // ① 累加充值金额
        // ② 保存本地订单
        // ③ 触发打印
        // ...
    }
}
```

### 7.5 第四步: 实现网络管理器

```csharp
// NetManager.cs
public class NetManager : MonoBehaviour
{
    IMqttClient mqttClient;

    void Start()
    {
        // 获取 MachineCode
        string machineCode = GetMachineCode();

        // HTTP 登录
        StartCoroutine(HttpLogin(machineCode));
    }

    string GetMachineCode()
    {
        // 策略1: 本地缓存
        string cachePath = Application.persistentDataPath + "/MachineCode.json";
        if (File.Exists(cachePath))
            return File.ReadAllText(cachePath);

        // 策略2: SBox USN
        SBoxSandbox.USN();
        // ... 等待回调 ...

        // 策略3: MAC 地址
        return GetMacAddress();
    }

    IEnumerator HttpLogin(string machineCode)
    {
        // ① AES 加密
        string json = JsonConvert.SerializeObject(new {
            machineCode = machineCode,
            lang = "en"
        });
        string encrypted = AESCrypto.Encrypt(json, FixedKey);

        // ② 发送请求
        WWWForm form = new WWWForm();
        form.AddField("ctxt", encrypted);
        form.AddField("lang", "en");

        UnityWebRequest request = UnityWebRequest.Post(ServerUrl + "/login", form);
        request.timeout = 5;
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ③ 解析响应
            var mesg = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);

            if (mesg.Code == 0)
            {
                // ④ 解密动态密钥
                var data = JsonConvert.DeserializeObject<LoginData>(
                    AESCrypto.Decrypt(mesg.Data.ctxt, FixedKey));

                // ⑤ 连接 MQTT
                yield return ConnectMQTT(data.mqttBroker, data.accessToken);
            }
        }
    }

    async Task ConnectMQTT(string broker, string token)
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithWebSocketServer(o => o.WithUri(broker))
            .WithTimeout(TimeSpan.FromSeconds(3))
            .Build();

        await mqttClient.ConnectAsync(options);

        // 发送 Token
        await PublishEncrypted("machine/HD_login/1",
            JsonConvert.SerializeObject(new { token }));

        // 订阅
        await mqttClient.SubscribeAsync("machine/HD_login/1");
    }

    public async Task PublishEncrypted(string topic, string payload)
    {
        // AES 加密
        string encrypted = AESCrypto.Encrypt(payload, DynamicKey);
        var wrapper = new { ctxt = encrypted, lang = "en" };

        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonConvert.SerializeObject(wrapper))
            .Build();

        await mqttClient.PublishAsync(msg);
    }
}
```

### 7.6 关键设计模式总结

| 模式 | 实现 | 用途 |
|------|------|------|
| 请求-响应 | `SBoxIOEvent.AddListener(cmd, callback)` | SBox 硬件通信 |
| 发布-订阅 | MQTT Publish/Subscribe | 与云服务器通信 |
| 事件驱动 | `EventCenter.Instance.EventTrigger/AddEventListener` | 模块间解耦 |
| 单例 | `MonoSingleton<T>` / `BaseManager<T>` | 全局访问点 |
| 反射加载 | `Assembly.Load + GetType + Invoke` | HybridCLR 热更新 |
| 协程 | `IEnumerator + yield return` | 异步流程控制 |
| 本地持久化 | `File.WriteAllText + AES加密` | 断电保护 |

---

## 8. 项目策划书模板

### 8.1 项目概述

```
项目名称: Kiosk 自助服务终端系统
目标平台: Android 8.0+ 定制硬件
屏幕分辨率: 1080×1920 竖屏
开发引擎: Unity 2021.3 LTS
核心技术: SBox 硬件框架 / MQTT / HybridCLR 热更新
```

### 8.2 功能模块清单

| 模块 | 功能 | 优先级 |
|------|------|--------|
| **硬件层** | | |
| 投币器接入 | 多型号纸币器支持 | P0 |
| 钱箱控制 | 出钞/状态监控/预警 | P0 |
| 打印机 | 凭条打印/二维码生成 | P0 |
| 算法卡 | 游戏配置/账目/打码 | P1 |
| **网络层** | | |
| 机台登录 | HTTP + AES 认证 | P0 |
| 实时通信 | MQTT over WebSocket | P0 |
| 消息加密 | AES-256-CBC | P0 |
| 断线重连 | 自动重试+本地补发 | P0 |
| **业务层** | | |
| 现金充值 | 纸币→虚拟账户 | P0 |
| 现金兑换 | 扫码→出钞 | P0 |
| 订单管理 | 本地持久化+补发 | P0 |
| 管理后台 | 查账/设参/打报表 | P1 |
| **辅助功能** | | |
| 机台激活 | 首次使用激活 | P0 |
| 多语言 | EN/CN/ES | P1 |
| 图片轮播 | 广告位 | P2 |
| 热更新 | 远程更新业务逻辑 | P1 |

### 8.3 网络接口清单

| 接口 | 协议 | 地址 | 说明 |
|------|------|------|------|
| 登录 | HTTP POST | `/login` | 获取 MQTT 参数 |
| 激活 | HTTP POST | `/activate` | 激活新机台 |
| 认证 | MQTT | `machine/HD_login/1` | Token 认证 |
| 充值码 | MQTT | `order/HD_NewPrcode/1` | 生成预订单 |
| 纸币验证 | MQTT | `order/HD_CheckPerCashIn/1` | 逐张验证 |
| 兑换查询 | MQTT | `order/HD_FindWithdrawalOrder/1` | 二维码验证 |
| 兑换确认 | MQTT | `order/HD_ConfirmWithdrawalOrder/1` | 开始出钞 |
| 兑换完成 | MQTT | `order/HD_FinishWithdrawalOrder/1` | 出钞结果 |
| 钱箱配置 | MQTT | `admin/HD_GetCashBoxSettings/1` | 获取钱箱状态 |

### 8.4 数据安全设计

```
传输加密: AES-256-CBC
密钥管理:
  - 固定密钥: 编译时硬编码, 用于登录阶段
  - 动态密钥: 登录后服务器下发, 用于业务通信
  - 银行密钥: 登录后服务器下发, 用于扫描二维码加解密
IV生成: 每次加密使用新的随机16字节IV
本地存储: 订单数据 AES 加密后再写入文件
```

### 8.5 硬件接口清单

| 硬件 | SBox API | 说明 |
|------|----------|------|
| 纸币器 | `BillSelect/BillApprove/BillReject/BillCredit` | 多种型号自动适配 |
| 钱箱 | `CoinOutStart/CoinOutStop/IsCoinOutTimeout` | 4钱箱出钞 |
| 打印机 | `PrinterSelect/PrinterMessage/PrinterPaperCut` | 热敏打印 |
| 投币器 | `NumberOfCoinIn` | 硬币计数 |
| 开关量 | `SwitchInState/SwitchOutStateOn/SwitchOutStateOff` | 按键/LED/门磁 |
| 算法卡 | `ReadConf/WriteConf/GetAccount/GetSummary` | 游戏参数/账目 |

### 8.6 异常处理策略

| 异常场景 | 处理方式 |
|----------|----------|
| HTTP 登录失败 | 6秒后自动重试 (无限次) |
| MQTT 断线 | HTTP 重新登录 + MQTT 重连 (5.6秒间隔) |
| Publish 成功但未收到响应 | 15秒守护任务, 超时自动补发 |
| 打印失败 | 最多重试2次, 每次清缓冲区 |
| 断电 | 订单 AES 加密存本地, 下次启动自动补发 |
| 钱箱空 | 弹窗提示, 禁止兑换 |
| 钱箱不足 | 弹窗预警, 仍可操作 |
| 纸币验证失败 | 拒收纸币, 恢复可操作状态 |
| 兑换失败 | 删除本地订单, 弹窗提示 |

---

> **文档版本**: 2.0
> **生成日期**: 2026-06-19
> **基于分支**: blizz
