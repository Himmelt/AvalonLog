![Logo](logo.png)

# AvalonLog

[![license](https://img.shields.io/github/license/Himmelt/AvalonLog)](LICENSE)

AvalonLog 是一个快速且线程安全的 WPF 彩色文本日志查看器。基于 [AvalonEditB](https://github.com/goswinr/AvalonEditB)。适用于 .NET 10.0+（Windows）。

> 本项目 Fork 自 [goswinr/AvalonLog](https://github.com/goswinr/AvalonLog)，原项目使用 F# 编写。
> 本版本使用 C# 完全重写，移除了对 FSharp.Core 的依赖，仅保留 C# API。

## 特性

- **线程安全** — 可以从任意线程调用打印方法
- **高性能** — 缓冲重复打印调用，按时间间隔批量更新视图
- **彩色文本** — 支持 RGB 颜色和 `SolidColorBrush` 自定义文本颜色
- **虚拟化渲染** — 基于 AvalonEdit，可轻松处理数千行文本
- **自动裁剪** — 超过最大行数时自动移除旧内容
- **搜索面板** — 内置搜索功能
- **选中高亮** — 选中文本后自动高亮所有匹配项

## 使用方式

### 基本用法

```csharp
using AvalonLog;
using System.Windows;
using System.Windows.Media;

var log = new AvalonLog();

// 默认颜色输出
log.AppendLine("普通日志消息");

// 使用 RGB 颜色输出
log.AppendLine("红色警告", 255, 0, 0);
log.AppendLine("绿色信息", 0, 155, 0);
log.AppendLine("蓝色提示", 0, 0, 255);

// 使用 SolidColorBrush 输出
log.AppendLine("自定义颜色", Brushes.Orange);

// 追加不换行的文本
log.Append("Hello, ");
log.Append("World! ", 0, 128, 255);
log.AppendLine("← 这行才换行");

// 使用上一次的颜色继续追加
log.AppendWithLastColor("续写内容");
log.AppendLineWithLastColor("续写并换行");

// 清除日志
log.Clear();

new Window { Content = log }.ShowDialog();
```

### 使用 TextWriter

```csharp
// 获取带颜色的 TextWriter，可重定向 Console.Out
var redWriter = log.GetTextWriter(255, 0, 0);
var blueWriter = log.GetTextWriter(Brushes.Blue);

redWriter.WriteLine("这行是红色的");
blueWriter.WriteLine("这行是蓝色的");

// 条件输出 — 仅当谓词返回 true 时才写入
var errorWriter = log.GetConditionalTextWriter(
    s => s.Contains("ERROR"), 255, 0, 0);
errorWriter.WriteLine("INFO: 正常信息");   // 不会输出
errorWriter.WriteLine("ERROR: 出错了！");  // 会输出
```

### 配置属性

```csharp
var log = new AvalonLog();

// 字体设置
log.FontFamily = new FontFamily("Cascadia Code");
log.FontSize = 14.0;

// 自动换行
log.WordWrap = true;

// 最大字符数（超过后停止日志输出）
log.MaximumCharacterAllowance = 1_024_000;

// 最大可见行数（超过后自动裁剪旧内容）
log.MaxVisibleLines = 5000;

// 裁剪比例（行数达到 MaxVisibleLines × TrimRatio 时触发裁剪）
log.TrimRatio = 1.1;

// 显示行号
log.ShowLineNumbers = true;

// 启用超链接
log.EnableHyperlinks = true;

// 打印间隔（毫秒）
log.PrintInterval = 50;

// 控制打印行为
log.IsAlive = true;
```

## API 概览

### 输出方法

| 方法 | 说明 |
|------|------|
| `Append(string)` | 追加文本（默认颜色，不换行） |
| `AppendLine(string)` | 追加文本（默认颜色，换行） |
| `Append(string, SolidColorBrush)` | 追加文本（指定画刷，不换行） |
| `AppendLine(string, SolidColorBrush)` | 追加文本（指定画刷，换行） |
| `Append(string, int, int, int)` | 追加文本（RGB 颜色，不换行） |
| `AppendLine(string, int, int, int)` | 追加文本（RGB 颜色，换行） |
| `AppendWithLastColor(string)` | 使用上次颜色追加（不换行） |
| `AppendLineWithLastColor(string)` | 使用上次颜色追加（换行） |
| `AppendLine()` | 追加空行（继承前一条消息颜色） |
| `PrintLine(string, ...)` | 设置颜色并追加换行文本 |
| `Clear()` | 清除所有日志内容 |

### TextWriter 工厂方法

| 方法 | 说明 |
|------|------|
| `GetTextWriter(int, int, int)` | 获取 RGB 颜色的 TextWriter |
| `GetTextWriter(SolidColorBrush)` | 获取指定画刷的 TextWriter |
| `GetConditionalTextWriter(Func, ...)` | 获取条件输出的 TextWriter |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsAlive` | `bool` | 是否允许输出 |
| `FontFamily` | `FontFamily` | 字体 |
| `FontSize` | `double` | 字号 |
| `WordWrap` | `bool` | 自动换行 |
| `ShowLineNumbers` | `bool` | 显示行号 |
| `EnableHyperlinks` | `bool` | 启用超链接 |
| `MaximumCharacterAllowance` | `int` | 最大字符数 |
| `MaxVisibleLines` | `int` | 最大可见行数 |
| `TrimRatio` | `double` | 裁剪比例（1.0~2.0） |
| `PrintInterval` | `long` | 打印间隔（毫秒） |
| `LastPrintDelay` | `int` | 最后打印延迟（毫秒） |
| `Selection` | `Selection` | 当前选区 |
| `SearchPanel` | `SearchPanel` | 搜索面板 |
| `SelectedTextHighLighter` | `SelectedTextHighlighter` | 选中高亮器 |
| `VerticalScrollBarVisibility` | `ScrollBarVisibility` | 垂直滚动条可见性 |
| `HorizontalScrollBarVisibility` | `ScrollBarVisibility` | 水平滚动条可见性 |

## 安装

通过 NuGet 安装：

```
dotnet add package AvalonLog
```

## 构建

```bash
dotnet build
```

## 与原版的差异

| 特性 | 原版 (goswinr) | 本版 (Himmelt) |
|------|---------------|----------------|
| 语言 | F# | C# |
| FSharp.Core 依赖 | 需要 | 无 |
| F# printf 格式化 | 支持 | 不支持 |
| 目标框架 | .NET Framework 4.7.2+, .NET 7.0+ | .NET 10.0 (Windows) |
| 搜索面板 | — | 内置 |
| 选中高亮 | — | 内置 |
| 累计行号 | — | 裁剪后保持累计计数 |

## 致谢

- 原作者 [Goswin Rothenthal](https://github.com/goswinr) — [goswinr/AvalonLog](https://github.com/goswinr/AvalonLog)
- [AvalonEditB](https://github.com/goswinr/AvalonEditB) — WPF 文本编辑器控件

## 许可证

[MIT](LICENSE)
