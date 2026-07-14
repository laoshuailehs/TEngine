# PSS-ON-00054 项目拆解与图片 UI 修改指南

---

## 一、项目概述

**PSS-ON-00054** 是一个基于 **Cocos2d-html5** 引擎开发的 **HTML5 老虎机（Slot Machine）游戏**，游戏名称为 **FUNBET101**，连接后端服务器进行真钱赌博。

| 项目属性 | 详情 |
|----------|------|
| 游戏类型 | 视频老虎机（Video Slot） |
| 游戏引擎 | Cocos2d-html5 v3.x（纯 JavaScript 版） |
| 渲染方式 | Canvas / WebGL |
| 通讯协议 | WebSocket + Google Protocol Buffers |
| 分辨率 | 1280×720（支持横竖屏切换） |
| 框架率 | 50 FPS |
| 支持语言 | 11 种（eng, esp, ind, jp, kor, mys, ru, sch, tai, tch, vie） |
| 游戏版本 | 2.3.1 |
| 卷轴规格 | 3 列 × 3 行（可扩展至 3x4, 3x5, 4x4, 4x5） |
| 后端地址 | ws://8.138.202.232:81/slot |

---

## 二、项目结构详解

```
g:\pss-on-00054\
├── index.html                    # 入口 HTML 页面
├── main.js                       # 游戏启动入口（资源加载 → 进入场景）
├── project.json                  # Cocos 引擎配置（模块列表、JS 文件加载顺序）
├── .cocos-project.json          # Cocos 项目元数据（纯 JS 项目）
├── CMakeLists.txt               # 原生构建配置（SpiderMonkey js-bindings，非 Web 使用）
│
├── frameworks/                   # Cocos2d-html5 游戏引擎
│   └── cocos2d-html5/           # 引擎核心（CCBoot.js、渲染、动画、UI 组件等）
│
├── lib/                          # 第三方 JS 库
│   ├── ProtoBuf.js              # Google Protocol Buffers 编解码
│   ├── ByteBufferAB.min.js      # 二进制缓冲区
│   ├── Long.min.js              # 64位整数支持
│   ├── jquery-2.1.4.min.js      # jQuery（DOM 操作）
│   ├── game.proto               # 游戏通信协议定义
│   └── lobby.proto              # 大厅通信协议定义
│
├── src/                          # ★ 游戏核心源码
│   ├── app.js                   # 默认 HelloWorld 层（未使用）
│   ├── resource.js              # ★ 主资源清单（图片、音频、动画）
│   ├── g_variable.js            # ★ 全局变量、状态机、事件系统、卷轴配置
│   ├── g_ctrlor_state.js        # ★ 游戏状态控制器（FSM 状态机）
│   ├── g_ctrlor_reel.js         # ★ 卷轴控制器（物理运动）
│   ├── g_mathdata.js            # 数学数据（中奖线、RNG、赔付表）
│   ├── g_fakemath.js            # 赔付表定义
│   ├── g_recover_data.js        # 断线重连数据
│   ├── lkmessage.js             # ★ WebSocket 网络层（消息收发）
│   ├── scene_base.js            # ★ 主游戏场景（BaseScene）
│   ├── scene_feature.js         # 特色游戏场景（Feature Scene）
│   ├── picnumber.js             # 数字精灵渲染
│   ├── pay_table_config.js      # 帮助页赔付表配置
│   ├── temp_anm.js              # 临时动画定义
│   │
│   ├── bs/                      # 基础场景各层实现
│   │   ├── bs_ly_theme.js       # ★ 背景主题层（背景图、卷轴框、大动画）
│   │   ├── bs_ly_reels.js       # ★ 卷轴层（符号渲染与滚动）
│   │   ├── bs_ly_win_line.js    # 中奖线高亮层
│   │   ├── bs_ly_creditwin.js   # 赢分显示层
│   │   └── bs_ly_animation_0.js # 动画特效层
│   │
│   └── lib/                     # ★ UI 组件库
│       ├── bs_ly_ui_2.js        # ★ 主 UI 层（按钮、菜单、积分、下注等）
│       ├── bs_ly_ui_2_1.js      # 副 UI 层
│       ├── ui_resource2.0.js    # ★ UI 2.0 资源映射
│       ├── ui_ctrlor_state.js   # UI 状态控制器
│       ├── ps_text.js           # 多语言文本资源
│       ├── ps_music_ctrlor.js   # 音频控制器
│       ├── ps_number_format.js  # 数字格式化
│       ├── big_win_anm.js       # Big Win / Mega Win / Super Win 动画
│       ├── tile_message.js      # 弹窗消息层
│       ├── slow_motion.js       # 慢动作特效
│       ├── newloading.js        # 加载画面
│       ├── jp/                  # Jackpot UI 组件
│       ├── new_draw/            # 新版中奖线绘制系统
│       ├── medal/               # 勋章系统
│       ├── ui3.0/               # UI 3.0 组件
│       └── ...                  # 其他组件
│
├── res/                          # ★★★ 所有游戏资源文件 ★★★
│   ├── loading.js               # 预加载脚本
│   ├── common/                  # 公共资源（所有语言共享）
│   │   ├── pic/                 # 静态图片（符号、背景、卷轴框、图标）
│   │   ├── anm/                 # 动画资源（精灵帧序列、图集）
│   │   │   ├── symbol/          # 符号动画（symbol_00 ~ symbol_05）
│   │   │   └── bkg.*           # 背景动画
│   │   ├── num/                 # 数字精灵图
│   │   ├── sound/               # 音效文件（37 个 MP3）
│   │   └── ui2.0/              # ★ UI 2.0 图片
│   │       ├── base/            # 基础背景、系统背景、货币图标
│   │       ├── down_ui/         # 底部栏（自动、下注、音量、帮助、积分）
│   │       ├── spin/            # 旋转按钮（开始、停止、赢分、加速）
│   │       ├── info/            # 信息面板（下注菜单、按钮）
│   │       ├── bigwin/anm/      # Big Win / Mega Win / Super Win 动画
│   │       ├── jp/              # Jackpot 显示（数字、背景、Logo）
│   │       ├── slowmotion/      # 慢动作遮罩
│   │       ├── loadingInGame/   # 游戏内加载动画
│   │       └── home/            # 首页/大厅横幅
│   │
│   └── {lang}/                  # 语言特定资源（11 种语言各一份）
│       ├── anm/logo/            # Logo 动画
│       ├── pic/                 # 语言特定图片（帮助页面等）
│       ├── jp/                  # Jackpot 级别名称图片
│       └── ui2.0/               # 语言特定 UI 文字图片
│
└── publish/                      # 发布输出目录
    └── html5/                   # HTML5 发布版
```

---

## 三、核心架构

### 3.1 场景层级结构（从上到下）

```
BaseScene（主场景）
  ├── G_TILE       - 弹窗消息层（最顶层）
  ├── G_JACKPOT    - 全局 Jackpot 数字层
  ├── BS_ANM_0     - 动画特效层
  ├── BS_CREDIT_WIN - 赢分显示层
  ├── BS_UI_2      - 副 UI 层
  ├── BS_UI        - ★ 主 UI 层（所有按钮、菜单、积分显示）
  ├── BS_REEL      - ★ 卷轴层（3×3 符号矩阵）
  ├── BS_WIN_LINE  - 中奖线层
  └── BS_THEME     - ★ 背景主题层（背景图、卷轴框、Logo）
```

### 3.2 游戏流程

```
加载 index.html
  → 加载第三方库（ProtoBuf, jQuery）
  → 加载 Cocos2d 引擎（CCBoot.js）
  → 执行 main.js
    → 预加载 g_resources 中所有资源
    → prepareRes() 注册精灵帧和动画缓存
    → 创建 BaseScene（所有图层）
    → 建立 WebSocket 连接
    → Login → Config → Strips → 游戏就绪
    → 玩家点击 Spin → 发送 ResultCall → 接收 ResultRecall → 播放动画
```

### 3.3 资源加载流程

```
resource.js 中定义:
  res = { }          ← 核心资源（符号、背景、音效）
  animation = { }    ← 帧动画定义
  ui_res = { }       ← UI 2.0 资源（从 ui_resource2.0.js）
  new_draw_res = { } ← 新版中奖线资源
  new_draw_line_res = { } ← 中奖线资源

  → 合并为 g_resources[] 数组
  → cc.LoaderScene.preload(g_resources) 预加载
  → prepareRes():
    ├── .plist 文件 → cc.spriteFrameCache 注册精灵帧
    └── 动画定义 → ps.AnimationCache 注册动画
```

---

## 四、图片 UI 修改方法（核心指南）

### 4.1 理解图片资源的组织结构

项目中所有图片分布在 `res/` 目录下，分为两大类：

| 类型 | 路径 | 说明 |
|------|------|------|
| **公共图片** | `res/common/` | 所有语言共享的图片 |
| **语言图片** | `res/{lang}/` | 每种语言独立的图片（含文字） |

**语言代码对照：**
| 代码 | 语言 | 代码 | 语言 |
|------|------|------|------|
| eng | 英语 | mys | 马来语 |
| esp | 西班牙语 | ru | 俄语 |
| ind | 印尼语 | sch | 简体中文 |
| jp | 日语 | tai | 泰语 |
| kor | 韩语 | tch | 繁体中文 |
| vie | 越南语 | | |

### 4.2 修改图片 UI 的完整流程

#### 步骤 1：找到目标图片文件

根据你要修改的 UI 元素，在 `res/` 目录下找到对应的图片文件：

| UI 元素 | 图片路径 | 说明 |
|---------|----------|------|
| 游戏背景 | `res/common/pic/bs_bg.png` | 桌面版背景 |
| 手机背景 | `res/common/pic/bs_bg_m.png` | 移动版背景 |
| 卷轴框 | `res/common/pic/frame.png` / `frame2.png` | 卷轴边框 |
| 卷轴底色 | `res/common/pic/reel_bg.png` | 卷轴背景 |
| 符号 0-9 | `res/common/pic/symbol_00.png` ~ `symbol_09.png` | 卷轴符号图 |
| UI 底栏背景 | `res/common/ui2.0/down_ui/bg_d.png` | 底部 UI 栏 |
| Spin 按钮 | `res/common/ui2.0/spin/start_up.png` / `start_down.png` | 开始按钮（普通/按下） |
| Stop 按钮 | `res/common/ui2.0/spin/stop_up.png` / `stop_down.png` | 停止按钮 |
| Win 按钮 | `res/common/ui2.0/spin/win_up.png` / `win_down.png` | 赢分状态按钮 |
| 自动按钮 | `res/common/ui2.0/down_ui/auto_on.png` / `auto_off.png` | 自动模式开关 |
| 音量按钮 | `res/common/ui2.0/down_ui/voice_on.png` / `voice_off.png` | 音量开关 |
| 帮助按钮 | `res/common/ui2.0/down_ui/help_on.png` / `help_off.png` | 帮助开关 |
| 下注加减 | `res/common/ui2.0/down_ui/add_up.png` / `minus_up.png` | 下注加减按钮 |
| 游戏标题 | `res/{lang}/ui2.0/base/game_bg.png` | 含语言的游戏标题 |
| 帮助页 | `res/{lang}/pic/help/help_01.png` ~ `help_04.png` | 帮助页面 |
| Jackpot Logo | `res/{lang}/ui2.0/jp/jp_logo.png` | Jackpot 标志 |
| Jackpot 级别 | `res/{lang}/jp/mini.png`, `minor.png`, `major.png`, `grand.png` | JP 级别名称 |
| 积分标签 | `res/{lang}/ui2.0/down_ui/credit.png` | CREDIT 标签 |
| 赢分标签 | `res/{lang}/ui2.0/down_ui/win.png` | WIN 标签 |
| 下注菜单 | `res/{lang}/ui2.0/info/bet_1.png` ~ `bet_4.png` | 下注菜单标题 |
| 货币图标 | `res/common/ui2.0/base/currency/*.png` | 70+ 加密货币图标 |

#### 步骤 2：替换图片文件

**方法 A：直接替换文件（推荐，最简单）**

1. 准备你的新图片（保持**相同尺寸**和**相同格式** PNG）
2. 将新图片命名为与原文件**完全相同的文件名**
3. 覆盖 `res/` 目录下的对应文件
4. 刷新浏览器即可看到效果

> ⚠️ **注意**：如果修改的是语言相关的图片（路径中包含 `{lang}`），需要替换**每个语言目录**下的对应文件，或者只修改你关心的语言。

**方法 B：修改资源路径引用**

如果你想把图片放到不同位置，需要修改对应的资源映射文件。

#### 步骤 3：修改资源映射（如需要）

资源路径在两个地方定义：

**① [src/resource.js](src/resource.js) — 核心资源映射**

```javascript
var res = {
    // 符号图片
    sym_0_png: "res/common/pic/symbol_00.png",  // ← 修改这里的路径
    sym_1_png: "res/common/pic/symbol_01.png",
    // ...
    
    // 背景图片
    bs_bg_w: "res/common/pic/bs_bg.png",
    bs_bg_m: "res/common/pic/bs_bg_m.png",
    
    // 卷轴框
    reel_frame_1: "res/common/pic/frame.png",
    reel_frame_2: "res/common/pic/frame2.png",
    
    // 音效
    sound_btnclick: "res/common/sound/common/BtnClick.mp3",
    // ...
};
```

**② [src/lib/ui_resource2.0.js](src/lib/ui_resource2.0.js) — UI 2.0 资源映射**

```javascript
var ui_res = {
    // 底部 UI 栏
    ui_widget_bg_png: "res/common/ui2.0/down_ui/bg_d.png",
    ui_widget_bg_m_png: "res/common/ui2.0/down_ui/bg_m.png",
    
    // Spin 按钮
    ui_spin_start_up_png: "res/common/ui2.0/spin/start_up.png",
    ui_spin_start_down_png: "res/common/ui2.0/spin/start_down.png",
    ui_spin_stop_up_png: "res/common/ui2.0/spin/stop_up.png",
    ui_spin_stop_down_png: "res/common/ui2.0/spin/stop_down.png",
    
    // 语言相关 - 路径中使用 RES_LANGUAGE 变量
    ui_auto_png: "res/" + RES_LANGUAGE + "/ui2.0/down_ui/auto.png",
    ui_credit_png: "res/" + RES_LANGUAGE + "/ui2.0/down_ui/credit.png",
    ui_win_png: "res/" + RES_LANGUAGE + "/ui2.0/down_ui/win.png",
    // ...
};
```

> 💡 **关键理解**：`RES_LANGUAGE` 变量从 URL 参数 `?lang=` 获取。公共图片路径是写死的，语言相关图片路径是动态拼接的。

#### 步骤 4：修改代码中使用图片的位置（如需要）

图片在代码中的使用方式主要有三种：

**方式 1：创建 Sprite（静态图片）**

```javascript
// 在 bs_ly_theme.js 中
this.bs_bg = new cc.Sprite(res.bs_bg_w);  // 使用 res 对象中的路径
this.bs_bg.setAnchorPoint(0, 0);
this.bs_bg.setPosition(0, 0);
this.addChild(this.bs_bg, 1);
```

**方式 2：创建 Button（按钮 - 有按下/弹起状态）**

```javascript
// 在 bs_ly_ui_2.js 中
this._spin_btn = new ccui.Button(
    ui_res.ui_spin_start_up_png,    // 弹起状态图片
    ui_res.ui_spin_start_down_png,  // 按下状态图片
    ui_res.ui_spin_start_down_png   // 禁用状态图片
);
this._spin_btn.setAnchorPoint(0.5, 0.5);
this._spin_btn.setPosition(1195, 351);
this.addChild(this._spin_btn);
```

**方式 3：创建 MenuItemToggle（切换按钮 - 如自动/音量开关）**

```javascript
// 自动播放切换按钮
var auto_on = new cc.MenuItemImage(ui_res.ui_auto_up_png, ui_res.ui_auto_up_png);
var auto_off = new cc.MenuItemImage(ui_res.ui_auto_down_png, ui_res.ui_auto_down_png);
this._autoplay_btn = new cc.MenuItemToggle(auto_on, auto_off, this.click_autoplay, this);
```

**方式 4：从 SpriteFrame 缓存创建（动画 / plist 图集）**

```javascript
var SpriteInPlist = cc.Sprite.extend({
    ctor: function(rcname) {
        var pFrame = cc.spriteFrameCache.getSpriteFrame(rcname);
        this._super(pFrame);
    }
});
```

### 4.3 具体修改案例

#### 案例 1：更换游戏背景图

```
1. 准备新背景图（建议 1280×720 或等比例）
2. 替换文件：
   - res/common/pic/bs_bg.png        （桌面版）
   - res/common/pic/bs_bg_m.png      （手机版）
3. 如果文件名不同，修改 src/resource.js：
   bs_bg_w: "res/common/pic/你的新背景.png",
   bs_bg_m: "res/common/pic/你的新背景_m.png",
4. 刷新浏览器查看效果
```

#### 案例 2：更换 Spin 按钮

```
1. 准备 3 张图片：
   - 弹起状态（正常显示）
   - 按下状态（点击时显示）  
   - 禁用状态（不可点击时显示）
2. 替换文件：
   - res/common/ui2.0/spin/start_up.png    ← 弹起
   - res/common/ui2.0/spin/start_down.png  ← 按下/禁用
3. 如果尺寸变化，修改 bs_ly_ui_2.js 中的位置：
   this._spin_btn.setPosition(新X, 新Y);
4. 刷新查看效果
```

#### 案例 3：更换卷轴符号

```
1. 准备 10 张符号图（symbol_00 到 symbol_09）
2. 替换文件：
   - res/common/pic/symbol_00.png ~ symbol_09.png
3. 如果需要修改符号大小，修改 src/g_variable.js 中的 BK.REEL_CONFIG
4. 刷新查看效果
```

#### 案例 4：修改多语言图片

```
1. 找到要修改的语言目录：
   例如简体中文：res/sch/
   例如繁体中文：res/tch/
2. 修改该目录下对应的图片：
   - res/sch/ui2.0/down_ui/credit.png   （CREDIT 文字）
   - res/sch/ui2.0/down_ui/win.png      （WIN 文字）
   - res/sch/pic/help/help_01.png       （帮助页 1）
3. 对所有 11 种语言重复（或只改需要的语言）
4. 通过 ?lang=sch 参数测试
```

### 4.4 修改图片 UI 的注意事项

| 注意点 | 说明 |
|--------|------|
| **图片格式** | 必须使用 PNG 格式（支持透明通道） |
| **图片尺寸** | 保持与原图相同尺寸，或同步修改代码中的位置/锚点 |
| **文件命名** | 必须与资源映射中的名称完全一致（区分大小写） |
| **plist 图集** | 如果修改 plist 图集中的图片，需要用 TexturePacker 重新打包 |
| **多语言** | 含文字的图片需要修改每种语言版本 |
| **浏览器缓存** | 修改后需要**强制刷新**（Ctrl+Shift+R）或清除浏览器缓存 |
| **预加载** | 新图片需要在 `g_resources` 数组中，否则不会被预加载 |
| **坐标系统** | Cocos2d 坐标系原点在**左下角**，锚点 (0.5, 0.5) 表示中心 |

### 4.5 资源类型速查

```
文件扩展名    用途                    加载方式
─────────────────────────────────────────────────
.png          静态精灵图              new cc.Sprite(path)
.plist+.png   SpriteFrame 图集        cc.spriteFrameCache.addSpriteFrames(plist)
.atlas+.json+.png  Spine 骨骼动画    new sp.SkeletonAnimation(json, atlas)
.json         动画描述文件            用于帧动画配置
.mp3          音频文件                cc.audioEngine.playEffect(path)
```

---

## 五、关键文件速查表

| 文件 | 作用 | 修改场景 |
|------|------|----------|
| [src/resource.js](src/resource.js) | 核心资源路径定义 | 修改符号、背景、音效路径 |
| [src/lib/ui_resource2.0.js](src/lib/ui_resource2.0.js) | UI 资源路径定义 | 修改按钮、菜单、Jackpot 图片路径 |
| [src/scene_base.js](src/scene_base.js) | 主场景图层结构 | 调整图层顺序、添加新图层 |
| [src/g_variable.js](src/g_variable.js) | 全局配置、状态机 | 修改卷轴规格、分辨率、状态逻辑 |
| [src/lib/bs_ly_ui_2.js](src/lib/bs_ly_ui_2.js) | 主 UI 层实现 | 修改按钮位置、行为、布局 |
| [src/bs/bs_ly_theme.js](src/bs/bs_ly_theme.js) | 背景主题层 | 修改背景图、卷轴框、Logo |
| [src/bs/bs_ly_reels.js](src/bs/bs_ly_reels.js) | 卷轴渲染层 | 修改符号显示方式 |
| [src/lkmessage.js](src/lkmessage.js) | WebSocket 通信 | 修改后端地址、协议处理 |
| [project.json](project.json) | Cocos 项目配置 | 添加新 JS 文件、修改帧率 |
| [index.html](index.html) | 入口页面 | 修改页面标题、加载顺序 |

---

## 六、本地运行与调试

### 启动方式

在 VS Code 中按 `F5` 启动调试（使用 Live Server，端口 5505），或直接在终端运行：

```bash
# 使用任意 HTTP 服务器在项目根目录启动
npx live-server --port=5505
```

浏览器访问：`http://localhost:5505/?lang=sch&token=xxx`

### URL 参数说明

| 参数 | 说明 | 示例 |
|------|------|------|
| `lang` | 语言选择 | `sch`（简体中文）, `tch`（繁体中文）, `eng`（英语） |
| `token` | 用户认证令牌 | 由后端生成 |
| `sm` | 显示模式 | 控制积分单位和小数点格式 |

---

## 七、总结

这个项目采用了 Cocos2d-html5 引擎的标准 MVC 变体架构：
- **Model**：`g_variable.js`（状态定义）、`g_mathdata.js`（数学数据）
- **View**：`res/` 目录（资源文件） + 各 Layer 文件（渲染逻辑）
- **Controller**：`g_ctrlor_state.js`（状态机）、`lkmessage.js`（网络层）

修改图片 UI 的核心思路是：
1. **找文件** → 在 `res/` 目录定位目标图片
2. **找引用** → 在 `resource.js` 或 `ui_resource2.0.js` 确认路径映射
3. **找使用** → 在对应的 Layer 文件确认代码中的使用方式
4. **替换** → 替换图片文件或修改路径引用
5. **测试** → 强制刷新浏览器验证效果
