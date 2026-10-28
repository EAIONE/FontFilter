# 验证记录 · 2026-10-27

## 已执行

环境：Intel x86_64 iMac，macOS 15.7.9，.NET SDK 8.0.427，Avalonia 11.3.22。

- 核心回归：43/43 通过。覆盖保留的 Windows 核心行为，以及 WAL 安全导入、名称歧义保护、数据保留、API 密钥不落盘、单实例锁、扫描取消与异常保护、六款去重上限、未保存标签编辑及补充平面 Unicode 缺字。
- 桌面编译：0 警告、0 错误。
- 全新数据目录真实字体扫描：2,031 个系统家族条目，4,278 个可读取样式；第二次 4,278 个全部复用。
- 本机 GB18030 Bitmap 字体无法读取数据表，报告 1 个异常；按设计保留原索引，不对全库执行缺失判定。此字体没有被软件修改。
- UI 交互：空搜索结果、AND 查询、家族/字重切换、人工标签切换保存、收藏与筛选、172 标签库、4,949 条离线资料、HSB 主题窗口、密钥遮挡。
- 真实字形预览、缺字（含补充平面字符）及相同段落对比；试排与对比截图人工检查。
- 4,000 字符、120 号大字号长样文渲染；使用裁剪的原生绘制，不分配整篇巨大位图。
- 独立代码审阅发现两个问题，并分别先复现后修复：页签切换导致空预览，字号改变丢失样文草稿。回归检查通过。

## 交付验证

- Mac 自包含应用已独立启动，退出状态 0，并通过扫描、增量、真实预览与交互检查。
- 使用临时目录避开同步目录 FinderInfo 注入，签名验证以及 ZIP 解压后的严格签名验证通过。
- Windows x64 自包含发布成功；PE32+ x86-64 GUI 可执行文件、资料库、运行时和原生依赖齐全。
- ZIP 内容完整性检查通过。
- 真实旧版 7,648 行字体数据库导入：1,441 个带资料的样式唯一关联，2,827 项保留待关联，设置成功读取。未把 Windows 字体路径当作 Mac 本地字体。
- 追加关闭后的 WAL 库缺少附属文件回归：系统 SQLite 下复现失败，切换两端随包附带的 SQLitePCLRaw.lib.e_sqlite3 3.53.3 后通过；没有改用不一致的直接文件复制进行在线快照。

## 明确边界

没有 Windows 执行环境，不能声称 Windows 实机启动或渲染已经通过；只能验证目标构建和包内资源。没有真实 API 密钥，没有调用或验证付费服务。Mac 本地临时签名不是 Apple 开发者签名或公证。

最终复核：重新发布包含 SQLite 3.53.3 的两端包，ZIP CRC 检查通过。最终 Mac ZIP 解压后严格签名检查通过，自包含程序启动与全部交互检查退出 0，最终核心回归 43/43。Mac 版已复制到 ~/Applications/字里行间.app。

## 0.2.1 界面更新复核

48/48 核心回归通过；0 警告、0 错误。原生 UI 验证四个独立颜色（主色、辅助色、强调色、顶栏底色）的切换、取消、保存、旧设置兼容和浅色可读性；标签类别顺序、搜索留白、按钮右对齐；滚动条真实 24px 轨道/20px 拖动区域、彩色和圆角；单字体满宽对比行；路由滚轮事件调整字重、字号和行距。通过 CoreText 实测定位本机字体文件。Mac 最终自包含包解压/严格签名/启动与交互验证通过，用户数据备份后升级至 0.2.1；保留当前字号 22、行距 1.3。Windows 同步交叉构建，尚无 Windows 实机运行证据。

## 0.2.2 渐变色条与实时预览

H/S/B 改为圆角色条：彩虹色相、当前色相的饱和度梯度、黑到亮色的明度梯度。保留原生滑杆、鼠标拖动及键盘交互，移除旧单色轨道覆盖；显示即时角度/百分比及十六进制值。

新增验证先复现旧版“修改后主界面不实时变化”和“原轨道覆盖渐变条”，修复后通过。真实控件指针按下、拖动、释放事件可移动色相滑杆；主界面实时预览，临时预览不改 UserSettings，取消/关闭恢复原配色。保存功能由原有独立角色保存回归覆盖。独立审阅未发现重要问题。

48/48 核心回归通过，桌面编译 0 警告、0 错误。重新发布两端自包含包，Mac 最终 ZIP 解压、严格签名及完整扫描/交互/实时配色验证退出 0。升级前备份用户数据，保留现有四种颜色。Windows 仍为交叉构建验证，未实机运行。

## 0.2.3 点击色块直接编辑

移除角色下拉选择，主色/辅助色/强调色/顶栏底色改为四个满宽点击按钮；当前编辑项有勾选、边框和文字提示。回归通过按钮点击切换四个角色，验证草稿保留、即时预览、保存、取消和恢复默认配色。补充实际按钮宽度回归，保证可点击面积覆盖色块。48/48 核心测试通过；最终自包含包 --verify 退出 0，签名及安装验证通过。升级前保存用户当前配色并备份数据。Windows 包同步更新，未实机运行。

## 0.2.4 配色语义、资料收纳与边框统一

界面配色顺序改为主题色、辅助色、强调色、滚动条色、底色；为兼容旧数据，旧 HeaderColor 继续存储顶栏/主题色，旧 ThemeColor 仅用于滚动条，新增 BackgroundColor 控制整页底色。滚动条使用准确选色而非自动加黑，标签说明使用偏暗主题色，色块选中边框减弱。
字体资料支持右侧收起/展开并持久化状态，保留未保存编辑。左上角标志与中英文标题统一左边线和垂直居中。搜索、下拉、数字输入、样文展开、弹窗和复选框改用柔和主题描边及一致焦点色。
49/49 核心回归通过；实际 UI 检查五色角色、底色/滚动条准确映射、资料折叠与草稿保留、标志文字几何对齐、输入控件描边、深色底色可读性、保存/取消及实时预览。深色复选框回归先复现失败，修复后通过。0 警告、0 错误；最终 Mac 解压包严格签名与完整 --verify 退出 0。独立审阅未发现其他重要缺陷。用户数据备份后安装升级，Windows 包已同步构建，未在 Windows 实机运行。

## 0.2.5 · 2026-10-28
- Core 49/49；Desktop build 0 warnings/errors；Mac/Windows self-contained publish succeeded.
- Extracted final Mac package strict code-sign verification passed; isolated real-font UI verification exited 0 (4278 styles, incremental reuse 4278). Existing one unreadable bitmap font warning retained.
- Added real clipboard verification, comparison current-card visibility, collapsed details height, A5 page creation and live top-margin checks. Screenshot verified numeric values remain visible after widening fields.
- Review identified final-line descender clipping at tight leading; page capacity now reserves font descent.
- Windows runtime not exercised on this Mac.

## 0.2.6 · 2026-10-28
- Core 52/52: OR labels combine categories, retain favorite and active restrictions; default/custom paper dimension sanitization.
- Desktop build and Mac/Windows self-contained publish succeeded. Review found no confirmed significant regression.
- Real-font page UI verifies default185×260, preset210×285, margins, populated candidates and Viewbox fit at1440×920 and1160×740; screenshots show full page at both sizes.
- Resizing transferred focus to search in the existing theme test. Border assertion now accepts the specified normal/hover/focus theme brushes instead of incorrectly demanding normal state while focused.
- Final extracted signed Mac package full verification exited0, including theme/UI checks; installed0.2.6 after complete user-data backup. Windows PE x64 build verified; Windows runtime not exercised here.

## 0.2.7 · 2026-10-28
- Core53/53 and Desktop build0 warnings/errors. Book sample is3500–4000 characters with over20 indented paragraphs.
- Final extracted Mac full UI verification exits0: default page contains more than500 characters, automatic pagination produces multiple pages, preset and small-window fitting checks remain passing. Screenshot confirms full paragraphs through the bottom of the type area.
- Mac installed after full user data backup. Windows package cross-built; Windows runtime not exercised here.

## 0.2.8 · 2026-10-28
- Core53/53, Desktop build0 warnings/errors, both platform publishes succeeded; final extracted Mac full UI verification exits0.
- Real deferred TextChanged regression: after selecting黑体＋宋体 and waiting, result IDs exactly equal union of separate searches. Picker retains active selections.
- Numeric template editor checks both horizontal and vertical centering; screenshots inspect input and row label placement.
- Page candidate count equals comparison count; selecting comparison candidate selects its exact style; clearing comparison leaves zero candidates. Fixed one sheet, sufficient generated overflow, all paragraph starts normalize to two fixed em spaces.
- Page default185×260 and small-window fitting remain passing. User data backed up before installation. Windows runtime not exercised on this Mac.

## 0.2.9 · 2026-10-28
- Core58/58, Desktop0 warnings/errors; code review found no confirmed significant bugs.
- Native UI verifies left-candidates/middle-page/right-settings positions, details hidden with zero reserved width in page mode, and restored outside page mode. Native tab headers are hidden and title-right mode buttons drive the same content selection.
- Screenshots inspect default Chinese and English full-page samples and small-window fit. English/traditional selection updates the page; old sample keys migrate.
- Core tests closing/opening punctuation, whole words, combining graphemes, long-word non-splitting and script presets. Renderer explicitly honors prewrapped lines instead of wrapping them again.
- Final extracted signed Mac package full verification exits0. Mac0.2.9 installed after user-data backup; both platform packages built. Windows runtime not exercised here.

## 0.2.10 · 2026-10-28
Layout-only adjustment: both publishes succeeded without warnings/errors; extracted and installed Mac strict signature verification passed. Inspected installed UI screenshot: three larger modes immediately right of heading, left aligned; reveal action remains far right. No behavioral changes; full regression not repeated.
