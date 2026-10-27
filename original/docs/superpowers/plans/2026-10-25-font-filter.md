# FontFilter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 构建适合2000—3000款本地字体的Windows筛选与试排工具。

**Architecture:** WPF负责虚拟化浏览与真实字体渲染；独立后台服务扫描字体并持久化SQLite索引。官网资料通过限流获取、确认匹配后入库，规则提取与人工标签分别保存。

**Tech Stack:** C#、WPF、SQLite；目标.NET版本依本机SDK检查确定。

**Spec:** ../specs/2026-10-25-font-filter-design.md

## Global Constraints
Windows独立程序；不调用AI接口；最多六款对比；后台增量扫描；人工标注优先；查询目标200毫秒，须实测。

## Review Focus
同名字体及多个字重不能错误合并。
损坏或不可读字体不得中止扫描。
缺字不得静默显示为另一字体。
网络失败不得破坏既有标签。
刷新资料不得覆盖人工修改。

### Task 1: 字体索引与持久化
**Files:** FontFilter.csproj, Models/FontRecord.cs, Services/FontScanner.cs, Services/FontStore.cs, Tests/IndexTests.cs
**Interfaces:** ScanAsync(CancellationToken)返回字体记录流；UpsertAsync(FontRecord)保存增量信息；SearchAsync(string,int,int)返回分页结果。
- [x] 检查SDK、SQLite依赖可用性和目录规则，建立独立项目。
- [x] 先写家族/字重区分、不可读字体跳过、人工标签保留的失败测试，执行确认失败。
- [x] 实现字体枚举、文件指纹、SQLite迁移和参数化查询。
- [x] 执行测试确认通过；扫描取消后可以恢复。

### Task 2: 本地标签与检索
**Files:** Services/TagRules.cs, Services/FontSearch.cs, Data/tag-rules.json, Tests/SearchTests.cs
**Interfaces:** Extract(string)返回带依据的候选标签；SearchAsync接受多个关键词，返回命中原因。
- [x] 先测试同义词、多关键词交集、未命中、人工覆盖，确认失败。
- [x] 实现分类词典、规则来源记录、批量编辑事务和检索去抖。
- [x] 使用3000条模拟记录验证正确性及查询耗时，实际字体性能另测。

### Task 3: 浏览、样文和对比
**Files:** App.xaml, MainWindow.xaml, MainWindow.xaml.cs, ViewModels/MainViewModel.cs, Services/PreviewService.cs, Services/SettingsStore.cs
**Interfaces:** PreviewService为指定字体提供可见区域预览及缺字检查；SettingsStore保存样文、字号和行距。
- [x] 实现回收式虚拟化列表、后台进度、暂停与取消、家族展开和标签编辑。
- [x] 实现三类内置样文、自定义样文持久化及六款对比上限。
- [x] 验证缺字提示、连续滚动缓存有界、扫描时UI响应和重启恢复。

### Task 4: 官方资料补充
**Files:** Services/OfficialMetadataService.cs, Views/MetadataReviewWindow.xaml, Tests/MetadataTests.cs
**Interfaces:** FetchAsync(Uri,CancellationToken)返回摘要及来源；仅用户确认后调用Store保存匹配。
- [x] 先测试错误域名拒绝、超时不改库、无有效介绍、人工标签不被覆盖，确认失败。
- [x] 实现方正/汉仪官方域名限制、低并发抓取、超时、来源时间、资料确认及本地规则提取。
- [x] 验证失败可重试，不能绕过登录或访问限制；不复制整站内容。

### Task 5: 交付验证
**Files:** README.md, build.ps1, Tests/PerformanceTests.cs
- [x] 编译Release并执行上述测试。
- [x] 用本机字体库检查首扫、增量扫描、检索、缺字、六款对比和内存变化。
- [x] 记录实测数据、未完成项目及运行步骤；提供可启动程序。

最终状态：21项测试通过，Release已发布到app目录。标准命令执行器故障通过独立开发进程解决；截图采集仍不可用，真实滚动视觉验收待用户本机检查。没有自动全站爬取，官网资料按官方地址提取并确认关联。
