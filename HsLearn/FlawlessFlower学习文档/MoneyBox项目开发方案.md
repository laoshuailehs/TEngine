# MoneyBox 自助终端 — 基于 SBoxBase 框架开发方案

> **基于框架**: SBoxBase (G:\SBoxBase)
> **框架版本**: HybridCLR 热更 + SBox SDK + M2Mqtt + WebSocket 局域网
> **目标产品**: 游戏厅现金兑换自助终端 (Kiosk)
> **制定日期**: 2026-06-18

---

## 目录

1. [现有框架能力盘点](#1-现有框架能力盘点)
2. [目标产品架构差距分析](#2-目标产品架构差距分析)
3. [总体架构设计](#3-总体架构设计)
4. [制作周期总览](#4-制作周期总览)
5. [阶段一：云网络通信层 (第1-2周)](#5-阶段一云网络通信层)
6. [阶段二：主界面 MainGameView (第3-4周)](#6-阶段二主界面-maingameview)
7. [阶段三：投币充值核心业务 (第5-6周)](#7-阶段三投币充值核心业务)
8. [阶段四：现金兑换业务 (第7-8周)](#8-阶段四现金兑换业务)
9. [阶段五：打印机凭条系统 (第9周)](#9-阶段五打印机凭条系统)
10. [阶段六：本地订单容错系统 (第10周)](#10-阶段六本地订单容错系统)
11. [阶段七：管理后台 + 机台激活 (第11周)](#11-阶段七管理后台--机台激活)
12. [阶段八：集成调试与真机部署 (第12-14周)](#12-阶段八集成调试与真机部署)

---

## 1. 现有框架能力盘点

### 1.1 已具备的核心能力

```
SBoxBase 框架
├── ✅ 启动与热更
│   ├── Main.cs              — HybridCLR 热更入口 (版本检测 → DLL/AB下载 → 反射加载)
│   ├── Load.cs              — 场景加载流程 (SBox初始化 → 读配置 → 联网 → 进场景)
│   └── AssetBundleMgr.cs    — AB包加载管理
│
├── ✅ SBox 硬件 SDK 封装 (SBoxApi 命名空间)
│   ├── SBoxInit.cs          — 硬件初始化 (算法卡 + 底板就绪检测)
│   ├── SBoxIdea.cs          — 算法卡 (配置/账目/密码/游戏逻辑)
│   ├── SBoxSandbox.cs       — 底板 (纸币器/打印机/退币器/电机/灯)
│   ├── SBoxSandboxListener  — 硬件IO事件监听 (BILL_IN/BILL_STACKED/按键...)
│   └── SBoxModel.cs         — 机台信息 (USN/MAC/DeviceId)
│
├── ✅ 网络通信
│   ├── M2Mqtt Unity         — 完整 MQTT 3.1.1 客户端 (已对接阿里云IoT)
│   ├── IoTPayment.cs        — 阿里云IoT平台支付 (设备注册/投币/退票)
│   ├── ClientWS.cs          — WebSocket 客户端 (UDP广播发现 → WS长连 → 心跳)
│   ├── ServerWS.cs          — WebSocket 服务端 (局域网主机模式)
│   └── NetMgr.cs            — 网络管理器
│
├── ✅ UI 框架
│   ├── BasePanel.cs         — 自动绑定控件 + 统一按钮回调
│   ├── IOCanvasView.cs      — 管理后台完整UI (密码/参数/账目/时间/彩金...)
│   ├── IOCanvasManager.cs   — 管理后台逻辑
│   ├── IOCanvasModel.cs     — 管理后台数据模型 + Language枚举
│   └── TipsMgr.cs           — 气泡/简单提示系统
│
├── ✅ 基础架构
│   ├── MonoSingleton<T>     — Mono单例基类
│   ├── BaseManager<T>       — 普通单例基类
│   ├── FSMSystem.cs         — 有限状态机
│   ├── Timer.cs             — 定时器系统
│   ├── InputMgr.cs          — 输入管理器
│   ├── SQLiteManager.cs     — 数据库
│   ├── AudioManager.cs      — 音频管理
│   └── 事件系统三层          — SBoxMessenger / LogicMessenger / NetMessenger
│
├── ✅ 即插即用的库
│   ├── zxing.unity.dll      — QR码生成 (已在 Plugins/)
│   ├── Newtonsoft.Json      — JSON序列化
│   ├── UnityWebSocket       — WebSocket库
│   └── DOTween (Demigiant)  — 动画库
│
└── ✅ 硬件驱动 (Plugins/Android/)
    ├── SandboxPlugin-V1.0.0.jar  — SBox底板驱动
    └── rctlibrary-debug.aar      — 算法卡驱动
```

### 1.2 现有的开发模式

```csharp
// 1. 事件通信模式 (三层 Messenger)
SBoxMessenger.AddListener<T>(MessageName.xxx, callback);      // SBox硬件层事件
LogicMessenger.Broadcast(MessageName.xxx, data);              // 业务逻辑层事件
NetMessenger.Broadcast<T>(MessageName.xxx, data);             // 网络层事件

// 2. Manager 模式
public class XxxManager : BaseManager<XxxManager> { }  // 普通Manager
public class XxxScript : MonoSingleton<XxxScript> { }  // Mono单例

// 3. UI Panel 模式
public class XxxPanel : BasePanel {
    // 自动绑定 Button/Text/Toggle/InputField (通过控件名)
    protected override void OnClick(string btnName) { }
}

// 4. 硬件操作模式
SBoxInit.Instance.Init(matchIP, onComplete);  // 初始化
SBoxSandbox.BillApprove();                    // 核准纸币
SBoxSandboxListener.Instance.AddButtonClick(switch, callback); // 监听按键
```

---

## 2. 目标产品架构差距分析

### 2.1 框架 vs 产品能力对照

| 能力 | SBoxBase 框架 | MoneyBox 需要 | 差距 |
|------|:---:|:---:|------|
| SBox 硬件初始化 | ✅ | ✅ | 直接复用 |
| 纸币器控制 (BILL_IN/ACCEPT/REJECT) | ✅ | ✅ | 直接复用 |
| 打印机基础控制 | ✅ | ✅ | 直接复用 |
| WebSocket 局域网通信 | ✅ | ❌不需要 | MoneyBox 用 HTTP+MQTT 连云端 |
| MQTT 客户端 | ✅ | ✅ | 需扩展 Topic 订阅 |
| 管理后台 UI 框架 | ✅ | ⚠️部分 | 需扩展钱箱/打印机/纸币器设置 |
| 多语言 (chs/cht/en) | ✅ | ⚠️部分 | 需扩展 EN/CN/ES |
| **HTTP 云登录** | ❌ | ✅ | **新建** |
| **MQTT 云端通信** | ⚠️只有IoT支付 | ✅ | **新建** (MachineTopic) |
| **MainGameView 主界面** | ❌ | ✅ | **新建** |
| **投币充值完整流程** | ❌ | ✅ | **新建** |
| **二维码扫码兑换** | ❌ | ✅ | **新建** |
| **本地订单持久化** | ❌ | ✅ | **新建** |
| **凭条排版/QR打印** | ❌ | ✅ | **新建** |
| **机台激活流程** | ❌ | ✅ | **新建** |
| **广告图片轮播** | ❌ | ✅ | **新建** |
| **AES 加解密** | ❌ | ✅ | **新建** |
| **数据差异热更** | ✅ | ✅ | 直接复用 |

### 2.2 结论

**框架已提供 40% 能力**（硬件层、网络层、UI框架、基础架构），需要在此基础上新建 **10 个核心业务模块**。

---

## 3. 总体架构设计

### 3.1 分层架构

```
┌──────────────────────────────────────────────────────────────┐
│                     Game.dll (新增业务层)                      │
│                                                              │
│  MoneyBoxMain    MainGameView    CashExchangePanel           │
│  SandBoxScript   PrinterScript   LocalOrderState             │
│  ActivateMachine CloudNet        BGMgr Kiosk                 │
├──────────────────────────────────────────────────────────────┤
│                     Base.dll (现有框架层)                      │
│                                                              │
│  IoTPayment  M2MqttUnityClient  ClientWS  ServerWS          │
│  IOCanvasView/Manager/Model     BasePanel  TipsMgr           │
│  SBoxInit  SBoxIdea  SBoxSandbox  SBoxSandboxListener        │
│  Timer  FSMSystem  InputMgr  AudioManager  SQLiteManager     │
├──────────────────────────────────────────────────────────────┤
│                     Plugins (原生层)                          │
│                                                              │
│  SandboxPlugin.jar  rctlibrary.aar  zxing.unity.dll          │
│  UnityWebSocket  Newtonsoft.Json  DOTween                   │
├──────────────────────────────────────────────────────────────┤
│                     Unity + Android OS                        │
│  触摸屏  纸币器  钱箱  热敏打印机  算法卡  底板  WiFi         │
└──────────────────────────────────────────────────────────────┘
```

### 3.2 数据流

```
                   HTTP/REST (AES加密)
  ┌────────┐  ───────────────────────────→  ┌──────────────┐
  │ MoneyBox│    登录/激活/验证纸币           │  业务云端     │
  │  Client │  ←───────────────────────────  │  (Spring)    │
  └───┬────┘                                └──────┬───────┘
      │                                            │
      │ MQTT over WebSocket                        │ MQTT
      │ (machine/HD_xxx Topics)                    │
      └────────────────────────────────────────────┘
```

---

## 4. 制作周期总览

```
周次     1    2    3    4    5    6    7    8    9   10   11   12   13   14
       ├────┼────┼────┼────┼────┼────┼────┼────┼────┼────┼────┼────┼────┤
云网络  ████████
主界面       ████████
投币充值             ████████
现金兑换                     ████████
打印机                            ████
本地订单                               ████
后台+激活                                   ████
联调+部署                                        ██████████
```

**总周期**: 14 周 (约 3.5 个月) | **开发人员**: 2 人 (客户端为主，1 人可兼服务端)

---

## 5. 阶段一：云网络通信层

> **时间**: 第 1-2 周 | **优先级**: P0
> **产出**: HTTP 登录 → MQTT 云连接 → AES 加解密 → 获取业务配置

### 5.1 使用现有框架能力

- `M2MqttUnityClient` — MQTT 连接基础（已对接阿里云，需扩展为自建 Broker）
- `IoTPayment` — 参考其 MQTT 连接 + Topic 订阅模式
- `SBoxModel.Instance.USN` / `macId` — 机台唯一标识
- `Utils.Post()` — HTTP 请求方法

### 5.2 需要新建的文件

| 文件 | 职责 |
|------|------|
| `CloudNet.cs` | HTTP 登录 + MQTT 连接 + Topic 管理 + 心跳 |
| `AESScript.cs` | AES-256-CBC 加解密 |
| `MoneyBoxMain.cs` | 替代现有 Load.cs 的启动流程 |

### 5.3 核心代码

#### AESScript.cs

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class AESScript
{
    private const CipherMode MODE = CipherMode.CBC;
    private const PaddingMode PADDING = PaddingMode.PKCS7;

    public static byte[] QRCodeIV = Encoding.UTF8.GetBytes("1234567890123456");

    public static string Encrypt(string plain, string key, byte[] iv = null)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        byte[] keyBytes = PadKey(Encoding.UTF8.GetBytes(key));

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv ?? new byte[16];
            aes.Mode = MODE;
            aes.Padding = PADDING;

            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] data = Encoding.UTF8.GetBytes(plain);
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string cipher, string key, byte[] iv = null)
    {
        if (string.IsNullOrEmpty(cipher)) return "";
        byte[] keyBytes = PadKey(Encoding.UTF8.GetBytes(key));

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv ?? new byte[16];
            aes.Mode = MODE;
            aes.Padding = PADDING;

            using (var ms = new MemoryStream(Convert.FromBase64String(cipher)))
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
                return sr.ReadToEnd();
        }
    }

    static byte[] PadKey(byte[] key)
    {
        Array.Resize(ref key, 32);
        return key;
    }
}
```

#### CloudNet.cs — HTTP 登录 + MQTT 连接

```csharp
using M2MqttUnity;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CloudNet : MonoSingleton<CloudNet>
{
    [Header("服务器配置")]
    public string ServerHost = "192.168.1.100";
    public int ServerPort = 8443;
    public int TimeoutSeconds = 10;

    // AES 密钥 — 通过安全渠道获取
    private string _aesKey = "BaseKey_ReplacedInProduction!";

    // 云端下发的数据
    [HideInInspector] public string AccessToken;
    [HideInInspector] public string MqttBroker;
    [HideInInspector] public string PlayerId;
    [HideInInspector] public string PlatTag;
    [HideInInspector] public string MachineSetId;     // 机台展示编号
    [HideInInspector] public int MaxRecharge;
    [HideInInspector] public int MinRecharge;
    [HideInInspector] public string[] AdImageUrls;     // 广告图片URL

    // 网络状态
    public bool IsCloudConnected { get; private set; }

    // MQTT Topic 前缀
    private const string TOPIC_LOGIN     = "machine/HD_login/1";
    private const string TOPIC_NEW_PRCODE = "machine/HD_NewPrcode/1";
    private const string TOPIC_CHECK_BILL = "machine/HD_CheckPerCashIn/1";
    private const string TOPIC_FIND_ORDER = "machine/HD_FindWithdrawalOrder/1";
    private const string TOPIC_CONFIRM    = "machine/HD_ConfirmWithdrawalOrder/1";
    private const string TOPIC_FINISH     = "machine/HD_FinishWithdrawalOrder/1";

    /// <summary>
    /// 入口: 硬件就绪后调用
    /// </summary>
    public void StartCloudConnection()
    {
        StartCoroutine(CloudLogin());
    }

    /// <summary>
    /// ① HTTP 登录 → 拿到 accessToken + mqttBroker
    /// </summary>
    IEnumerator CloudLogin()
    {
        string machineCode = SBoxModel.Instance.USN;
        string lang = "en";

        var loginReq = new { machineCode, lang };
        string json = JsonConvert.SerializeObject(loginReq);
        string encBody = AESScript.Encrypt(json, _aesKey);

        string url = $"https://{ServerHost}:{ServerPort}/login";
        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(encBody);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = TimeoutSeconds;
            req.certificateHandler = new BypassCert(); // 生产环境请用正式证书
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CloudNet] 登录失败: {req.error}");
                // 通知 UI 显示网络错误
                LogicMessenger.Broadcast(MessageName.Event_Error);
                yield break;
            }

            string respJson = AESScript.Decrypt(req.downloadHandler.text, _aesKey);
            var resp = JsonConvert.DeserializeObject<LoginResponse>(respJson);

            if (resp.code == 0)
            {
                AccessToken = resp.data.accessToken;
                MqttBroker = resp.data.mqttBroker;
                PlayerId = resp.data.playerId;
                PlatTag = resp.data.platTag;
                MachineSetId = resp.data.machineSetId;
                MaxRecharge = resp.data.rangeInfo.maxRecharge;
                MinRecharge = resp.data.rangeInfo.minRecharge;
                AdImageUrls = resp.data.adImages;

                // ② 连接 MQTT
                yield return StartCoroutine(ConnectMqtt());
            }
            else if (resp.code == 555 || resp.code == 557 || resp.code == 41801)
            {
                // 机台未激活 → 通知 UI 显示激活面板
                LogicMessenger.Broadcast(MessageName.Event_Active);
            }
            else
            {
                Debug.LogError($"[CloudNet] 登录异常: code={resp.code}");
                LogicMessenger.Broadcast(MessageName.Event_Error);
            }
        }
    }

    /// <summary>
    /// ② 连接 MQTT Broker → 发送登录 Token → 订阅业务 Topic
    /// </summary>
    IEnumerator ConnectMqtt()
    {
        // 复用框架已有的 M2MqttUnityClient
        var mqtt = M2MqttUnityClient.Instance;

        mqtt.brokerAddress = MqttBroker;
        mqtt.brokerPort = 1883;
        mqtt.ConnectionSucceeded += OnMqttConnected;
        mqtt.ConnectionFailed += OnMqttFailed;
        mqtt.ActionDecodeMessage += OnMqttMessage;

        mqtt.Connect();

        float wait = 0;
        while (!mqtt.IsConnected && wait < 5f)
        {
            yield return new WaitForSeconds(0.2f);
            wait += 0.2f;
        }

        if (!mqtt.IsConnected)
        {
            LogicMessenger.Broadcast(MessageName.Event_Error);
            yield break;
        }
    }

    void OnMqttConnected()
    {
        // 发送登录 Token
        MqttPublish(TOPIC_LOGIN, AccessToken);

        // 订阅业务 Topic
        SubscribeTopic(TOPIC_LOGIN);
        SubscribeTopic(TOPIC_NEW_PRCODE);
        SubscribeTopic(TOPIC_CHECK_BILL);
        SubscribeTopic(TOPIC_FIND_ORDER);
        SubscribeTopic(TOPIC_CONFIRM);
        SubscribeTopic(TOPIC_FINISH);

        IsCloudConnected = true;
        LogicMessenger.Broadcast(MessageName.Event_InitViewFinish);
    }

    void OnMqttFailed()
    {
        IsCloudConnected = false;
        LogicMessenger.Broadcast(MessageName.Event_Error);
    }

    void OnMqttMessage(string topic, byte[] data)
    {
        string payload = Encoding.UTF8.GetString(data);
        // 投递给业务模块
        NetMessenger.Broadcast(topic, payload);
    }

    void SubscribeTopic(string topic)
    {
        M2MqttUnityClient.Instance.AddSubscribeTopics(topic);
    }

    /// <summary>
    /// 公共 MQTT 发布 (带 AES 加密)
    /// </summary>
    public void MqttPublish(string topic, string payload)
    {
        string encrypted = AESScript.Encrypt(payload, _aesKey);
        M2MqttUnityClient.Instance.PublishMsg(topic, encrypted);
    }

    void OnDestroy()
    {
        var mqtt = M2MqttUnityClient.Instance;
        if (mqtt != null)
        {
            mqtt.ConnectionSucceeded -= OnMqttConnected;
            mqtt.ConnectionFailed -= OnMqttFailed;
            mqtt.ActionDecodeMessage -= OnMqttMessage;
        }
    }
}

public class BypassCert : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}

// 数据模型
[Serializable]
public class LoginResponse
{
    public int code;
    public LoginData data;
}

[Serializable]
public class LoginData
{
    public string accessToken;
    public string mqttBroker;
    public string playerId;
    public string platTag;
    public string machineSetId;
    public RangeInfo rangeInfo;
    public string[] adImages;
}

[Serializable]
public class RangeInfo
{
    public int maxRecharge;
    public int minRecharge;
}
```

#### MoneyBoxMain.cs — 替代现有 Load.cs 的启动流程

```csharp
using SBoxApi;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MoneyBox 启动流程: 硬件初始化 → 读配置 → 云登录 → 显示主界面
/// 替代原 Load.cs 的启动逻辑
/// </summary>
public class MoneyBoxMain : MonoBehaviour
{
    private Text descText;
    private Text descDetailText;

    void Awake()
    {
        descText = GameObject.Find("descText")?.GetComponent<Text>();
        descDetailText = GameObject.Find("descDetailText")?.GetComponent<Text>();
    }

    void Start()
    {
        if (descText != null)
            descText.text = "正在检测硬件...";

        // 注册配置读取事件
        SBoxMessenger.AddListener<SBoxConfData>(
            MessageName.Event_SBoxIdea_ReadConf, OnReadConfig);

        // ① 初始化 SBox 硬件 (复用框架 SBoxInit)
        SBoxInit.Instance.Init("192.168.3.122", descDetailText, onComplete: OnInitSBox);
    }

    void OnInitSBox()
    {
        if (descText != null)
            descText.text = "正在读取配置...";
        SBoxIdea.ReadConf();
    }

    void OnReadConfig(SBoxConfData cfg)
    {
        // 保存配置
        IOCanvasModel.Instance.CfgData = cfg;

        // 设置语言
        IOCanvasModel.Instance.CurLanguage = (Language)System.Enum.Parse(
            typeof(Language),
            PlayerPrefs.GetInt("CurLanguage", 0).ToString());

        // 移除监听
        SBoxMessenger.RemoveListener<SBoxConfData>(
            MessageName.Event_SBoxIdea_ReadConf, OnReadConfig);

        if (descText != null)
            descText.text = "正在连接云端...";

        // ② 启动云端连接 (HTTP 登录 + MQTT)
        CloudNet.Instance.ServerHost = "your-cloud-server.com";
        CloudNet.Instance.StartCloudConnection();
    }
}
```

---

## 6. 阶段二：主界面 MainGameView

> **时间**: 第 3-4 周 | **优先级**: P0
> **产出**: 主界面布局、按钮路由、扫码检测、广告轮播、错误提示

### 6.1 使用现有框架能力

- `BasePanel` — UI 面板基类（自动绑定控件 + 按钮回调分发）
- `TipsMgr` — 错误提示气泡
- `LogicMessenger` — 业务事件通信
- `Language` 枚举 + `Utils.GetLanguage()` — 多语言

### 6.2 需要新建的文件

| 文件 | 职责 |
|------|------|
| `MainGameView.cs` | 主界面控制器 |
| `UI/MainGameView.QRCode.cs` | 扫码输入检测 |
| `UI/ErrorPanelManager.cs` | 统一错误提示面板管理 |

### 6.3 主界面 UI 层级

```
MainPanel (Canvas)
├── TopBar
│   ├── WifiIcon (Image)
│   ├── SetIDText (Text) — "机台: ---"
│   └── VersionText (Text) — "Ver: 1.0.0"
├── PhotoCarousel (RawImage) — 广告轮播
├── QRCodeArea
│   ├── QRCodeInputField (InputField) — 隐藏扫码框
│   └── ScanLine (Image) — 扫描线
├── MainButtons (GridLayoutGroup)
│   ├── OnlineRecharge (Button) — 在线充值
│   ├── LocalRecharge (Button) — 设备充值
│   ├── CashRechange (Button) — 现金兑换
│   ├── PrintAgainButton (Button) — 重打凭条
│   └── LanguageChoose (Button) — 语言切换
├── MenuObj (空容器 — 管理后台动态菜单)
└── ErrorPanels
    ├── NetErrorPanel — 网络错误
    ├── NormalErrorPanel — 普通提示
    ├── PrinterErrorPanel — 打印机错误
    └── LocalOrderErrorPanel — 本地订单错误
```

### 6.4 核心代码

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainGameView : MonoBehaviour
{
    public static MainGameView Instance { get; private set; }

    // ═══ UI 引用 ═══
    [Header("顶部状态")]
    public Text SetIDText;
    public Text VersionText;
    public Image WifiIcon;

    [Header("广告")]
    public RawImage PhotoCarouselImage;

    [Header("扫码")]
    public InputField QRCodeInputField;
    public Image ScanLine;

    [Header("按钮")]
    public Button OnlineRechargeBtn;
    public Button LocalRechargeBtn;
    public Button CashRechangeBtn;
    public Button PrintAgainBtn;
    public Button LanguageChooseBtn;

    [Header("面板引用")]
    public GameObject CashExchangePanel;
    public GameObject ActivateMachinePanel;

    // ═══ 状态 ═══
    public static bool IsEnableClick = true;
    public static bool IsEnableQRCode = true;
    public static bool IsQRCodeHead = false;

    private Dictionary<string, GameObject> _menuObjs = new();

    void Awake()
    {
        Instance = this;

        // 缓存 MenuObj 子节点 (用于权限控制)
        var menuObj = transform.Find("MenuObj");
        if (menuObj != null)
            foreach (Transform t in menuObj)
                _menuObjs[t.name] = t.gameObject;
    }

    void Start()
    {
        // ① 注册所有按钮 (与 BasePanel 方式一致)
        foreach (var btn in GetComponentsInChildren<Button>(true))
            btn.onClick.AddListener(() => OnBtnClick(btn.name));

        // ② 监听业务事件
        LogicMessenger.AddListener(MessageName.Event_InitViewFinish, OnCloudReady);
        LogicMessenger.AddListener(MessageName.Event_Error, OnNetworkError);
        LogicMessenger.AddListener(MessageName.Event_Active, OnNeedActivate);

        // ③ 注册硬件 SET 键 → 打开管理后台
        SBoxSandboxListener.Instance.AddButtonClick(
            SBOX_SWITCH.SWITCH_SET, OpenAdminBackend);

        // ④ 激活扫码输入框
        if (QRCodeInputField != null)
            QRCodeInputField.ActivateInputField();

        // ⑤ 显示版本号
        if (VersionText != null)
            VersionText.text = $"Ver: {Application.version}";
    }

    void Update()
    {
        // ── 触摸/点击防连点 ──
        if (Input.GetMouseButtonDown(0) && IsEnableClick)
        {
            IsEnableClick = false;
            StartCoroutine(RestoreClick());
        }

        // ── 二维码扫码检测 ──
        if (QRCodeInputField != null
            && QRCodeInputField.text.Length > 0
            && IsEnableQRCode)
        {
            IsEnableQRCode = false;
            StartCoroutine(DetectQRInput());
        }

        // ── 保持输入框焦点 ──
        if (!IsQRCodeHead
            && QRCodeInputField != null
            && !QRCodeInputField.isFocused)
        {
            QRCodeInputField.ActivateInputField();
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F3)) OpenAdminBackend();
#endif
    }

    // ═══ 按钮路由 ═══
    void OnBtnClick(string btnName)
    {
        Debug.Log($"[MainGameView] 按钮点击: {btnName}");
        switch (btnName)
        {
            case "LanguageChoose":  SwitchLanguage(); break;
            case "OnlineRecharge":  OpenRecharge(true); break;
            case "LocalRecharge":   OpenRecharge(false); break;
            case "CashRechange":    CashExchangePanel.SetActive(true); break;
            case "PrintAgainButton": PrinterScript.RePrintLast(); break;
        }
    }

    // ═══ 事件回调 ═══
    void OnCloudReady()
    {
        // 更新机台编号显示
        if (SetIDText != null)
            SetIDText.text = $"机台: {CloudNet.Instance.MachineSetId}";

        // 下载广告图片
        if (CloudNet.Instance.AdImageUrls != null)
            StartCoroutine(LoadAdImages());

        // 更新语言
        Utils.GetLanguage(""); // 触发语言刷新
    }

    void OnNetworkError()
    {
        ShowErrorPanel("NetErrorPanel", "网络连接失败", 5f);
    }

    void OnNeedActivate()
    {
        ActivateMachinePanel?.SetActive(true);
    }

    // ═══ 扫码输入检测 ═══
    IEnumerator DetectQRInput()
    {
        string lastText = QRCodeInputField.text;
        float waited = 0;

        while (waited < 9.9f)
        {
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;

            string cur = QRCodeInputField.text;
            if (cur.Contains("&QRCodeEnd&")) break;
            if (cur == lastText && cur.Length > 0) break;
            if (cur.Length > 512) { ResetScanner(); yield break; }
            lastText = cur;
        }

        string final = QRCodeInputField.text;
        if (!string.IsNullOrEmpty(final))
            ProcessQRCode(final);
    }

    void ProcessQRCode(string raw)
    {
        // 提取内容 (去掉 qr_code: 或 bank: 头, 去掉 &QRCodeEnd& 尾)
        string content = raw;
        if (content.StartsWith("qr_code:"))
            content = content[8..];
        else if (content.StartsWith("bank:"))
            content = content[5..];

        content = content.Replace("&QRCodeEnd&", "");

        // 打开兑换面板
        CashExchangePanel.GetComponent<CashExchangePanel>()?.StartExchange(content);
    }

    void ResetScanner()
    {
        QRCodeInputField.text = "";
        IsEnableQRCode = true;
        IsQRCodeHead = false;
        QRCodeInputField.ActivateInputField();
    }

    IEnumerator RestoreClick() { yield return null; IsEnableClick = true; }

    // ═══ 错误面板管理 ═══
    void ShowErrorPanel(string panelName, string msg, float duration)
    {
        var panel = transform.Find(panelName);
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        panel.GetComponentInChildren<Text>().text = msg;
        if (duration > 0)
            StartCoroutine(AutoHidePanel(panel.gameObject, duration));
    }

    IEnumerator AutoHidePanel(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }

    IEnumerator LoadAdImages() { yield break; }

    // ═══ 辅助 ═══
    void SwitchLanguage()
    {
        int next = ((int)IOCanvasModel.Instance.CurLanguage + 1) % 3;
        IOCanvasModel.Instance.CurLanguage = (Language)next;
        PlayerPrefs.SetInt("CurLanguage", next);
        LogicMessenger.Broadcast(MessageName.Event_IO_ChangeLanguage);
    }

    void OpenRecharge(bool isOnline) { /* 打开充值面板 */ }
    void OpenAdminBackend()
    {
        // 复用框架 IOCanvasView
        var ioCanvas = FindObjectOfType<IOCanvasView>();
        if (ioCanvas != null)
            ioCanvas.gameObject.SetActive(true);
    }
}
```

---

## 7. 阶段三：投币充值核心业务

> **时间**: 第 5-6 周 | **优先级**: P0
> **产出**: 完整投币→验证→打印凭条流程

### 7.1 使用现有框架能力

- `SBoxSandboxListener` — 监听 `Event_Sandbox_ListenerBillIn` + `Event_Sandbox_ListenerBillStacked`
- `SBoxSandbox.BillApprove()` / `SBoxSandbox.BillReject()` — 核准/拒收纸币
- `CloudNet.MqttPublish()` — 与云端通信
- `NetMessenger` — 网络消息回调
- `LogicMessenger.Broadcast(MessageName.Event_PlayerCoinIn)` — 已有投币事件

### 7.2 核心代码

```csharp
using SBoxApi;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 投币充值控制器 — 使用现有框架的硬件事件
/// </summary>
public class SandBoxScript : MonoSingleton<SandBoxScript>
{
    [Header("充值状态")]
    public int CurrentTotalAmount { get; private set; }
    public bool CanAcceptBill { get; set; } = true;

    private bool _isRecharging = false;
    private string _currentPrcode;
    private int _currentBillStackedNum;

    void Start()
    {
        // 使用框架已有的 SBoxMessenger 监听硬件事件
        SBoxMessenger.AddListener<int>(MessageName.Event_Sandbox_ListenerBillIn, OnBillInserted);
        SBoxMessenger.AddListener<int>(MessageName.Event_Sandbox_ListenerBillStacked, OnBillStacked);
    }

    /// <summary>
    /// ① 纸币插入 → 验证
    /// </summary>
    void OnBillInserted(int denomination)
    {
        Debug.Log($"[SandBox] 纸币插入: 面额={denomination}");
        StartCoroutine(ValidateAndAccept(denomination));
    }

    IEnumerator ValidateAndAccept(int denomination)
    {
        // ─ 防重入 ─
        if (_isRecharging) { RejectBill(); yield break; }
        _isRecharging = true;

        // ─ 前置检查 ─
        if (!CanAcceptBill)
        {
            Debug.LogWarning("[SandBox] 当前不可投币");
            RejectBill();
            _isRecharging = false;
            yield break;
        }

        // 检查打印机 (使用框架 SBoxSandbox)
        if (!CheckPrinterReady())
        {
            RejectBill();
            _isRecharging = false;
            yield break;
        }

        // ─ 请求充值码 (如果是首张) ─
        if (string.IsNullOrEmpty(_currentPrcode))
        {
            bool gotCode = false;
            string prcode = null;

            // 使用框架 NetMessenger 监听 MQTT 响应
            Action<string> handler = (resp) =>
            {
                var r = JsonUtility.FromJson<NewPrcodeResp>(resp);
                if (r.code == 0)
                {
                    prcode = r.data.prcode;
                    MaxRechargeLimit = r.data.rangeInfo.maxRecharge;
                    gotCode = true;
                }
            };

            // 注册一次性监听
            NetMessenger.AddListener<string>("machine/HD_NewPrcode/1", handler);
            CloudNet.Instance.MqttPublish("machine/HD_NewPrcode/1", "{}");

            float waitTime = 0;
            while (!gotCode && waitTime < 10f)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
            NetMessenger.RemoveListener<string>("machine/HD_NewPrcode/1", handler);

            if (!gotCode)
            {
                RejectBill();
                _isRecharging = false;
                yield break;
            }

            _currentPrcode = prcode;
            _currentBillStackedNum = 0;

            // 保存预订单 (阶段六实现)
            LocalOrderState.Instance?.SaveOrder(_currentPrcode, denomination);
        }

        // ─ 金额上限检查 ─
        if (CurrentTotalAmount + denomination > MaxRechargeLimit)
        {
            RejectBill();
            TipsMgr.Instance.ShowBubbleTips("超出最大充值金额", transform);
            _isRecharging = false;
            yield break;
        }

        // ─ 云端验证纸币 ─
        bool verified = false;
        string errMsg = "";

        Action<string> checkHandler = (resp) =>
        {
            var r = JsonUtility.FromJson<BaseResp>(resp);
            verified = (r.code == 0);
            errMsg = r.message;
        };

        var checkBody = new CheckBillReq
        {
            platTag = CloudNet.Instance.PlatTag,
            amount = denomination,
            prcode = _currentPrcode,
            playerId = CloudNet.Instance.PlayerId
        };

        NetMessenger.AddListener<string>("machine/HD_CheckPerCashIn/1", checkHandler);
        CloudNet.Instance.MqttPublish("machine/HD_CheckPerCashIn/1",
            JsonUtility.ToJson(checkBody));

        float waitTime2 = 0;
        while (!verified && string.IsNullOrEmpty(errMsg) && waitTime2 < 10f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime2 += 0.1f;
        }
        NetMessenger.RemoveListener<string>("machine/HD_CheckPerCashIn/1", checkHandler);

        if (!verified)
        {
            RejectBill();
            TipsMgr.Instance.ShowBubbleTips(errMsg ?? "验证失败", transform);
            _isRecharging = false;
            yield break;
        }

        // ─ 验证通过！核准纸币 ─
        SBoxSandbox.BillApprove();
        _isRecharging = false;

        // 触发 UI 更新
        LogicMessenger.Broadcast(MessageName.Event_PlayerCoinIn, denomination);
    }

    /// <summary>
    /// ② 纸币存入钱箱 → 累加金额 → 触发打印
    /// </summary>
    void OnBillStacked(int denomination)
    {
        _currentBillStackedNum++;
        CurrentTotalAmount += denomination;
        Debug.Log($"[SandBox] 纸币存入: 本张={denomination}, 累计={CurrentTotalAmount}, 张数={_currentBillStackedNum}");

        // 更新本地订单
        LocalOrderState.Instance?.UpdateAmount(_currentPrcode, CurrentTotalAmount);

        // 触发打印凭条
        PrinterScript.Instance?.PrintRechargeTicket(
            CurrentTotalAmount, _currentPrcode);
    }

    /// <summary>
    /// 完成当前充值 (例如超时或手动结束)
    /// </summary>
    public void FinishCurrentRecharge()
    {
        CurrentTotalAmount = 0;
        _currentPrcode = null;
        _currentBillStackedNum = 0;
        CanAcceptBill = true;
    }

    bool CheckPrinterReady()
    {
        var state = SBoxSandbox.PrinterState();
        return state >= 0;
    }

    void RejectBill()
    {
        SBoxSandbox.BillReject();
    }

    void OnDestroy()
    {
        SBoxMessenger.RemoveListener<int>(
            MessageName.Event_Sandbox_ListenerBillIn, OnBillInserted);
        SBoxMessenger.RemoveListener<int>(
            MessageName.Event_Sandbox_ListenerBillStacked, OnBillStacked);
    }

    [HideInInspector] public int MaxRechargeLimit = 5000;

    [Serializable] class NewPrcodeResp { public int code; public NewPrcodeData data; }
    [Serializable] class NewPrcodeData { public string prcode; public RangeInfo rangeInfo; }
    [Serializable] class BaseResp { public int code; public string message; }
    [Serializable] class CheckBillReq
    {
        public string platTag;
        public int amount;
        public string prcode;
        public string playerId;
    }
}
```

---

## 8. 阶段四：现金兑换业务

> **时间**: 第 7-8 周 | **优先级**: P0
> **产出**: 扫码 → 验证订单 → 确认金额 → 出钞 → 打印凭条

### 8.1 使用现有框架能力

- `MainGameView.QRCodeInputField` — 扫码输入 (阶段二已做)
- `CloudNet.MqttPublish()` — 与云端通信
- `SBoxSandbox.CoinOutStart()` — 退币/出钞 (框架已有)
- `NetMessenger` — 监听 MQTT 响应

### 8.2 核心代码 (兑换面板 — 关键方法)

```csharp
/// <summary>
/// 现金兑换面板 — 继承框架 BasePanel 获得自动控件绑定
/// </summary>
public class CashExchangePanel : BasePanel
{
    public static CashExchangePanel Instance { get; private set; }

    // 金额输入
    private Text _balanceText;
    private InputField _amountInput;
    private Text _inputDisplayText;

    // 面板引用
    private GameObject _inputPanel;     // 输入面板
    private GameObject _confirmPanel;   // 二次确认面板

    // 兑换数据
    private string _qrRawData;
    private int _availableBalance;
    private int _exchangeAmount;
    private int _codeType; // 0=绑定卡 1=标准 2=二次兑换
    private List<int> _presetAmounts = new();

    protected override void Awake()
    {
        base.Awake(); // BasePanel 自动绑定所有 Button/Text/InputField
        Instance = this;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 入口: 从 MainGameView 扫码后调用
    /// </summary>
    public void StartExchange(string qrData)
    {
        _qrRawData = qrData;
        gameObject.SetActive(true);
        ShowInputPanel();

        // ① 云端查询订单
        StartCoroutine(FindOrder());
    }

    IEnumerator FindOrder()
    {
        string encData = AESScript.Encrypt(_qrRawData, "cloud_key");
        bool done = false;
        FindOrderResp result = null;

        // 发送 + 注册一次性监听
        var handler = new System.Action<string>((resp) =>
        {
            result = JsonUtility.FromJson<FindOrderResp>(resp);
            done = true;
        });

        NetMessenger.AddListener<string>("machine/HD_FindWithdrawalOrder/1", handler);
        CloudNet.Instance.MqttPublish("machine/HD_FindWithdrawalOrder/1", encData);

        float wait = 0;
        while (!done && wait < 15f)
        {
            yield return new WaitForSeconds(0.1f);
            wait += 0.1f;
        }
        NetMessenger.RemoveListener<string>("machine/HD_FindWithdrawalOrder/1", handler);

        if (result == null || result.code != 0)
        {
            TipsMgr.Instance.ShowBubbleTips(
                result?.message ?? "查询失败, 请重试", transform);
            ClosePanel();
            yield break;
        }

        // 处理响应
        _codeType = result.data.codeType;
        _availableBalance = result.data.balance;
        _presetAmounts = result.data.presetAmounts ?? new List<int>();

        _balanceText.text = $"可用余额: {_availableBalance}";

        if (_codeType == 2)
        {
            // 二次兑换 — 自动确认全额
            _exchangeAmount = _availableBalance;
            StartCoroutine(ConfirmExchange());
        }
    }

    // ═══ BasePanel 按钮回调 ═══
    protected override void OnClick(string btnName)
    {
        switch (btnName)
        {
            case "ConfirmButton":    OnUserConfirm(); break;
            case "ReConfirmButton":  StartCoroutine(ConfirmExchange()); break;
            case "CancelButton":     ClosePanel(); break;
            case "ExChangeAllButton":  _amountInput.text = _availableBalance.ToString(); break;
            case "ClearButton":      _amountInput.text = "0"; break;
            default:
                // 数字按钮: "NumButton_0" ~ "NumButton_9"
                if (btnName.StartsWith("NumButton_"))
                {
                    string digit = btnName.Replace("NumButton_", "");
                    AppendDigit(digit);
                }
                break;
        }
    }

    void OnUserConfirm()
    {
        if (!int.TryParse(_amountInput.text, out int amount) ||
            amount < 1 || amount > _availableBalance)
        {
            TipsMgr.Instance.ShowBubbleTips("金额无效", transform);
            return;
        }
        _exchangeAmount = amount;
        ShowConfirmPanel(); // 显示二次确认
    }

    /// <summary>
    /// ② 确认兑换 → 出钞
    /// </summary>
    IEnumerator ConfirmExchange()
    {
        var req = new ConfirmExchangeReq
        {
            amount = _exchangeAmount,
            prcode = CloudNet.Instance.PlatTag,
        };

        bool done = false;
        ConfirmExchangeResp result = null;

        var handler = new System.Action<string>((resp) =>
        {
            result = JsonUtility.FromJson<ConfirmExchangeResp>(resp);
            done = true;
        });

        NetMessenger.AddListener<string>("machine/HD_ConfirmWithdrawalOrder/1", handler);
        CloudNet.Instance.MqttPublish("machine/HD_ConfirmWithdrawalOrder/1",
            JsonUtility.ToJson(req));

        float wait = 0;
        while (!done && wait < 15f)
        {
            yield return new WaitForSeconds(0.1f);
            wait += 0.1f;
        }
        NetMessenger.RemoveListener<string>("machine/HD_ConfirmWithdrawalOrder/1", handler);

        if (result?.code != 0)
        {
            TipsMgr.Instance.ShowBubbleTips(result?.message ?? "兑换失败", transform);
            ClosePanel();
            yield break;
        }

        // ③ 出钞 — 使用框架 SBoxSandbox.CoinOutStart
        foreach (var method in result.data.paymentMethods)
        {
            for (int i = 0; i < method.count; i++)
            {
                SBoxSandbox.CoinOutStart(method.denomination);
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ④ 打印凭条
        PrinterScript.Instance.PrintExchangeTicket(_exchangeAmount);

        // ⑤ 完成通知
        CloudNet.Instance.MqttPublish("machine/HD_FinishWithdrawalOrder/1",
            JsonUtility.ToJson(new { prcode = CloudNet.Instance.PlatTag }));

        TipsMgr.Instance.ShowSimpleTips("兑换成功", transform, Vector3.zero, Vector3.one, 3f);
        ClosePanel();
    }

    void ShowInputPanel()
    {
        _inputPanel.SetActive(true);
        _confirmPanel.SetActive(false);
    }
    void ShowConfirmPanel()
    {
        _inputPanel.SetActive(false);
        _confirmPanel.SetActive(true);
    }
    void ClosePanel()
    {
        gameObject.SetActive(false);
        MainGameView.InitScanner(true);
    }
    void AppendDigit(string d)
    {
        if (_amountInput.text == "0") _amountInput.text = d;
        else _amountInput.text += d;
    }
}
```

---

## 9. 阶段五：打印机凭条系统

> **时间**: 第 9 周 | **优先级**: P1
> **产出**: 凭条排版、QR码生成、自动重打

### 9.1 使用现有框架能力

- `SBoxSandbox.PrinterMessage()` — 打印文本行 (框架已有)
- `SBoxSandbox.PrinterPaperCut()` — 切纸 (框架已有)
- `SBoxSandbox.PrinterState()` — 打印机状态 (框架已有)
- `zxing.unity.dll` — QR码生成 (已在 Plugins/)

### 9.2 核心代码

```csharp
using SBoxApi;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

public class PrinterScript : MonoSingleton<PrinterScript>
{
    [Header("店铺信息")]
    public string StoreName = "GAME CENTER";
    public string StoreAddr = "123 Main St";
    public string StorePhone = "555-0100";
    public string StoreEmail = "support@game.com";

    // 重打计数器
    private int _reprintCount = 0;
    private const int MAX_REPRINT = 3;
    // 上次打印内容缓存 (用于重打)
    private List<string> _lastPrintContent;
    private string _lastQRContent;

    /// <summary>
    /// 打印充值凭条
    /// </summary>
    public void PrintRechargeTicket(int amount, string orderNo)
    {
        string amountWords = NumberToWords(amount);
        string qrContent = $"qr_code:{orderNo}";

        var lines = new List<string>
        {
            "═════════════════════════",
            $"  {StoreName}",
            "═════════════════════════",
            "",
            "    RECHARGE TICKET",
            "",
            $"  AMOUNT:  ${amount}",
            $"  {amountWords}",
            "",
            $"  DATE: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"  ORDER: {orderNo}",
            "",
            "═════════════════════════",
            $"  {StoreAddr}",
            $"  Tel: {StorePhone}",
            $"  Email: {StoreEmail}",
            "═════════════════════════",
        };

        _lastPrintContent = lines;
        _lastQRContent = qrContent;
        _reprintCount = 0;

        StartCoroutine(PrintAndCut(lines, qrContent));
    }

    /// <summary>
    /// 打印兑换凭条
    /// </summary>
    public void PrintExchangeTicket(int amount)
    {
        var lines = new List<string>
        {
            "═════════════════════════",
            $"  {StoreName}",
            "═════════════════════════",
            "",
            "    REDEEM TICKET",
            "",
            $"  AMOUNT:  ${amount}",
            $"  DATE: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "═════════════════════════",
        };

        _lastPrintContent = lines;
        _lastQRContent = null;
        _reprintCount = 0;

        StartCoroutine(PrintAndCut(lines, null));
    }

    IEnumerator PrintAndCut(List<string> lines, string qrContent)
    {
        // 逐行发送打印指令 (使用框架 SBoxSandbox)
        foreach (string line in lines)
        {
            SBoxSandbox.PrinterMessage(line);
            yield return new WaitForSeconds(0.05f);
        }

        // 打印 QR 码 (如果有)
        if (!string.IsNullOrEmpty(qrContent))
        {
            Texture2D qrTex = GenerateQR(qrContent, 256);
            // 转位图后发送给打印机...
            // SBoxSandbox.PrinterBitmap(qrBytes);
        }

        // 切纸
        yield return new WaitForSeconds(1f);
        SBoxSandbox.PrinterPaperCut();
    }

    /// <summary>
    /// 重打上次凭条
    /// </summary>
    public void RePrintLast()
    {
        if (_reprintCount >= MAX_REPRINT)
        {
            TipsMgr.Instance.ShowBubbleTips("已达最大重打次数", transform);
            return;
        }
        if (_lastPrintContent == null)
        {
            TipsMgr.Instance.ShowBubbleTips("无打印记录", transform);
            return;
        }

        _reprintCount++;
        StartCoroutine(PrintAndCut(_lastPrintContent, _lastQRContent));
    }

    /// <summary>生成 QR 码 (使用 zxing.unity.dll)</summary>
    Texture2D GenerateQR(string content, int size)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = size,
                Height = size,
                Margin = 1
            }
        };

        var colors = writer.Write(content);

        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        tex.SetPixels32(colors);
        tex.Apply();
        return tex;
    }

    /// <summary>数字转英文金额</summary>
    string NumberToWords(int amount)
    {
        if (amount == 0) return "ZERO DOLLARS ONLY";
        if (amount >= 10000) return $"{amount} DOLLARS ONLY";

        string[] ones = {"","ONE","TWO","THREE","FOUR","FIVE","SIX","SEVEN","EIGHT","NINE"};
        string[] teens = {"TEN","ELEVEN","TWELVE","THIRTEEN","FOURTEEN","FIFTEEN",
                          "SIXTEEN","SEVENTEEN","EIGHTEEN","NINETEEN"};
        string[] tens = {"","","TWENTY","THIRTY","FORTY","FIFTY","SIXTY","SEVENTY","EIGHTY","NINETY"};

        string r = "";
        if (amount >= 1000) { r += $"{ones[amount/1000]} THOUSAND "; amount %= 1000; }
        if (amount >= 100) { r += $"{ones[amount/100]} HUNDRED "; amount %= 100; }
        if (amount >= 20) { r += $"{tens[amount/10]} "; amount %= 10; }
        if (amount >= 10) { r += $"{teens[amount-10]} "; amount = 0; }
        if (amount > 0) r += $"{ones[amount]} ";
        return $"{r}DOLLARS ONLY";
    }
}
```

---

## 10. 阶段六：本地订单容错系统

> **时间**: 第 10 周 | **优先级**: P0
> **产出**: AES加密本地存储、断网自动补发、过期订单处理

### 10.1 使用现有框架能力

- `Application.persistentDataPath` — 本地文件路径
- `AESScript` — 加密 (阶段一)
- `CloudNet.MqttPublish()` — 补发通信
- `Timer.LoopAction()` — 定时检查 (框架已有)
- `SBoxModel.Instance.USN` — 机台标识

### 10.2 核心代码

```csharp
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LocalOrderState : MonoSingleton<LocalOrderState>
{
    private const string FILE_PREFIX = "LocalOrder_";
    private const int EXPIRE_MINUTES = 20;
    private const int RESCAN_INTERVAL = 30;

    private string _aesKey = "LocalFileEncKey_32BytesMUST__!";
    private byte[] _iv = new byte[16];

    /// <summary>当前订单数据</summary>
    public LocalOrderData CurrentOrder = new();

    void Start()
    {
        // 联网后 3 秒启动补发
        LogicMessenger.AddListener(MessageName.Event_InitViewFinish, _ =>
        {
            Invoke(nameof(StartResend), 3f);
        });
    }

    public void SaveOrder(string precode, int amount)
    {
        CurrentOrder.time = DateTime.Now.ToString("yyyyMMddHHmmss");
        CurrentOrder.precode = precode;
        CurrentOrder.amount = amount.ToString();
        CurrentOrder.machineCode = SBoxModel.Instance.USN;
        CurrentOrder.action = "machine/HD_CMRO/1";

        string json = JsonConvert.SerializeObject(CurrentOrder);
        string enc = AESScript.Encrypt(json, _aesKey, _iv);
        string path = Path.Combine(Application.persistentDataPath, $"{FILE_PREFIX}{precode}.json");
        File.WriteAllText(path, enc);
        Debug.Log($"[LocalOrder] 已保存: {path}");
    }

    public void UpdateAmount(string precode, int newAmount)
    {
        CurrentOrder.amount = newAmount.ToString();
        SaveOrder(precode, newAmount);
    }

    public void DeleteOrder(string precode)
    {
        string path = Path.Combine(Application.persistentDataPath, $"{FILE_PREFIX}{precode}.json");
        if (File.Exists(path)) File.Delete(path);
    }

    void StartResend()
    {
        StartCoroutine(ResendLoop());
    }

    IEnumerator ResendLoop()
    {
        while (true)
        {
            string[] files = Directory.GetFiles(
                Application.persistentDataPath, $"{FILE_PREFIX}*.json");

            if (files.Length > 0)
            {
                Debug.Log($"[LocalOrder] 发现 {files.Length} 个待补发订单");
                foreach (var file in files)
                    yield return StartCoroutine(ProcessOrder(file));
            }

            yield return new WaitForSeconds(RESCAN_INTERVAL);
        }
    }

    IEnumerator ProcessOrder(string filePath)
    {
        try
        {
            string enc = File.ReadAllText(filePath);
            string json = AESScript.Decrypt(enc, _aesKey, _iv);
            var order = JsonConvert.DeserializeObject<LocalOrderData>(json);

            // 检查过期
            if (DateTime.TryParseExact(order.time, "yyyyMMddHHmmss",
                null, System.Globalization.DateTimeStyles.None, out var orderTime))
            {
                if ((DateTime.Now - orderTime).TotalMinutes > EXPIRE_MINUTES)
                {
                    Debug.LogWarning($"[LocalOrder] 订单过期: {order.precode}");
                    TipsMgr.Instance.ShowBubbleTips("存在过期订单", transform);
                    yield break;
                }
            }

            // 补发
            bool done = false;
            bool success = false;

            var handler = new Action<string>((resp) =>
            {
                var r = JsonUtility.FromJson<BaseResp>(resp);
                success = (r.code == 0);
                done = true;
            });

            NetMessenger.AddListener<string>(order.action, handler);
            CloudNet.Instance.MqttPublish(order.action, JsonConvert.SerializeObject(order));

            float wait = 0;
            while (!done && wait < 10f)
            {
                yield return new WaitForSeconds(0.1f);
                wait += 0.1f;
            }
            NetMessenger.RemoveListener<string>(order.action, handler);

            if (success)
            {
                // 补打凭条
                PrinterScript.Instance.PrintRechargeTicket(
                    int.Parse(order.amount ?? "0"), order.precode);
                File.Delete(filePath);
                Debug.Log($"[LocalOrder] 补发成功: {order.precode}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalOrder] 处理异常: {ex.Message}");
        }
    }
}

[Serializable]
public class LocalOrderData
{
    public string time;
    public string precode;
    public string amount;
    public string action;
    public string machineCode;
}

[Serializable]
public class BaseResp { public int code; public string message; }
```

---

## 11. 阶段七：管理后台 + 机台激活

> **时间**: 第 11 周 | **优先级**: P1
> **产出**: 扩展管理后台、机台激活流程

### 11.1 管理后台 — 在现有 IOCanvasView 基础上扩展

现有框架已有完整的 `IOCanvasView/IOCanvasManager/IOCanvasModel` 管理后台系统（密码验证、参数设置、账目报表、时间设置、彩金设置）。

MoneyBox 需要新增的菜单项：
- **BGMgrCassetteSetting** — 钱箱参数 (出钞面额、预警阈值)
- **BGMgrBillAcceptorSetting** — 纸币器设置 (支持面额列表)
- **BGMgrPrinterSetting** — 打印机设置 (打印浓度、切纸模式)
- **BGMgrNetworkSetting** — 云端服务器地址配置

**实现方式**: 在 `IOCanvasView` 中添加新 Section/Function，复用现有的密码→菜单→参数面板流程。

### 11.2 机台激活 — 核心代码

```csharp
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ActivateMachinePanel : MonoBehaviour
{
    public Text MachineCodeText;
    public InputField ActivateCodeInput;
    public Button ConfirmBtn;
    public Text TipText;

    void OnEnable()
    {
        // 显示机台唯一标识 (使用框架 SBoxModel)
        MachineCodeText.text = $"机台编号: {SBoxModel.Instance.USN}";
        ActivateCodeInput.text = "";
        TipText.text = "请输入激活码";
    }

    void Start()
    {
        ConfirmBtn.onClick.AddListener(() => StartCoroutine(Activate()));
    }

    IEnumerator Activate()
    {
        string code = ActivateCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code)) { TipText.text = "激活码不能为空"; yield break; }

        var body = new
        {
            activationCode = code,
            machineCode = SBoxModel.Instance.USN,
            lang = "en"
        };

        string encBody = AESScript.Encrypt(JsonUtility.ToJson(body), "cloud_key");

        string url = $"https://{CloudNet.Instance.ServerHost}:{CloudNet.Instance.ServerPort}/activate";
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(encBody));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCert();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            var resp = JsonUtility.FromJson<ActivateResp>(
                AESScript.Decrypt(req.downloadHandler.text, "cloud_key"));

            switch (resp.code)
            {
                case 0:
                    TipText.text = "激活成功! 正在重新登录...";
                    yield return new WaitForSeconds(2);
                    CloudNet.Instance.StartCloudConnection();
                    gameObject.SetActive(false);
                    break;
                case 556: TipText.text = "激活码无效"; break;
                case 557: TipText.text = "机台已被禁用"; break;
                case 41801: TipText.text = "激活码不存在"; break;
                default: TipText.text = resp.message ?? "激活失败"; break;
            }
        }
    }

    [System.Serializable] class ActivateResp { public int code; public string message; }
}
```

---

## 12. 阶段八：集成调试与真机部署

> **时间**: 第 12-14 周 | **优先级**: P0
> **产出**: 完整功能联调、异常测试、真机部署

### 12.1 编辑器调试快捷键 (利用框架已有机制)

```csharp
#if UNITY_EDITOR
void Update()
{
    if (Input.GetKeyDown(KeyCode.F1))
        SBoxMessenger.Broadcast(MessageName.Event_Sandbox_ListenerBillIn, 100);
    if (Input.GetKeyDown(KeyCode.F2))
        SBoxMessenger.Broadcast(MessageName.Event_Sandbox_ListenerBillStacked, 100);
    if (Input.GetKeyDown(KeyCode.F3))
        FindObjectOfType<IOCanvasView>()?.gameObject.SetActive(true);
}
#endif
```

### 12.2 测试清单

| 类别 | 测试项 | 验证点 |
|------|--------|--------|
| 硬件 | SBox 初始化 | 算法卡 + 底板就绪 |
| 网络 | HTTP 登录 | 正常/超时/机台未激活 |
| 网络 | MQTT 连接 | 连接/断线重连/心跳 |
| 投币 | 正常投币 | 插入→核准→打印凭条 |
| 投币 | 打印机故障 | 拒收纸币 |
| 投币 | 超额投币 | 拒收+提示 |
| 兑换 | 正常扫码 | 扫码→确认→出钞→打印 |
| 兑换 | 余额不足 | 金额校验失败 |
| 容错 | 断电恢复 | 重启后自动补发 |
| 容错 | 断网恢复 | 联网后自动补发 |
| 后台 | 密码验证 | 三级权限 |
| 激活 | 输入激活码 | 各状态码处理 |

### 12.3 真机部署步骤

```bash
# 1. 打包
Unity → File → Build Settings → Android → Build

# 2. 安装
adb install -r MoneyBox_v1.0.apk

# 3. 查看日志
adb logcat -s Unity:V MoneyBox:V SBoxApi:V

# 4. 确认 SBox 硬件通信
adb logcat | grep "SBoxInit"

# 5. 确认云端连接
adb logcat | grep "CloudNet"
```

---

## 附录 A：项目文件结构 (新增文件)

```
Assets/Scripts/Game/          ← Game.dll (新增业务层)
├── MoneyBoxMain.cs           — 启动流程 (替代 Load.cs)
├── CloudNet.cs               — HTTP + MQTT 云端通信
├── AESScript.cs              — AES 加解密
├── MainGameView.cs           — 主界面控制器
├── SandBoxScript.cs          — 投币充值控制器
├── CashExchangePanel.cs      — 现金兑换面板
├── PrinterScript.cs          — 打印机凭条系统
├── LocalOrderState.cs        — 本地订单持久化
├── ActivateMachinePanel.cs   — 机台激活面板
├── ErrorPanelManager.cs      — 错误提示管理
└── DataModels.cs             — 全部数据模型定义

Assets/Scripts/Base/          ← Base.dll (无需修改)
├── (现有框架代码保持不变)
│
Assets/Plugins/               ← (无需修改)
├── SandboxPlugin-V1.0.0.jar  — SBox 底板驱动
├── rctlibrary-debug.aar      — 算法卡驱动
├── zxing.unity.dll           — QR 码库
└── ...

Assets/Resources/
└── Languages/
    └── strings.txt            — 多语言文本 (格式: key|EN|CN|ES)
```

---

## 附录 B：各阶段依赖关系

```
阶段一 (云网络)     ← 无依赖，最先做
    ↓
阶段二 (主界面)     ← 依赖阶段一 (需要 CloudNet 事件)
    ↓
阶段三 (投币充值)   ← 依赖阶段一 (CloudNet) + 阶段二 (MainGameView)
阶段四 (现金兑换)   ← 依赖阶段一 + 阶段二 (扫码)
    ↓
阶段五 (打印机)     ← 依赖阶段三/四 (凭条内容来源)
阶段六 (本地订单)   ← 依赖阶段三/四 (订单产生点)
阶段七 (后台+激活)  ← 依赖阶段一 (CloudNet) + 现有 IOCanvas
    ↓
阶段八 (集成调试)   ← 所有模块就绪
```

---

> **文档版本**: 2.0 (基于 SBoxBase 框架实际代码)
> **制定日期**: 2026-06-18
> **总周期**: 14 周 (约 3.5 个月)
> **现有框架复用率**: ~40% (硬件层、网络层、UI框架、基础架构)
> **新增代码量**: 约 3000-4000 行 C# (10 个核心模块)
