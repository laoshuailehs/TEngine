# SBox基础框架

## 1. 代码规范

- 本基础框架统一使用**驼峰**命名
- 使用**见名只意**的命名方式
例如
```
private DelayTimer showTimer;
private void ShowTimer(float time)
{
    if (showTimer == null)
        showTimer = Timer.DelayAction(time, HideTips);
    else
        showTimer.Restart();
}
```

## 2. 基本思想

- 本框架架构模式是[MVC模式](https://www.runoob.com/design-pattern/mvc-pattern.html)

- 同时本框架还集成了[FSM有限状态机](https://blog.csdn.net/ChinarCSDN/article/details/82263126)

## 3. 使用方法

> 首先在Unity中创建一个**任意物体**作为项目入口, 并在该物体上挂载**入口脚本**, 在入口脚本中调用以下方法
```
    SBoxInit.Instance.Init("192.168.2.42", OnComplete);

```
> 其中192.168.2.42为匹配调试机器的IP, OnComplete为回调函数, 用于在连接成功后执行后续操作(获取SBox配置信息, 初始化网络模块, 跳转游戏场景)

> 本框架可在UnityEditor远程调试机器, 也可直接打包至机器上运行
```
#if UNITY_EDITOR
        MatchDebugManager.Instance.SendUdpMessage(SBoxEventHandle.SBOX_SADNBOX_RESET);
#else
        SBoxSandbox.Reset();
#endif
```
> 远程调试时要先在**SBoxSandbox**中找到要调试的方法, 在上述例子中要调试的是Reset, 则找到对应的返回Handle为**SBoxEventHandle.SBOX_SADNBOX_RESET**, 则把**SBoxEventHandle.SBOX_SADNBOX_RESET**发送给机器. 更详细的用法请参考**SBoxInit.cs**以及**MatchDebugManager.cs**

> 实际调用则主要参看**SBoxSandbox.cs**

## 4. 比较重要的几个脚本

- EventCenter.cs

> 事件中心, 用于在SBox中传递事件
```
    //事件中心模块 分发按下抬起事件
    if (Input.GetKeyDown(key))
        EventCenter.Instance.EventTrigger(EventHandle.KEY_DOWN, key);

    //事件中心模块, 接收按下抬起事件
    EventCenter.Instance.AddEventListener(EventHandle.KEY_DOWN, OnKeyDown);

    //事件中心模块, 移除事件
    EventCenter.Instance.RemoveEventListener(EventHandle.KEY_DOWN, OnKeyDown);
```


- SBoxSandboxListener.cs

> 硬件监听器, 用于监听硬件事件, 例如按键, 投币等
```
    //硬件监听器模块, 添加按键按下事件
    SBoxSandboxListener.Instance.AddButtonDown(SBOX_SWITCH.SWITCH_DOWN, () => { OnKeyDown(SBOX_SWITCH.SWITCH_DOWN); });

    //硬件监听器模块, 添加按键抬起事件
    SBoxSandboxListener.Instance.AddButtonUp(SBOX_SWITCH.SWITCH_DOWN, () => { OnKeyUp(SBOX_SWITCH.SWITCH_DOWN); });

    //硬件监听器模块, 添加按键短按事件
    SBoxSandboxListener.Instance.AddButtonClick(SBOX_SWITCH.SWITCH_DOWN, () => { OnKeyClick(SBOX_SWITCH.SWITCH_DOWN); });
```
> 其他如投币等更详细用法, 请参考代码(主要通过事件中心往外发出)

## 5. Base里面常用的脚本

- BaseManager.cs 管理器单例基类 MVC中的C层请继承此方法

- FSMSystem.cs 有限状态机管理器

- InputMgr.cs 输入管理器(键盘测试的时候可以使用)

- Timer.cs 定时器 

- MonoController.cs 代理Mono, 常用于在非继承MonoBehavior类中调起协程, 或者调用Update

- MonoSingleSingle.cs 继承了MonoBehavior的单例, 在初始化时会主动在场景中寻找继承了该类的Object, 在未找到的情况下会新建Object

- ResMgr.cs 资源加载管理器

- ScenesMgr.cs 场景管理器

- SoundManager.cs 音效播放管理器

## 1. 技术栈
游戏引擎: Unity  

编程语言: C#  

热更新: HybridCLR（支持IL2CPP热更）  

2D动画: Spine  

网络: WebSocket (UnityWebSocket)  

硬件交互: SBox SDK（街机设备控制）  

IoT集成: 投币器、出票器等硬件设备  

## 打码流程
登录
https://mc.cfkj88.com/
cfkj01
cfkj01@01
打开游戏设置，先设置游戏机台号
再去激活报码，在网站点击天数打码 与 游戏设置一一对应

