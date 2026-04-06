# SDBU 串口协议工具实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 创建基于Avalonia UI的串口协议解析工具，支持协议帧配置、消息/信号管理、SDBU文件读写

**Architecture:** 参考dbc_Tools项目，使用Avalonia UI跨平台框架，采用MVC模式，数据模型与视图分离

**Tech Stack:** 
- .NET 9.0 + Avalonia UI 11.x
- 参照dbc_Tools的项目结构

---

## 项目结构

```
sdbu_Tools/
├── SdbTools.sln
├── SdbTools/
│   ├── SdbTools.csproj
│   ├── Program.cs
│   ├── App.axaml / App.axaml.cs
│   ├── Models/
│   │   ├── SdbuSignal.cs
│   │   └── ProtocolFrame.cs
│   ├── Views/
│   │   └── MainWindow.axaml / MainWindow.axaml.cs
│   ├── Writers/
│   │   └── SdbuWriter.cs
│   └── Parsers/
│       └── SdbuParser.cs
```

---

## Task 1: 创建项目基础结构

**Files:**
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools.sln`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\SdbTools.csproj`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Program.cs`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\App.axaml`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\App.axaml.cs`

- [ ] **Step 1: 创建解决方案和项目**

```bash
cd D:\ScudPower\UpperComputer\sdbu_Tools
dotnet new sln -n SdbTools
dotnet new avalonia -n SdbTools -o SdbTools
dotnet sln add SdbTools/SdbTools.csproj
```

- [ ] **Step 2: 添加项目引用**

```bash
cd D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools
dotnet add package Avalonia.Desktop
```

- [ ] **Step 3: 验证项目编译**

```bash
cd D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools
dotnet build
```

---

## Task 2: 数据模型定义

**Files:**
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Models\SdbuSignal.cs`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Models\ProtocolFrame.cs`

- [ ] **Step 1: 定义枚举**

在 `SdbuSignal.cs` 中添加：
- FieldType枚举 (None, Header, Command, SubCommand, Version, DeviceAddress, Length, Data, CheckSum, Footer)
- ValueType枚举 (unsigned, signed, IEEE_float, IEEE_double)
- ByteOrder枚举 (Motorola, Intel)
- CheckSumType枚举 (None, CRC8, CRC16, CRC32, Sum, XOR)

- [ ] **Step 2: 创建 SdbuSignal 类**

```csharp
public class SdbuSignal
{
    public uint MessageId { get; set; }
    public byte MessageDlc { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StartBit { get; set; }
    public int Length { get; set; }
    public double Factor { get; set; }
    public double Offset { get; set; }
    public string Unit { get; set; } = string.Empty;
    public ByteOrder ByteOrder { get; set; }
    public ValueType ValueType { get; set; }
}
```

- [ ] **Step 3: 创建 ProtocolFrame 类**

```csharp
public class ProtocolFrame
{
    public ushort EnableFlags { get; set; }  // 16位标志
    public byte[] ProtocolConfig { get; set; } = new byte[16];  // 每位置 [类型4bit|长度4bit]
}
```

- [ ] **Step 4: 创建主项目数据类**

```csharp
public class SdbuProject
{
    public ProtocolFrame Protocol { get; set; } = new();
    public List<SdbuSignal> Signals { get; set; } = new();
}
```

---

## Task 3: SDBU文件读写

**Files:**
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Writers\SdbuWriter.cs`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Parsers\SdbuParser.cs`

- [ ] **Step 1: 实现 SdbuWriter**

```csharp
public static class SdbuWriter
{
    private const string MagicNumber = "sdbu";
    
    public static void Write(string outputPath, SdbuProject project)
    {
        // 文件头写入 (32字节)
        // 协议配置写入
        // CRC16计算
        // 信号体写入 (每个64字节)
    }
    
    private static ushort Crc16(byte[] data, int offset, int length);
    private static void WriteString(BinaryWriter bw, string value, int fixedLength);
}
```

- [ ] **Step 2: 实现 SdbuParser**

```csharp
public static class SdbuParser
{
    public static SdbuProject Parse(string filePath);
    private static ushort ReadCrc16(byte[] data);
    private static string ReadString(byte[] data, int offset, int length);
}
```

- [ ] **Step 3: 测试读写功能**

编写测试代码验证读写一致性

---

## Task 4: 主界面UI实现

**Files:**
- Modify: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\MainWindow.axaml`
- Modify: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\MainWindow.axaml.cs`

- [ ] **Step 1: 设计主界面布局**

```axaml
<Window>
    <DockPanel>
        <Menu DockPanel.Dock="Top">
            <MenuItem Header="_File">
                <MenuItem Header="_New" Click="NewProject"/>
                <MenuItem Header="_Open" Click="OpenProject"/>
                <MenuItem Header="_Save" Click="SaveProject"/>
                <MenuItem Header="Save _As" Click="SaveProjectAs"/>
            </MenuItem>
        </Menu>
        
        <Grid ColumnDefinitions="*,*">
            <!-- 左侧: 消息列表 -->
            <Panel Grid.Column="0">
                <StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <Button Content="+ 新增" Click="AddMessage"/>
                        <Button Content="- 删除" Click="DeleteMessage"/>
                        <Button Content="✎ 编辑" Click="EditMessage"/>
                    </StackPanel>
                    <ListBox x:Name="MessageList" SelectionChanged="OnMessageSelected"/>
                </StackPanel>
            </Panel>
            
            <!-- 右侧: 信号列表 -->
            <Panel Grid.Column="1">
                <StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <Button Content="+ 新增" Click="AddSignal"/>
                        <Button Content="- 删除" Click="DeleteSignal"/>
                        <Button Content="✎ 编辑" Click="EditSignal"/>
                    </StackPanel>
                    <ListBox x:Name="SignalList"/>
                </StackPanel>
            </Panel>
        </Grid>
    </DockPanel>
</Window>
```

- [ ] **Step 2: 添加协议配置面板**

在下方添加TabControl:
- Tab1: 协议配置 (EnableFlags设置 + 字段顺序配置)
- Tab2: 消息属性 (选中的消息编辑)
- Tab3: 信号属性 (选中的信号编辑)

- [ ] **Step 3: 实现基本交互**

- 新增/删除/编辑消息
- 新增/删除/编辑信号
- 消息选择后显示对应信号

---

## Task 5: 协议配置UI

**Files:**
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\ProtocolConfigDialog.axaml`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\ProtocolConfigDialog.axaml.cs`

- [ ] **Step 1: 创建协议配置对话框**

- 16个字段位置的配置，每行显示：
  - 位置序号 (1-16)
  - 字段类型下拉 (None/Header/Command/SubCommand/...)
  - 长度输入 (0-15)
- EnableFlags 复选框 (bit0-bit15)

- [ ] **Step 2: 实现配置保存/加载**

与SdbuProject的ProtocolFrame属性绑定

---

## Task 6: 信号编辑器UI

**Files:**
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\SignalEditorDialog.axaml`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\SignalEditorDialog.axaml.cs`

- [ ] **Step 1: 创建信号编辑对话框**

字段:
- MessageId: 数值输入
- MessageDlc: 数值输入
- Name: 文本输入
- StartBit: 数值输入 (0-511)
- Length: 数值输入 (1-64)
- Factor: 浮点数输入
- Offset: 浮点数输入
- Unit: 文本输入
- ByteOrder: 下拉 (Motorola/Intel)
- ValueType: 下拉 (unsigned/signed/IEEE_float/IEEE_double)

---

## Task 7: 消息编辑器UI

**Files:**
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\MessageEditorDialog.axaml`
- Create: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Views\MessageEditorDialog.axaml.cs`

- [ ] **Step 1: 创建消息编辑对话框**

- MessageId: 数值输入
- MessageName: 文本输入 (可选，用于显示)
- MessageDlc: 数值输入

---

## Task 8: 项目集成与测试

**Files:**
- Modify: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\Program.cs`
- Modify: `D:\ScudPower\UpperComputer\sdbu_Tools\SdbTools\App.axaml.cs`

- [ ] **Step 1: 集成文件操作**

- New: 创建空项目
- Open: 解析SDBU文件
- Save: 写入SDBU文件

- [ ] **Step 2: 测试完整流程**

1. 新建项目
2. 配置协议 (Header 2字节, Command 1字节, Length 2字节, Data, CheckSum 1字节)
3. 添加消息 (ID=0x01, DLC=8)
4. 添加信号 (SOC, StartBit=0, Length=8, Factor=0.1, Offset=0)
5. 保存文件
6. 重新打开文件，验证数据完整

---

## Task 9: 完善与优化

**Files:**
- Modify: Various

- [ ] **Step 1: 添加输入验证**

- 起始位范围检查 (0-511)
- 长度范围检查 (1-64)
- 消息ID范围检查

- [ ] **Step 2: 添加常用协议模板**

- 预定义几种常见协议配置供选择

- [ ] **Step 3: 添加状态栏**

显示当前文件路径、操作状态等

---

## 预期产出

1. 可编译运行的Avalonia UI应用程序
2. 支持SDBU文件创建、打开、保存
3. 左右分栏界面：消息列表 + 信号列表
4. 协议配置、消息编辑、信号编辑功能
5. 符合SDBU_FORMAT.md定义的二进制文件格式
