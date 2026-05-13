# AvalonLog 移植说明：F# → C#

## 概述

本文档记录将 **AvalonLog** 从原始 **F#** 版本（`reference/AvalonLog`）移植到 **C# / .NET 10** 版本的全部变更。

原始版本地址：[goswinr/AvalonLog](https://github.com/goswinr/AvalonLog)

---

## 目录

1. [项目级变更](#1-项目级变更)
2. [组件映射](#2-组件映射)
3. [API 变更](#3-api-变更)
4. [已发现并修复的问题](#4-已发现并修复的问题)
5. [C# 版本独有的改进](#5-c-版本独有的改进)
6. [缺失功能说明](#6-缺失功能说明)
7. [待处理事项](#7-待处理事项)

---

## 1. 项目级变更

### 1.1 语言与框架

| 项目 | 原始 (F#) | 当前 (C#) |
|---|---|---|
| 语言 | F# | C# (LangVersion: `preview`) |
| 目标框架 | `net472;net7.0-windows`（多目标） | `net10.0-windows`（单目标） |
| Nullable | 不适用 | `enable` |
| 解决方案 | `AvalonLog.sln` | `AvalonLog.slnx` |

### 1.2 NuGet 包依赖

| 依赖项 | 原始 | 当前 | 说明 |
|---|---|---|---|
| `AvalonEditB` | `2.4.0` | `2.4.0` | 一致 |
| `FSharp.Core` | `6.0.7`（显式引用） | 无 | 不需要 |
| `Microsoft.Extensions.Logging` | 无（已在 v0.20.0 中移除） | 无 | 一致 |
| `Ionide.KeepAChangelog.Tasks` | `0.3.3` | `0.3.3` | 一致 |
| `Microsoft.SourceLink.GitHub` | `10.0.201` | `10.0.201` | 一致 |

### 1.3 包元数据

| 属性 | 原始 | 当前 |
|---|---|---|
| `PackageTags` | `wpf;console;fsharp;avalonedit` | `wpf;console;csharp;avalonedit` |
| `Description` | 含 "Including F# printf formatting" | 不含 |
| `FsDocs*` 属性 | 若干 | 无 |

### 1.4 dotnet-tools.json

- **原始**：含 `fsdocs-tool: 21.0.0`，用于 FSharp.Formatting 文档生成
- **当前**：空 —— C# 项目不使用 FSharp.Formatting

---

## 2. 组件映射

### 2.1 源文件映射

| 原始 (`*.fs`) | 当前 (`*.cs`) | 类型 | 功能匹配 |
|---|---|---|---|
| `Brush.fs` | `Brush.cs` | `static class BrushHelper` / `PenHelper` | ✅ 完全等价 |
| `Util.fs` | `Util.cs` | `static class Util` | ✅ 功能等价 |
| `Sync.fs` | `Sync.cs` | `static class SyncAvalonLog` | ✅ 功能等价 |
| `TextColor.fs` | `TextColor.cs` | `struct NewColor` / `struct RangeColor` / `class ColorizingTransformer` | ✅ 完全等价 |
| `SelectedTextHighlighter.fs` | `SelectedTextHighlighter.cs` | `class SelectedTextHighlighter` | ✅ 功能等价 |
| `LogTextWriter.fs` | `LogTextWriter.cs` | `class LogTextWriter` | ✅ 功能等价 |
| `AvalonLog.fs` | `AvalonLog.cs` | `class AvalonLog` | ✅ 核心功能等价（API 有重设计） |

### 2.2 内部类型名称映射

| 原始 (F#) | 当前 (C#) | 说明 |
|---|---|---|
| `module Brush` | `static class BrushHelper` | 画笔工具方法 |
| `module Pen` | `static class PenHelper` | Pen 工具方法 |
| `module Util` | `static class Util` | 内部辅助方法 |
| `type SyncAvalonLog` | `static class SyncAvalonLog` | 同步上下文管理 |
| `type NewColor` (struct) | `struct NewColor` | 颜色偏移记录 |
| `type RangeColor` (struct) | `struct RangeColor` | 行内颜色区间 |
| `type ColorizingTransformer` | `class ColorizingTransformer` | 颜色渲染转换器 |
| `type SelectedTextHighlighter` | `class SelectedTextHighlighter` | 选中文本高亮器 |
| `type LogTextWriter` | `class LogTextWriter` | 日志 TextWriter |

---

## 3. API 变更

### 3.1 `Append` / `AppendLine` 系列

F# 采用了命名导向的 API（`AppendWithColor` / `AppendWithBrush` / `AppendWithLastColor`），C# 改用重载。

#### 无颜色（使用默认前景色）

| 操作 | 原始 (F#) | 当前 (C#) |
|---|---|---|
| 追加，无换行 | `log.Append(s)` | `log.Append(s)` |
| 追加，有换行 | `log.AppendLine(s)` | `log.AppendLine(s)` |

**语义**：使用 `_defaultBrush`（编辑器默认前景色），不会更新 `_customBrush`。

#### 指定颜色（RGB）

| 操作 | 原始 (F#) | 当前 (C#) |
|---|---|---|
| 追加，无换行 | `log.AppendWithColor(r, g, b, s)` | `log.Append(s, r, g, b)` |
| 追加，有换行 | `log.AppendLineWithColor(r, g, b, s)` | `log.AppendLine(s, r, g, b)` |

#### 指定画笔

| 操作 | 原始 (F#) | 当前 (C#) |
|---|---|---|
| 追加，无换行 | `log.AppendWithBrush(br, s)` | `log.Append(s, brush)` |
| 追加，有换行 | `log.AppendLineWithBrush(br, s)` | `log.AppendLine(s, brush)` |

#### 使用上一次颜色

| 操作 | 原始 (F#) | 当前 (C#) |
|---|---|---|
| 追加，无换行 | `log.AppendWithLastColor(s)` | `log.AppendWithLastColor(s)` |
| 追加，有换行 | `log.AppendLineWithLastColor(s)` | `log.AppendLineWithLastColor(s)` |

**语义**：使用 `_customBrush`（用户最后一次通过颜色参数或 `Append*WithBrush/Color` 主动指定的画笔）。

#### C# 新增

| 操作 | 说明 |
|---|---|
| `log.AppendLine()` | 输出一个空白行 |
| `log.PrintLine(s)` | 使用 `_customBrush` 打印并换行 |
| `log.PrintLine(s, brush)` | 使用指定画笔打印并换行 |
| `log.PrintLine(s, r, g, b)` | 使用 RGB 颜色打印并换行 |

#### API 设计理念

C# 的 API 重设计理由：

1. **参数顺序** `(string, color)` 符合 C# 重载惯例 —— 核心操作对象放第一位，修饰参数靠后
2. **重载比多方法名** 更符合 C# 开发者心智模型 —— `Append(s)` → `Append(s, brush)` 递进直观
3. **`AppendLine()`** 补全了 F# 版本缺失的简单空行输出
4. **`PrintLine`** 作为便捷入口，填补了 F# `printf*` 格式化方法缺失的空白

### 3.2 其他属性与方法

以下 API 在两个版本中完全一致：

- `IsAlive` — 开关日志
- `VerticalScrollBarVisibility` / `HorizontalScrollBarVisibility`
- `FontFamily` / `FontSize`
- `ShowLineNumbers` / `EnableHyperlinks`
- `WordWrap`
- `MaximumCharacterAllowance`
- `LastPrintDelay` / `PrintInterval`
- `GetText()` / `GetText(ISegment)`
- `Selection` / `SearchPanel` / `SelectedTextHighLighter`
- `AvalonEdit` (被标记为 `[Obsolete]`)
- `Clear()`
- `GetTextWriter(int red, int green, int blue)`
- `GetTextWriter(SolidColorBrush br)`
- `GetConditionalTextWriter(Func<string, bool>, SolidColorBrush)`
- `GetConditionalTextWriter(Func<string, bool>, int, int, int)`

### 3.3 `SelectedTextHighlighter` 事件签名

| 属性 | 原始 (F#) | 当前 (C#) |
|---|---|---|
| `OnHighlightCleared` | `Event<unit>` | `event Action?` |
| `OnHighlightChanged` | `Event<string * ResizeArray<int>>`（元组） | `event Action<string, List<int>>?`（分离参数） |

功能等价，C# 版本使用独立参数代替 F# 元组。

---

## 4. 已发现并修复的问题

以下问题在对比分析过程中被发现并修复：

### 4.1 `Append(s)` / `AppendLine(s)` 错误使用 `_customBrush`

- **问题**：C# 版本最初使用了 `_customBrush`（记忆色），而 F# 原始版本使用 `defaultBrush`（编辑器默认前景色）
- **影响**：在 F# 中每次无颜色 `Append` 都会恢复到默认前景色；在 C# 旧代码中会延续之前设置的任何颜色
- **修复**：改为 `_defaultBrush`

```csharp
// 修复后
public void Append(string s)  => PrintOrBuffer(s, false, _defaultBrush);
public void AppendLine(string s) => PrintOrBuffer(s, true, _defaultBrush);
```

### 4.2 `AppendWithLastColor` / `AppendLineWithLastColor` 使用了错误的颜色源

- **问题**：C# 版本最初使用了 `_prevMsgBrush ?? _defaultBrush`，而 F# 原始版本使用 `customBrush`
- **影响**：`_prevMsgBrush` 是上一条消息被分配的实际画笔，`_customBrush` 是用户最后一次主动设置的画笔。在边缘场景（如先调用 `Append(s)` 再调用 `AppendWithLastColor(s)`）下两者不同
- **修复**：改为 `_customBrush`

```csharp
// 修复后
public void AppendWithLastColor(string s)     => PrintOrBuffer(s, false, _customBrush);
public void AppendLineWithLastColor(string s) => PrintOrBuffer(s, true, _customBrush);
```

### 4.3 Timer 未释放导致资源泄漏

- **问题**：`PrintOrBuffer` 中创建的 `System.Threading.Timer` 对象在回调中未执行 `Dispose()`
- **影响**：高频打印场景下 Timer 对象累积，造成内存压力
- **修复**：回调中显式调用 `timer?.Dispose()`

```csharp
// 修复后：常规分支
Timer? timer = null;
timer = new Timer(_ =>
{
    if (Interlocked.Read(ref _printCallsCounter) == k && _isAlive)
        _log.Dispatcher.Invoke(PrintToLog);
    timer?.Dispose();  // ← 自释放
}, null, _lastPrintDelay, Timeout.Infinite);
```

### 4.4 `dontPrintJustBuffer` 分支中 `Thread.Sleep` 阻塞线程池线程

- **问题**：C# 版本最初在 Timer 回调中使用 `while (...) { Thread.Sleep(50); }` 阻塞线程池线程，而 F# 使用非阻塞的 `Async.Sleep`
- **影响**：在 `Clear()` 调用期间阻塞一个线程池线程
- **修复**：用 Timer 自重新调度替代 while 循环

```csharp
// 修复后：自重新调度的 Timer（非阻塞）
Timer? timer = null;
timer = new Timer(_ =>
{
    if (!_dontPrintJustBuffer || !_isAlive)
    {
        if (Interlocked.Read(ref _printCallsCounter) == k && _isAlive)
            _log.Dispatcher.Invoke(PrintToLog);
        timer?.Dispose();
    }
    else
    {
        timer?.Change(50, Timeout.Infinite);  // ← 重新调度，不阻塞
    }
}, null, 50, Timeout.Infinite);
```

---

## 5. C# 版本独有的改进

### 5.1 `Clear()` 中 ColorizingTransformer 默认画笔同步

F# 原始版本在 `Clear()` 中仅更新模块级别的 `defaultBrush` 变量，未同步到 `ColorizingTransformer` 内部引用。

```fsharp
// F# 原始：仅更新模块变量
defaultBrush <- (log.Foreground.Clone() :?> SolidColorBrush |> Brush.freeze)
```

C# 版本额外调用 `SetDefaultBrush` 保证内部状态一致：

```csharp
_defaultBrush = BrushHelper.FreezeIt((SolidColorBrush)_log.Foreground.Clone());
_color.SetDefaultBrush(_defaultBrush);  // ← 显式同步
```

### 5.2 Nullable 支持

`LogTextWriter.Write(string? s)` 支持 nullable 输入，空字符串调用被安全处理。

### 5.3 `AppendLine()` 空白行

F# 版本没有直接输出空白行的方法（需要用 `log.AppendLine("")`），C# 版本提供专用的无参重载。

---

## 6. 缺失功能说明

以下 F# 原始版本的功能在 C# 版本中**不存在且不需要**：

| 功能 | 原因 |
|---|---|
| `printfBrush` / `printfnBrush` | F# 专用的 `Printf.kprintf` 类型安全格式化，C# 使用字符串插值 |
| `printfColor` / `printfnColor` | 同上 |
| `printfLastColor` / `printfnLastColor` | 同上 |
| `ILogger` 接口实现 | 已在原始版本 v0.20.0 中移除（因 Velopack 兼容性问题） |
| `LogStreamWriter` | 原始版本中是注释掉的实验性代码（ANSI 转义码支持未完成） |
| `module Util.ignore` | F# 专用函数，防止部分应用被意外忽略 |

---

## 7. 待处理事项

以下为非核心功能的后续工作：

### 7.1 README.md

当前 README 与 F# 原始版本完全相同，需要更新：

- 移除 "Including F# `printf` formatting" 描述
- 更新目标框架说明为 `net10.0-windows`
- 移除 "When used from C# add a reference to FSharp.Core 6.0.7 or higher"
- 添加 C# 使用示例

### 7.2 GitHub Actions

| 工作流 | 问题 |
|---|---|
| `build.yml` | 步骤名仍为 `Build fsproj`，应改为 `Build csproj` |
| `docs.yml` | C# 项目未安装 `fsdocs-tool`（`dotnet-tools.json` 为空），需替换为 C# 文档工具 |
| `outdatedNuget.yml` | 仍排除 `FSharp.Core` 和 `Microsoft.Extensions.Logging`（不再需要的依赖） |

### 7.3 文档生成

- `dotnet-tools.json` 为空，需要安装适合 C# 项目的文档工具（如 `docfx`）
- `docs.yml` 工作流需要同步替换
- `.csproj` 中需要移除 `FsDocs*` 属性，添加新文档工具的对应配置

### 7.4 NuGet 发布

`releaseNuget.yml` 发布的是 `symbols.nupkg` 包，机制仍适用。包标签已更新为 `csharp`。

---

## 附录 A：核心架构说明

AvalonLog 的核心机制保持不变：

### 缓冲与限流

```
PrintOrBuffer(text) 
    │
    ├─→ 写入 StringBuilder 缓冲区
    ├─→ 记录颜色偏移 (_offsetColors)
    │
    └─→ 两种打印触发模式：
         ├─ CASE 1: 距上次打印 > _printInterval ms → 立即打印
         └─ CASE 2: 等待 _lastPrintDelay ms，若期间无新调用 → 打印
```

### 颜色渲染管线

```
Append/Print 调用
    │
    ├─→ _offsetColors: List<NewColor> 记录颜色变化偏移
    │
    └─→ ColorizingTransformer (DocumentColorizingTransformer)
         ├─ 每行渲染时被调用
         ├─ RangeColor.GetInRange() 二分搜索获取该行颜色区间
         ├─ ForegroundBrush 应用到各区间
         └─ 选区排除：选区范围内的文本不着色
```

### 线程模型

- 任意线程均可调用 `Append` / `Print` 等方法
- 内部使用 `lock(_buffer)` 保护缓冲区
- 实际 UI 更新通过 `_log.Dispatcher.Invoke()` 回到 UI 线程
- `SyncAvalonLog.Context` 惰性初始化 `DispatcherSynchronizationContext`

---

## 附录 B：文件对照表

```
原始 (F#)                                 当前 (C#)
───────────────────────────────────────   ───────────────────────────────────
Src/AvalonLog.fs                           Src/AvalonLog.cs
Src/AvalonLog.fsproj                       Src/AvalonLog.csproj
Src/Brush.fs                               Src/Brush.cs
Src/LogTextWriter.fs                       Src/LogTextWriter.cs
Src/SelectedTextHighlighter.fs             Src/SelectedTextHighlighter.cs
Src/Sync.fs                                Src/Sync.cs
Src/TextColor.fs                           Src/TextColor.cs
Src/Util.fs                                Src/Util.cs
AvalonLog.sln                              AvalonLog.slnx
.config/dotnet-tools.json                  .config/dotnet-tools.json
.github/workflows/build.yml                .github/workflows/build.yml
.github/workflows/cleanup.yml              .github/workflows/cleanup.yml
.github/workflows/docs.yml                 .github/workflows/docs.yml
.github/workflows/releaseNuget.yml         .github/workflows/releaseNuget.yml
.github/workflows/outdatedNuget.yml        .github/workflows/outdatedNuget.yml
.github/workflows/outdatedDotnetTool.yml.disabled  .github/workflows/outdatedDotnetTool.yml.disabled
.github/dependabot.yml                     .github/dependabot.yml
.gitignore                                 .gitignore
README.md                                  README.md
CHANGELOG.md                               CHANGELOG.md
LICENSE.md                                 LICENSE.md
Docs/img/*                                 Docs/img/*
```
