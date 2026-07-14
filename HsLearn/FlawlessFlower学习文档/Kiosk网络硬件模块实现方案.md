# Kiosk 自助终端网络+硬件模块实现方案

> 基于 MoneyBox 项目经验，从零搭建同类项目的完整方案

---

## 目录

1. [整体架构](#1-整体架构)
2. [文件结构](#2-文件结构)
3. [步骤1: 基础框架](#3-步骤1-基础框架)
4. [步骤2: 加密模块](#4-步骤2-加密模块)
5. [步骤3: HTTP 登录](#5-步骤3-http-登录)
6. [步骤4: MQTT 通信层](#6-步骤4-mqtt-通信层)
7. [步骤5: 可靠消息发送](#7-步骤5-可靠消息发送)
8. [步骤6: 网络总控](#8-步骤6-网络总控)
9. [步骤7: 投币充值模块](#9-步骤7-投币充值模块)
10. [步骤8: 打印机模块](#10-步骤8-打印机模块)
11. [步骤9: 现金兑换模块](#11-步骤9-现金兑换模块)
12. [步骤10: 断电保护模块](#12-步骤10-断电保护模块)
13. [步骤11: Unity 接入](#13-步骤11-unity-接入)
14. [API 接口速查表](#14-api-接口速查表)

---

## 1. 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        Unity MonoBehaviour                       │
│                                                                 │
│  AppBootstrapper (场景入口, 挂载到 GameObject)                    │
│       │                                                         │
│       ├── NetManager           ← 网络总控 (唯一对外接口)          │
│       │     ├── LoginService   ← HTTP 登录                      │
│       │     ├── MqttClient     ← MQTT 收发                      │
│       │     ├── CryptoProvider ← AES 加解密 + 密钥管理           │
│       │     └── RetryPolicy    ← 超时重试/断线重连               │
│       │                                                         │
│       ├── BillAcceptor         ← 投币充值 (纸币流程)             │
│       │     ├── LocalOrderStore ← 本地订单持久化                 │
│       │     └── OrderResender   ← 订单补发                       │
│       │                                                         │
│       ├── CashDispenser        ← 现金兑换 (出钞流程)             │
│       │                                                         │
│       ├── TicketPrinter        ← 打印机 (凭条/报表)              │
│       │                                                         │
│       └── QrCodeScanner        ← 扫码枪输入                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. 文件结构

```
Assets/
├── Scenes/
│   └── MainScene.unity
│
├── Scripts/
│   ├── Framework/
│   │   ├── EventCenter.cs           // 全局事件中心 (观察者模式)
│   │   ├── MonoSingleton.cs         // Mono 单例基类
│   │   └── AppBootstrapper.cs       // ⭐ 场景启动入口
│   │
│   ├── Network/                     // ===== 网络模块 =====
│   │   ├── CryptoProvider.cs        // AES/MD5 加解密 + 密钥管理
│   │   ├── LoginService.cs          // HTTP POST /login /activate
│   │   ├── MqttClient.cs            // MQTT 底层收发
│   │   ├── MqttResponse.cs          // 响应数据类型
│   │   ├── NetManager.cs            // ⭐ 网络总控 (唯一对外)
│   │   └── NetConfig.cs             // 服务器地址配置
│   │
│   ├── Requests/                    // ===== 请求体定义 =====
│   │   ├── RechargeRequests.cs      // 充值相关
│   │   ├── WithdrawRequests.cs      // 兑换相关
│   │   ├── AdminRequests.cs         // 管理后台相关
│   │   └── MachineRequests.cs       // 机台相关
│   │
│   ├── Modules/                     // ===== 业务模块 =====
│   │   ├── BillAcceptor.cs          // ⭐ 投币充值
│   │   ├── CashDispenser.cs         // ⭐ 现金兑换
│   │   ├── TicketPrinter.cs         // ⭐ 打印机
│   │   ├── QrCodeScanner.cs         // 扫码枪
│   │   ├── LocalOrderStore.cs       // ⭐ 本地订单持久化
│   │   ├── OrderResender.cs         // ⭐ 订单补发
│   │   ├── ActivationManager.cs     // 机台激活
│   │   └── PhotoCarousel.cs         // 广告轮播
│   │
│   └── UI/
│       ├── MainPanel.cs
│       ├── RechargePanel.cs
│       ├── ExchangePanel.cs
│       └── AdminPanel.cs
│
├── GameRes/Prefabs/
│   ├── MainPanel.prefab
│   └── ...
│
└── Plugins/
    ├── MQTTnet.dll                   // MQTT 库
    └── Newtonsoft.Json.dll           // JSON 库
```

---

## 3. 步骤1: 基础框架

### 3.1 EventCenter — 模块间解耦

```csharp
// Framework/EventCenter.cs
public class EventCenter
{
    private static EventCenter _instance;
    public static EventCenter Instance => _instance ??= new EventCenter();

    private readonly Dictionary<string, Delegate> _events = new();

    public void On<T>(string eventName, Action<T> handler)
    {
        if (_events.TryGetValue(eventName, out var d))
            _events[eventName] = Delegate.Combine(d, handler);
        else
            _events[eventName] = handler;
    }

    public void On(string eventName, Action handler)
    {
        if (_events.TryGetValue(eventName, out var d))
            _events[eventName] = Delegate.Combine(d, handler);
        else
            _events[eventName] = handler;
    }

    public void Emit<T>(string eventName, T arg)
    {
        if (_events.TryGetValue(eventName, out var d) && d is Action<T> action)
            action.Invoke(arg);
    }

    public void Emit(string eventName)
    {
        if (_events.TryGetValue(eventName, out var d) && d is Action action)
            action.Invoke();
    }
}

// 事件名常量
public static class Events
{
    public const string NET_READY = "NET_READY";              // 网络就绪
    public const string NET_DISCONNECTED = "NET_DISCONNECTED";// 网络断开
    public const string BILL_RECEIVED = "BILL_RECEIVED";      // 收到纸币 (参数: 面额)
    public const string BILL_STACKED = "BILL_STACKED";        // 纸币存入钱箱 (参数: 金额)
    public const string BILL_REJECTED = "BILL_REJECTED";      // 纸币被拒收
    public const string RECHARGE_COMPLETE = "RECHARGE_COMPLETE"; // 充值完成
    public const string WITHDRAW_COMPLETE = "WITHDRAW_COMPLETE"; // 兑换完成
    public const string PRINTER_ERROR = "PRINTER_ERROR";       // 打印机故障
    public const string CASHBOX_LOW = "CASHBOX_LOW";           // 钱箱不足
    public const string CASHBOX_EMPTY = "CASHBOX_EMPTY";       // 钱箱空
}
```

### 3.2 AppBootstrapper — 启动入口

```csharp
// Framework/AppBootstrapper.cs
public class AppBootstrapper : MonoBehaviour
{
    public NetManager NetManager { get; private set; }
    public BillAcceptor BillAcceptor { get; private set; }
    public CashDispenser CashDispenser { get; private set; }
    public TicketPrinter TicketPrinter { get; private set; }
    public static AppBootstrapper Instance { get; private set; }

    async void Start()
    {
        Instance = this;

        // ① 初始化网络
        NetManager = new NetManager();
        await NetManager.StartAsync();

        // ② 网络就绪后初始化业务模块
        BillAcceptor = new BillAcceptor(NetManager);
        CashDispenser = new CashDispenser(NetManager);
        TicketPrinter = new TicketPrinter(NetManager);

        EventCenter.Instance.Emit(Events.NET_READY);
    }
}
```

---

## 4. 步骤2: 加密模块 (CryptoProvider)

```csharp
// Network/CryptoProvider.cs
using System.Security.Cryptography;
using System.Text;

public class CryptoProvider
{
    // ⚠️ 生产环境应从安全存储读取, 不要硬编码
    private const string FIXED_KEY_BASE64 = "nSFQxn9+lZBLu1by7E9ibvZEPljhvMC3GQo9plFR9RI=";

    private byte[] _fixedKey;
    private byte[] _dynamicKey;
    private byte[] _bankKey;

    public CryptoProvider()
    {
        _fixedKey = Convert.FromBase64String(FIXED_KEY_BASE64);
    }

    // ===== 密钥管理 =====
    public void SetDynamicKey(string base64Key) => _dynamicKey = Convert.FromBase64String(base64Key);
    public void SetBankKey(string base64Key) => _bankKey = Convert.FromBase64String(base64Key);

    // ===== 固定密钥 (HTTP登录阶段) =====
    public string EncryptFixed(string plainText) => Encrypt(plainText, _fixedKey);
    public string DecryptFixed(string cipherBase64) => Decrypt(cipherBase64, _fixedKey);

    // ===== 动态密钥 (MQTT通信阶段) =====
    public string EncryptDynamic(string plainText) => Encrypt(plainText, _dynamicKey);
    public string DecryptDynamic(string cipherBase64) => Decrypt(cipherBase64, _dynamicKey);

    // ===== 银行卡密钥 (二维码加解密) =====
    public string EncryptBank(string plainText) => Encrypt(plainText, _bankKey);
    public string DecryptBank(string cipherBase64) => Decrypt(cipherBase64, _bankKey);

    // ===== AES-256-CBC 核心 =====
    private string Encrypt(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();           // 随机 IV
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // IV + 密文 → Base64
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(result);
    }

    private string Decrypt(string cipherBase64, byte[] key)
    {
        var fullData = Convert.FromBase64String(cipherBase64);

        // 提取 IV (前16字节)
        var iv = new byte[16];
        Buffer.BlockCopy(fullData, 0, iv, 0, 16);

        // 提取密文
        var cipherBytes = new byte[fullData.Length - 16];
        Buffer.BlockCopy(fullData, 16, cipherBytes, 0, cipherBytes.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

---

## 5. 步骤3: HTTP 登录 (LoginService)

```csharp
// Network/LoginService.cs
using Newtonsoft.Json;
using UnityEngine.Networking;

// --- 数据类型 ---
public class LoginRequest
{
    public string MachineCode { get; set; }
    public string Lang { get; set; } = "en";
}

public class LoginResponse
{
    public int Code { get; set; }
    public string ErrMsg { get; set; }
    public LoginResponseData Data { get; set; }
}

public class LoginResponseData
{
    public string Ctxt { get; set; }  // AES 密文
}

public class LoginResult
{
    public string MqttBroker { get; set; }
    public string AccessToken { get; set; }
    public string DynamicKey { get; set; }
    public string BankKey { get; set; }
}

// --- 服务 ---
public class LoginService
{
    private readonly CryptoProvider _crypto;
    private readonly string _serverUrl;

    public LoginService(CryptoProvider crypto, string serverUrl)
    {
        _crypto = crypto;
        _serverUrl = serverUrl;
    }

    /// <summary>POST /login — 返回 MQTT 连接参数</summary>
    public async Task<LoginResult> LoginAsync(string machineCode)
    {
        // ① 构造 + 加密请求
        var plain = JsonConvert.SerializeObject(new LoginRequest { MachineCode = machineCode });
        var encrypted = _crypto.EncryptFixed(plain);

        // ② 发送 HTTP POST
        var form = new WWWForm();
        form.AddField("ctxt", encrypted);
        form.AddField("lang", "en");

        using var request = UnityWebRequest.Post($"{_serverUrl}/login", form);
        request.timeout = 5;
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            throw new Exception($"Login HTTP failed: {request.error}");

        // ③ 解析响应
        var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);

        if (response.Code == 555 || response.Code == 557)
            throw new NeedActivationException(response.Code, response.ErrMsg);

        if (response.Code != 0)
            throw new Exception($"Login failed: [{response.Code}] {response.ErrMsg}");

        // ④ 解密 MQTT 参数
        if (string.IsNullOrEmpty(response.Data?.Ctxt))
            throw new Exception("Login response has no ctxt");

        var decrypted = _crypto.DecryptFixed(response.Data.Ctxt);
        return JsonConvert.DeserializeObject<LoginResult>(decrypted);
    }

    /// <summary>POST /activate — 激活机台</summary>
    public async Task<bool> ActivateAsync(string machineCode, string activationCode)
    {
        var plain = JsonConvert.SerializeObject(new
        {
            machineCode = machineCode,
            activationCode = activationCode,
            lang = "en"
        });
        var encrypted = _crypto.EncryptFixed(plain);

        var form = new WWWForm();
        form.AddField("ctxt", encrypted);
        form.AddField("lang", "en");

        using var request = UnityWebRequest.Post($"{_serverUrl}/activate", form);
        request.timeout = 5;
        await request.SendWebRequest();

        var response = JsonConvert.DeserializeObject<LoginResponse>(request.downloadHandler.text);
        return response.Code == 0;
    }
}

public class NeedActivationException : Exception
{
    public int Code { get; }
    public NeedActivationException(int code, string msg) : base(msg) => Code = code;
}
```

---

## 6. 步骤4: MQTT 通信层 (MqttClient)

```csharp
// Network/MqttClient.cs
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

public class MqttResponse
{
    public string Action { get; set; }
    public int Code { get; set; }
    public string ErrMsg { get; set; }
    public string ModuleType { get; set; }
    public ReturnDataContent Data { get; set; }
    public bool IsSuccess => Code == 0;
}

public class ReturnDataContent
{
    public string Ctxt { get; set; }
    // 按需添加更多字段...
}

/// <summary>MQTT 底层 — 收发封装, 一个 Topic = 一个 TaskCompletionSource</summary>
public class MqttClient
{
    private IMqttClient _client;
    private readonly MqttFactory _factory = new();
    private readonly CryptoProvider _crypto;

    // ⭐ 核心: 每个请求用 TaskCompletionSource 精准等待
    private readonly Dictionary<string, TaskCompletionSource<MqttResponse>> _pending = new();

    public bool IsConnected => _client?.IsConnected ?? false;

    public MqttClient(CryptoProvider crypto) => _crypto = crypto;

    // ===== 连接 =====
    public async Task ConnectAsync(string brokerUri, CancellationToken ct = default)
    {
        _client?.Dispose();
        _client = _factory.CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMessage;

        var options = new MqttClientOptionsBuilder()
            .WithWebSocketServer(o => o.WithUri(brokerUri))
            .WithTimeout(TimeSpan.FromSeconds(3))
            .Build();

        await _client.ConnectAsync(options, ct);
    }

    // ===== 订阅 =====
    public async Task SubscribeAsync(string topic)
    {
        var options = _factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(topic).Build();
        await _client.SubscribeAsync(options);
    }

    // ===== 发送 + 等待响应 (一次搞定) =====
    public async Task<MqttResponse> RequestAsync(string topic, string payloadJson,
        int timeoutMs = 15000, CancellationToken ct = default)
    {
        // ① 创建等待器
        var waitKey = topic.Replace("/1", "");
        var tcs = new TaskCompletionSource<MqttResponse>();
        lock (_pending) { _pending[waitKey] = tcs; }

        try
        {
            // ② 加密 + 发送
            var encrypted = _crypto.EncryptDynamic(payloadJson);
            var wrapper = JsonConvert.SerializeObject(new { ctxt = encrypted, lang = "en" });

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(wrapper)
                .Build();

            await _client.PublishAsync(msg, ct);

            // ③ 等待响应 (带超时)
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                var result = await tcs.Task.WaitAsync(linked.Token);
                return result;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException($"MQTT request timeout: {topic} ({timeoutMs}ms)");
            }
        }
        finally
        {
            lock (_pending) { _pending.Remove(waitKey); }
        }
    }

    // ===== 消息到达 → 唤醒等待者 =====
    private Task OnMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var rawJson = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.Array);
        var response = JsonConvert.DeserializeObject<MqttResponse>(rawJson);

        // 解密 (如果 ctxt 非空)
        if (!string.IsNullOrEmpty(response.Data?.Ctxt))
        {
            var decrypted = _crypto.DecryptDynamic(response.Data.Ctxt);
            var innerData = JsonConvert.DeserializeObject<ReturnDataContent>(decrypted);
            response.Data = innerData;
        }

        // 唤醒等待者
        var key = $"{response.ModuleType}/{response.Action}";
        lock (_pending)
        {
            if (_pending.TryGetValue(key, out var tcs))
                tcs.TrySetResult(response);
        }

        return Task.CompletedTask;
    }

    public void Dispose() => _client?.Dispose();
}
```

---

## 7. 步骤5: 可靠消息发送

```csharp
// Network/NetManager.cs (部分 — 可靠发送)
public class NetManager
{
    // ... 其他字段 ...

    /// <summary>带重试的消息发送 (最多重试3次)</summary>
    public async Task<MqttResponse> SendWithRetryAsync(string topic, string payload,
        int maxRetries = 3, int retryDelayMs = 1000)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await _mqtt.RequestAsync(topic, payload);

                // 服务器繁忙 → 重试
                if (response.Code == 11111 && i < maxRetries - 1)
                {
                    await Task.Delay(retryDelayMs);
                    continue;
                }

                return response;
            }
            catch (TimeoutException) when (i < maxRetries - 1)
            {
                await Task.Delay(retryDelayMs);
            }
            catch when (!_mqtt.IsConnected)
            {
                // MQTT 断开 → 重连后再试
                await ReconnectAsync();
            }
        }

        // 所有重试都失败
        return new MqttResponse { Code = 99999, ErrMsg = "All retries exhausted" };
    }

    /// <summary>断线重连 (自动重新登录 + MQTT 连接)</summary>
    private async Task ReconnectAsync()
    {
        for (int j = 0; j < 100; j++)
        {
            try
            {
                var result = await _loginService.LoginAsync(MachineCode);
                _crypto.SetDynamicKey(result.DynamicKey);
                await _mqtt.ConnectAsync(result.MqttBroker);
                await _mqtt.SubscribeAsync("machine/HD_login/1");

                var authResult = await _mqtt.RequestAsync("machine/HD_login/1",
                    JsonConvert.SerializeObject(new { token = result.AccessToken }));

                if (authResult.IsSuccess) return;
            }
            catch { }

            await Task.Delay(5600);
        }
    }
}
```

---

## 8. 步骤6: 网络总控 (NetManager)

```csharp
// Network/NetManager.cs (完整)
public class NetManager
{
    private readonly CryptoProvider _crypto;
    private readonly LoginService _loginService;
    private readonly MqttClient _mqtt;
    private readonly NetConfig _config;

    public string MachineCode { get; private set; }
    public bool IsReady { get; private set; }

    // ---- 服务器地址配置 ----
    public class NetConfig
    {
        // 4 套环境, 根据版本号自动选择
        public static readonly string[] Servers = new[]
        {
            "https://cbapi.xxx.com/machine",       // 美国正式
            "http://8.138.xxx.xxx:26266/machine",   // 香港
            "http://192.168.3.174:26266/machine",   // 本地测试
        };

        public static string GetServerUrl()
        {
#if UNITY_EDITOR
            return Servers[2]; // 编辑器默认本地
#else
            // 根据 Application.version 选择
            return Servers[0];
#endif
        }
    }

    public NetManager()
    {
        _crypto = new CryptoProvider();
        _config = new NetConfig();
        _loginService = new LoginService(_crypto, NetConfig.GetServerUrl());
        _mqtt = new MqttClient(_crypto);
    }

    // ===== 启动 =====
    public async Task StartAsync()
    {
        // ① 获取机台 ID
        MachineCode = await GetMachineCodeAsync();
        if (string.IsNullOrEmpty(MachineCode))
            throw new Exception("Cannot get MachineCode");

        // ② HTTP 登录
        LoginResult loginResult;
        try
        {
            loginResult = await _loginService.LoginAsync(MachineCode);
        }
        catch (NeedActivationException ex)
        {
            // 需要激活 → 触发激活流程
            EventCenter.Instance.Emit("ACTIVATION_REQUIRED", ex.Code);
            return;
        }

        // ③ 切换到动态密钥
        _crypto.SetDynamicKey(loginResult.DynamicKey);
        _crypto.SetBankKey(loginResult.BankKey);

        // ④ MQTT 连接 + 认证
        await _mqtt.ConnectAsync(loginResult.MqttBroker);
        await _mqtt.SubscribeAsync("machine/HD_login/1");

        var authResponse = await _mqtt.RequestAsync("machine/HD_login/1",
            JsonConvert.SerializeObject(new { token = loginResult.AccessToken }));

        if (!authResponse.IsSuccess)
            throw new Exception($"MQTT auth failed: {authResponse.Code}");

        IsReady = true;
        EventCenter.Instance.Emit(Events.NET_READY);
    }

    // ===== 对外接口: 发送请求 =====
    public Task<MqttResponse> SendAsync(string topic, object payload)
        => SendWithRetryAsync(topic, JsonConvert.SerializeObject(payload));

    // ===== 机台 ID 获取: 4 级降级 =====
    private async Task<string> GetMachineCodeAsync()
    {
        // ① 本地缓存
        var cachePath = Application.persistentDataPath + "/MachineCode.json";
        if (File.Exists(cachePath))
            return File.ReadAllText(cachePath);

        // ② 硬件序列号 (具体实现取决于硬件平台)
        var usn = await GetHardwareUSNAsync();
        if (!string.IsNullOrEmpty(usn))
        {
            File.WriteAllText(cachePath, usn);
            return usn;
        }

        // ③ MAC 地址
        var mac = GetMacAddress();
        if (!string.IsNullOrEmpty(mac))
            return $"m:{mac}";

        // ④ UUID 兜底
        return $"u:{Guid.NewGuid()}";
    }

    private Task<string> GetHardwareUSNAsync()
    {
        // Android: 通过 SBox SDK 获取
        // 其他平台: 返回空, 降级到 MAC
        return Task.FromResult<string>(null);
    }

    private string GetMacAddress()
    {
        try
        {
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var active = nics.FirstOrDefault(n => n.OperationalStatus ==
                System.Net.NetworkInformation.OperationalStatus.Up);
            return active?.GetPhysicalAddress().ToString();
        }
        catch { return null; }
    }
}
```

---

## 9. 步骤7: 投币充值模块 (BillAcceptor)

```csharp
// Modules/BillAcceptor.cs — 完整投币充值流程
public class BillAcceptor
{
    private readonly NetManager _net;
    private readonly LocalOrderStore _orderStore;
    private readonly TicketPrinter _printer;

    private string _currentPrcode;    // 当前批次防重码
    private int _totalAmount;         // 当前批次累计金额
    private bool _isRecharging;       // 是否正在充值 (互斥锁)

    public BillAcceptor(NetManager net, LocalOrderStore orderStore, TicketPrinter printer)
    {
        _net = net;
        _orderStore = orderStore;
        _printer = printer;
    }

    // ===== 步骤1: 收到纸币 → 启动充值 =====
    public async Task OnBillReceived(int amount)
    {
        if (_isRecharging) return;  // 已在充值中, 防并发
        _isRecharging = true;

        try
        {
            // ① 检查网络
            if (!_net.IsReady)
            {
                await RejectBill("No network");
                return;
            }

            // ② 打印机检查
            if (!_printer.IsReady())
            {
                await RejectBill("Printer not ready");
                return;
            }

            // ③ 申请防重码 (如果还没有)
            if (string.IsNullOrEmpty(_currentPrcode))
            {
                var prcodeResponse = await _net.SendAsync(
                    "order/HD_NewPrcode/1", new { });

                if (!prcodeResponse.IsSuccess)
                {
                    await RejectBill($"Cannot get prcode: {prcodeResponse.Code}");
                    return;
                }
                _currentPrcode = prcodeResponse.Data.Prcode;
            }

            // ④ 检查金额上限
            if (_totalAmount + amount > MaxRechargeAmount)
            {
                await RejectBill($"Exceeds max recharge: {MaxRechargeAmount}");
                return;
            }

            // ⑤ 逐张纸币验证
            var checkResponse = await _net.SendAsync("order/HD_CheckPerCashIn/1",
                new CheckPerCashInRequest
                {
                    Prcode = _currentPrcode,
                    Amount = amount,
                    SeqNum = _currentBillSeq,
                    PlatTag = CurrentPlatTag,
                    Lang = "en",
                    RechargeScene = _currentScene
                });

            if (!checkResponse.IsSuccess)
            {
                await RejectBill($"Check failed: {checkResponse.Code}");
                return;
            }

            // ⑥ 验证通过 → 保存本地订单 (断电保护)
            _totalAmount += amount;
            _currentBillSeq++;
            _orderStore.SaveOrder(new LocalOrder
            {
                Time = DateTime.Now.ToString("yyyyMMddHHmmss"),
                Prcode = _currentPrcode,
                Amount = _totalAmount,
                Action = "order/HD_CMRO/1"
            });
        }
        finally
        {
            _isRecharging = false;
        }
    }

    // ===== 步骤2: 纸币存入钱箱 → 完成充值 =====
    public async Task OnBillStacked()
    {
        if (_totalAmount <= 0) return;

        // ① 发送确认充值请求
        var response = await _net.SendAsync("order/HD_CMRO/1",
            new ConfirmRechargeRequest
            {
                Amount = _totalAmount,
                Prcode = _currentPrcode,
                IsLocal = "0",
                Lang = "en"
            });

        if (response.IsSuccess)
        {
            // ② 打印充值凭条
            _printer.PrintRechargeTicket(new PrintTicketData
            {
                Amount = _totalAmount,
                OrderNo = response.Data.OrderNo,
                QrCode = response.Data.CodeContent,
                StoreName = response.Data.StoreInfo.StoreName
            });

            // ③ 删除本地订单
            _orderStore.DeleteOrder(_currentPrcode);

            // ④ 重置批次
            _currentPrcode = null;
            _totalAmount = 0;
        }
    }

    private async Task RejectBill(string reason)
    {
        Debug.LogWarning($"Bill rejected: {reason}");
        // 硬件拒收纸币 — 具体实现取决于硬件平台
        EventCenter.Instance.Emit(Events.BILL_REJECTED);
    }

    // 配置
    public int MaxRechargeAmount { get; set; } = 10000;
    private int _currentBillSeq = 1;
    public string CurrentPlatTag { get; set; } = "MF";
    private string _currentScene = "device";
}
```

---

## 10. 步骤8: 打印机模块 (TicketPrinter)

```csharp
// Modules/TicketPrinter.cs
public class PrintTicketData
{
    public int Amount { get; set; }
    public string OrderNo { get; set; }
    public string QrCode { get; set; }
    public string StoreName { get; set; }
    public string StoreAddr { get; set; }
    public string StoreTel { get; set; }
    public string TicketType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TicketPrinter
{
    private readonly NetManager _net;
    // private PhoenixPrinter _printer;  // 热敏打印机驱动 (项目特定)

    public TicketPrinter(NetManager net) => _net = net;

    // ===== 打印机状态检查 =====
    public bool IsReady()
    {
        // 检查逻辑:
        // ① 打印机是否在线
        // ② 是否有纸
        // ③ 是否卡纸/故障
        return true; // 实际需调用硬件驱动
    }

    // ===== 打印充值/兑换凭条 =====
    public async Task PrintRechargeTicket(PrintTicketData data)
    {
        if (!IsReady())
        {
            // 保存到本地, 等打印机恢复后补打
            return;
        }

        try
        {
            // ① 生成二维码图片
            var qrTexture = GenerateQRCode(data.QrCode, 256, 256);

            // ② 排版打印内容
            var document = new List<string>
            {
                data.StoreName,
                data.TicketType,
                $"${data.Amount:N0}",
                NumberToEnglish(data.Amount),
                data.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                $"Order: {data.OrderNo}",
                data.StoreAddr,
                data.StoreTel
            };

            // ③ 发送到打印机
            // printer.Reinitialize();
            // printer.PrintDocument(document);
            // printer.PrintImage(qrTexture);
            // printer.FormFeed();

            // ④ 等待打印完成
            await WaitForPrintComplete();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Print failed: {ex.Message}");
            EventCenter.Instance.Emit(Events.PRINTER_ERROR);
        }
    }

    // ===== 打印账目报表 =====
    public async Task PrintReport(List<string> lines)
    {
        // printer.Reinitialize();
        // printer.PrintDocument(lines);
        // printer.FormFeed();
        await WaitForPrintComplete();
    }

    private async Task WaitForPrintComplete()
    {
        // 轮询等待打印机就绪 (最多等18秒)
        for (int i = 0; i < 18; i++)
        {
            if (IsReady()) return;
            await Task.Delay(1000);
        }
    }

    // ===== 二维码生成 =====
    private Texture2D GenerateQRCode(string content, int width, int height)
    {
        // 使用 ZXing.Net 库
        // var writer = new ZXing.BarcodeWriter { ... };
        // return writer.Write(content);
        return null;
    }

    // ===== 数字转英文 =====
    private string NumberToEnglish(int number)
    {
        if (number == 0) return "ZERO";
        // ... 转换逻辑
        return $"{number} DOLLARS ONLY";
    }
}
```

---

## 11. 步骤9: 现金兑换模块 (CashDispenser)

```csharp
// Modules/CashDispenser.cs — 扫码兑换流程
public class CashDispenser
{
    private readonly NetManager _net;
    private readonly LocalOrderStore _orderStore;
    private readonly TicketPrinter _printer;

    public CashDispenser(NetManager net, LocalOrderStore orderStore, TicketPrinter printer)
    {
        _net = net;
        _orderStore = orderStore;
        _printer = printer;
    }

    // ===== 步骤1: 扫描二维码 → 查询订单 =====
    public async Task<ScanResult> ScanQrCode(string qrContent)
    {
        // ① 发送查询请求
        var response = await _net.SendAsync("order/HD_FindWithdrawalOrder/1",
            new { codeContent = qrContent, lang = "en" });

        if (!response.IsSuccess)
            return new ScanResult { Success = false, ErrorMsg = response.ErrMsg };

        // ② 解析银行数据
        var bankData = JsonConvert.DeserializeObject<BankActionData>(response.Data.ActionData);

        return new ScanResult
        {
            Success = true,
            CodeType = bankData.CodeType,        // 0=绑定 1=标准兑换 2=二次兑换
            TotalMoney = bankData.TotalMoney,
            AvailMoney = bankData.AvailMoney,
            OrderNo = bankData.OrderNo,
            PresetAmounts = bankData.PresetAmount?.Split(';')
        };
    }

    // ===== 步骤2: 确认兑换 → 出钞 =====
    public async Task<WithdrawResult> ExecuteWithdraw(int amount, string orderNo)
    {
        // ① 检查打印机
        if (!_printer.IsReady())
            return new WithdrawResult { Success = false, ErrorMsg = "Printer not ready" };

        // ② 保存本地订单 (断电保护)
        _orderStore.SaveOrder(new LocalOrder
        {
            Time = DateTime.Now.ToString("yyyyMMddHHmmss"),
            OrderNo = orderNo,
            Amount = amount,
            Action = "order/HD_ConfirmWithdrawalOrder/1"
        });

        // ③ 检查钱箱状态 (是否有足够现金)
        var cashboxResponse = await _net.SendAsync("admin/HD_GetCashBoxSettings/1",
            new { lang = "en" });

        if (!cashboxResponse.IsSuccess)
            return new WithdrawResult { Success = false, ErrorMsg = "Cannot get cashbox status" };

        // ④ 发送确认兑换请求
        var response = await _net.SendAsync("order/HD_ConfirmWithdrawalOrder/1",
            new
            {
                amount = amount,
                orderno = orderNo,
                isLocal = "0",
                lang = "en"
            });

        if (!response.IsSuccess)
        {
            _orderStore.DeleteOrder(orderNo);
            return new WithdrawResult { Success = false, ErrorMsg = response.ErrMsg };
        }

        // ⑤ 执行出钞 (硬件操作)
        var dispenseResult = await DispenseCash(response.Data.OutMoneyMethod);

        // ⑥ 出钞完成后 → 通知服务器
        var finishResponse = await _net.SendAsync("order/HD_FinishWithdrawalOrder/1",
            new { orderno = orderNo, lang = "en" });

        // ⑦ 打印兑换凭条 (如果部分出钞, 打印剩余金额)
        if (finishResponse.Data.IsPartialWithdrawal == "2")
        {
            _printer.PrintRechargeTicket(new PrintTicketData
            {
                Amount = int.Parse(finishResponse.Data.RemainingMoney),
                OrderNo = finishResponse.Data.NewOrder.OrderNo,
                QrCode = finishResponse.Data.NewOrder.CodeContent
            });
        }

        // ⑧ 删除本地订单
        _orderStore.DeleteOrder(orderNo);

        return new WithdrawResult { Success = true };
    }

    private async Task<bool> DispenseCash(int[] outMoneyMethod)
    {
        // 硬件出钞 — 调用钱箱驱动
        // CassetteScript.CashPresentFunc(outMoneyMethod);
        await Task.Delay(3000); // 模拟出钞时间
        return true;
    }
}

// --- 数据类型 ---
public class ScanResult
{
    public bool Success { get; set; }
    public string ErrorMsg { get; set; }
    public int CodeType { get; set; }
    public int TotalMoney { get; set; }
    public int AvailMoney { get; set; }
    public string OrderNo { get; set; }
    public string[] PresetAmounts { get; set; }
}

public class WithdrawResult
{
    public bool Success { get; set; }
    public string ErrorMsg { get; set; }
}

public class BankActionData
{
    public string OrderNo { get; set; }
    public int CodeType { get; set; }
    public int TotalMoney { get; set; }
    public int AvailMoney { get; set; }
    public int RestMoney { get; set; }
    public string PresetAmount { get; set; }
    public string DeviceId { get; set; }
}
```

---

## 12. 步骤10: 断电保护模块

```csharp
// Modules/LocalOrderStore.cs + OrderResender.cs
public class LocalOrder
{
    public string Time { get; set; }
    public string Prcode { get; set; }
    public string OrderNo { get; set; }
    public int Amount { get; set; }
    public string Action { get; set; }
    public string PlayerId { get; set; }
}

/// <summary>本地订单持久化 — AES加密存文件</summary>
public class LocalOrderStore
{
    private readonly CryptoProvider _crypto;
    private readonly string _storagePath;

    public LocalOrderStore(CryptoProvider crypto)
    {
        _crypto = crypto;
        _storagePath = Application.persistentDataPath;
    }

    // 保存订单
    public void SaveOrder(LocalOrder order)
    {
        var json = JsonConvert.SerializeObject(order);
        var encrypted = _crypto.EncryptDynamic(json);  // 用动态密钥加密
        var fileName = $"{_storagePath}/Order_{order.Prcode ?? order.OrderNo}.json";
        File.WriteAllText(fileName, encrypted);
    }

    // 删除订单 (完成后清理)
    public void DeleteOrder(string id)
    {
        var files = Directory.GetFiles(_storagePath, $"Order_{id}.json");
        foreach (var f in files) File.Delete(f);
    }

    // 获取所有本地订单
    public List<(string FileName, LocalOrder Order)> GetAllOrders()
    {
        var orders = new List<(string, LocalOrder)>();
        var files = Directory.GetFiles(_storagePath, "Order_*.json");

        foreach (var file in files)
        {
            try
            {
                var encrypted = File.ReadAllText(file);
                var json = _crypto.DecryptDynamic(encrypted);
                orders.Add((file, JsonConvert.DeserializeObject<LocalOrder>(json)));
            }
            catch { /* 解密失败, 跳过损坏文件 */ }
        }

        return orders;
    }
}

/// <summary>订单补发 — 启动后自动检查, 每30秒重试</summary>
public class OrderResender : IDisposable
{
    private readonly NetManager _net;
    private readonly LocalOrderStore _store;
    private readonly TicketPrinter _printer;
    private CancellationTokenSource _cts;

    public OrderResender(NetManager net, LocalOrderStore store, TicketPrinter printer)
    {
        _net = net;
        _store = store;
        _printer = printer;
    }

    // 启动自动补发
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = ResendLoop(_cts.Token);
    }

    private async Task ResendLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(30000, ct);  // 每30秒检查
            if (!_net.IsReady) continue;

            await ResendAllOrders();
        }
    }

    /// <summary>补发所有未完成的本地订单</summary>
    public async Task ResendAllOrders()
    {
        var orders = _store.GetAllOrders();

        foreach (var (fileName, order) in orders)
        {
            try
            {
                // 检查订单时间 (超过20分钟的不补打凭条)
                var orderTime = DateTime.ParseExact(order.Time, "yyyyMMddHHmmss", null);
                var canPrint = (DateTime.Now - orderTime).TotalMinutes <= 20;

                var response = await _net.SendAsync(order.Action, order);

                if (response.IsSuccess || response.Code == 42400)
                {
                    // 成功 → 删除本地文件
                    _store.DeleteOrder(order.Prcode ?? order.OrderNo);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Resend order failed: {ex.Message}");
            }
        }
    }

    public void Dispose() => _cts?.Cancel();
}
```

---

## 13. 步骤11: Unity 接入

```csharp
// 场景中挂载到 GameObject 即可
public class AppBootstrapper : MonoBehaviour
{
    public static AppBootstrapper Instance { get; private set; }

    public NetManager NetManager { get; private set; }
    public BillAcceptor BillAcceptor { get; private set; }
    public CashDispenser CashDispenser { get; private set; }
    public TicketPrinter TicketPrinter { get; private set; }
    public OrderResender OrderResender { get; private set; }

    async void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ① 初始化网络
        NetManager = new NetManager();
        await NetManager.StartAsync();

        if (!NetManager.IsReady)
        {
            // 未激活 → 显示激活界面
            // UIManager.OpenPanel("ActivatePanel");
            return;
        }

        // ② 初始化本地存储
        var orderStore = new LocalOrderStore(NetManager.Crypto);

        // ③ 初始化打印机
        TicketPrinter = new TicketPrinter(NetManager);

        // ④ 初始化业务模块
        BillAcceptor = new BillAcceptor(NetManager, orderStore, TicketPrinter);
        CashDispenser = new CashDispenser(NetManager, orderStore, TicketPrinter);

        // ⑤ 启动订单补发
        OrderResender = new OrderResender(NetManager, orderStore, TicketPrinter);
        OrderResender.Start();

        // ⑥ 显示主界面
        // UIManager.OpenPanel("MainPanel");

        EventCenter.Instance.Emit(Events.NET_READY);
    }

    void OnDestroy()
    {
        OrderResender?.Dispose();
    }
}

// ===== UI 层调用示例 =====
public class MainPanel : MonoBehaviour
{
    // 充值完打印 — 一行代码
    public async void OnRechargeComplete(int amount)
    {
        await AppBootstrapper.Instance.BillAcceptor.OnBillStacked();
    }

    // 扫码兑换
    public async void OnScanQrCode(string qrContent)
    {
        var result = await AppBootstrapper.Instance.CashDispenser.ScanQrCode(qrContent);
        if (result.Success)
        {
            // 显示确认界面...
        }
    }
}
```

---

## 14. API 接口速查表

### HTTP 接口

| 方法 | URL | 请求 (AES加密前) | 响应 (AES解密后) |
|------|-----|-----------------|-----------------|
| POST | `/login` | `{machineCode, lang}` | `{accessToken, mqttBroker, dynamicKey, bankKey}` |
| POST | `/activate` | `{machineCode, activationCode, lang}` | `{Code: 0/556/41801}` |

### MQTT Topic (常用)

| Topic | 功能 | 请求关键字段 | 响应关键字段 |
|-------|------|-------------|-------------|
| `order/HD_NewPrcode/1` | 生成充值码 | `{}` | `{prcode, rangeInfo}` |
| `order/HD_CheckPerCashIn/1` | 纸币验证 | `{amount, prcode, seq_num, platTag}` | `{Code}` |
| `order/HD_CMRO/1` | 确认充值 | `{amount, prcode, isLocal}` | `{orderNo, codeContent}` |
| `order/HD_FindWithdrawalOrder/1` | 扫码查询 | `{codeContent, lang}` | `{actionData:{codeType, totalMoney, orderNo}}` |
| `order/HD_ConfirmWithdrawalOrder/1` | 确认兑换 | `{amount, orderno, isLocal}` | `{outMoneyMethod[], rangeInfo}` |
| `order/HD_FinishWithdrawalOrder/1` | 完成兑换 | `{orderno, cdmDispense, cashBoxes}` | `{isPartialWithdrawal, remainingMoney}` |
| `admin/HD_GetCashBoxSettings/1` | 钱箱配置 | `{lang}` | `{remainingCount[], warningCount[]}` |
| `admin/HD_Login/1` | 后台登录 | `{password, role, lang}` | `{Code, Data}` |
| `admin/HD_Stats_Dashboard/1` | 营收总览 | `{startDate, endDate, pageNum}` | `{total_pages, records[]}` |
| `machine/HD_GetPlats/1` | 平台列表 | `{page_num, page_size}` | `{records[{plat_tag, plat_name}]}` |

### 响应错误码

| Code | 含义 | 处理 |
|------|------|------|
| 0 | 成功 | 正常处理 |
| 555 | 未激活 | 跳激活页 |
| 557 | 被禁用 | 跳激活页 |
| 11111 | 服务器忙 | 自动重试 |
| 42801 | 后台登录过期 | 弹窗提示 |
| 42400 | 部分出钞 | 打印剩余金额凭条 |
| 41801 | 激活码不存在 | 重新输入 |

---

> 基于 MoneyBox blizz 分支实战经验整理
