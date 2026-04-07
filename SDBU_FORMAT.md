# SDBU 文件格式定义文档

## 1. 文件概述

SDBU (Signal Database for UART) 是一种用于串口协议信号定义的二进制文件格式。

## 2. 字段类型枚举

| 值 | 字段 | 说明 |
|----|------|------|
| 0 | None | 未启用 |
| 1 | Header | 包头 |
| 2 | Command | 命令 |
| 3 | SubCommand | 子命令 |
| 4 | Version | 版本 |
| 5 | DeviceAddress | 设备地址 |
| 6 | Length | 长度字段 |
| 7 | Data | 数据区 |
| 8 | CheckSum | 校验 |
| 9 | Footer | 包尾 |
| 10-15 | Reserved | 预留 |

## 3. 值类型枚举

| 值 | 类型 |
|----|------|
| 0 | unsigned |
| 1 | signed |
| 2 | IEEE float |
| 3 | IEEE double |

## 4. 字节顺序枚举

| 值 | 字节顺序 |
|----|----------|
| 0 | Motorola (Big Endian) |
| 1 | Intel (Little Endian) |

## 5. 校验类型枚举

| 值 | 类型 |
|----|------|
| 0 | None |
| 1 | CRC8 |
| 2 | CRC16 |
| 3 | CRC32 |
| 4 | Sum (求和) |
| 5 | XOR (异或) |

## 6. 文件结构

### 6.1 文件头 (50字节)

| 偏移 | 长度 | 字段 | 说明 |
|------|------|------|------|
| 0 | 4 | Magic | 文件标识，"sdbu" |
| 4 | 1 | HeaderLength | 文件头长度 (50) |
| 5 | 1 | SignalBodyLength | 信号体长度 (56) |
| 6 | 32 | ProtocolConfig | 协议配置，每位置2字节 |
| 38 | 4 | HeaderMagic | 包头特征码 (如 0xEA16) |
| 42 | 4 | FooterMagic | 包尾特征码 (如 0x0D0A) |
| 46 | 2 | CRC16 | 文件CRC16校验 |
| 48 | 2 | SignalCount | 信号数量 |

### 6.2 信号体 (56字节/个)

| 偏移 | 长度 | 字段 | 说明 |
|------|------|------|------|
| 0 | 4 | MessageId | 消息ID (命令值) |
| 4 | 1 | MessageDlc | 数据长度 (字节数) |
| 5 | 16 | Name | 信号名 (UTF-8，16字节) |
| 21 | 16 | MessageName | 消息名 (UTF-8，16字节) |
| 37 | 2 | Packed | 打包数据，高9bit=长度，低9bit=起始位，bit15=字节序(0=Motorola, 1=Intel) |
| 39 | 4 | Factor | 精度 (浮点数) |
| 43 | 4 | Offset | 偏移量 (浮点数) |
| 47 | 8 | Unit | 单位 (UTF-8，8字节) |
| 55 | 1 | Flags | 标志位: b0-b1=值类型 |

### 6.3 Packed字段格式

```
bit15      bit9    bit0
| 字节序  | 长度 | 起始位 |
```

- bit15: 字节序 (0=Motorola, 1=Intel)
- bit9-bit14: 长度 (6bit，最大64)
- bit0-bit8: 起始位 (9bit，最大511)

## 7. 协议配置示例

### 示例1: 标准协议
- 顺序：包头(2字节) → 命令(1字节) → 长度(2字节) → 数据 → 校验(1字节)
- EnableFlags: 0x0103 (bit0=1, bit1=1, bit7=1)
- ProtocolConfig: `[1|2][2|1][6|2][7|0][8|1][0|0]...`

### 示例2: 设备地址+版本
- 顺序：设备地址(1字节) → 版本(1字节) → 命令(1字节) → 数据 → 校验(1字节)
- EnableFlags: 0x0127 (bit0=1, bit1=1, bit2=1, bit4=1, bit5=1, bit7=1)
- ProtocolConfig: `[5|1][4|1][2|1][7|0][8|1][0|0]...`

### 示例3: 带子命令
- 顺序：包头(2字节) → 命令(1字节) → 子命令(1字节) → 长度(2字节) → 数据 → 校验(1字节)
- EnableFlags: 0x010B (bit0=1, bit1=1, bit2=1, bit3=1, bit7=1)
- ProtocolConfig: `[1|2][2|1][3|1][6|2][7|0][8|1][0|0]...`

## 8. CRC16校验算法

采用 Modbus CRC16 算法：
1. CRC 初始值: 0xFFFF
2. 每个字节与 CRC 进行异或
3. 每位右移，低位补0，如果原低位为1，则与 0xA001 异或

## 9. 版本历史

| 版本 | 日期 | 修改内容 |
|------|------|----------|
| 1.0 | 2026-04-06 | 初始版本 |
