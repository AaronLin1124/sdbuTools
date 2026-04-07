using System.Collections.Generic;
using System;

namespace SdbTools.Models;

public enum FieldType
{
    None = 0,
    Header = 1,
    Command = 2,
    SubCommand = 3,
    Version = 4,
    DeviceAddress = 5,
    Length = 6,
    Data = 7,
    CheckSum = 8,
    Footer = 9,
    Reserved10 = 10,
    Reserved11 = 11,
    Reserved12 = 12,
    Reserved13 = 13,
    Reserved14 = 14,
    Reserved15 = 15
}

public enum ValueTypeEnum
{
    Unsigned = 0,
    Signed = 1,
    IEEEFloat = 2,
    IEEEDouble = 3
}

public enum ByteOrderEnum
{
    Motorola = 0,
    Intel = 1
}

public enum CheckSumType
{
    None = 0,
    CRC8 = 1,
    CRC16 = 2,
    CRC32 = 3,
    Sum = 4,
    XOR = 5
}

public class SdbuSignal
{
    public uint MessageId { get; set; }
    public int StartBit { get; set; }
    public int Length { get; set; }
    public double Factor { get; set; } = 1.0;
    public double Offset { get; set; }
    public string Unit { get; set; } = string.Empty;
    public ByteOrderEnum ByteOrder { get; set; } = ByteOrderEnum.Intel;
    public ValueTypeEnum ValueType { get; set; } = ValueTypeEnum.Unsigned;
    public string Name { get; set; } = string.Empty;
    public string MessageName { get; set; } = string.Empty;
    
    public byte MessageDlc { get; set; }
}

public class SdbuMessage
{
    public uint MessageId { get; set; }
    public byte MessageDlc { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<SdbuSignal> Signals { get; set; } = new();
}

public class ProtocolFrame
{
    public byte[] ProtocolConfig { get; set; } = new byte[32];
}

public class SdbuProject
{
    public ProtocolFrame Protocol { get; set; } = new();
    public List<SdbuMessage> Messages { get; set; } = new();
    public string? FilePath { get; set; }
    public bool IsDirty { get; set; }
}
