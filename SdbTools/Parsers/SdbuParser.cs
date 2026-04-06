using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SdbTools.Models;

namespace SdbTools.Parsers;

public static class SdbuParser
{
    private const string MagicNumber = "sbdu";

    public static SdbuProject Parse(string filePath)
    {
        var project = new SdbuProject { FilePath = filePath };
        var data = File.ReadAllBytes(filePath);

        if (data.Length < 42)
            throw new InvalidDataException("File too short");

        var magic = Encoding.ASCII.GetString(data, 0, 4);
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number: {magic}");

        byte headerLength = data[4];
        byte signalLength = data[5];
        
        project.Protocol.ProtocolConfig = new byte[32];
        Array.Copy(data, 6, project.Protocol.ProtocolConfig, 0, 32);

        ushort storedCrc = BitConverter.ToUInt16(data, 38);
        ushort signalCount = BitConverter.ToUInt16(data, 40);

        ushort computedCrc = Crc16(data, 42, data.Length - 42);
        if (storedCrc != computedCrc)
            throw new InvalidDataException($"CRC mismatch: expected 0x{storedCrc:X}, got 0x{computedCrc:X}");

        int offset = 42;
        var signalDict = new Dictionary<uint, SdbuMessage>();

        for (int i = 0; i < signalCount; i++)
        {
            var sig = ReadSignal(data, offset);
            offset += 64;

            if (!signalDict.TryGetValue(sig.MessageId, out var msg))
            {
                msg = new SdbuMessage { MessageId = sig.MessageId, MessageDlc = sig.MessageDlc };
                signalDict[sig.MessageId] = msg;
                project.Messages.Add(msg);
            }
            msg.Signals.Add(sig);
        }

        return project;
    }

    private static SdbuSignal ReadSignal(byte[] data, int offset)
    {
        var sig = new SdbuSignal
        {
            MessageId = BitConverter.ToUInt32(data, offset),
            MessageDlc = data[offset + 4],
            Name = ReadString(data, offset + 5, 32)
        };

        ushort packed = BitConverter.ToUInt16(data, offset + 37);
        sig.StartBit = packed & 0x1FF;
        sig.Length = (packed >> 9) & 0x3F;
        sig.ByteOrder = (packed & 0x8000) != 0 ? ByteOrderEnum.Intel : ByteOrderEnum.Motorola;

        sig.Factor = BitConverter.ToDouble(data, offset + 39);
        sig.Offset = BitConverter.ToDouble(data, offset + 47);
        sig.Unit = ReadString(data, offset + 55, 16);
        sig.ValueType = (ValueTypeEnum)(data[offset + 71] & 0x0F);

        return sig;
    }

    private static ushort Crc16(byte[] data, int offset, int length)
    {
        ushort crc = 0xFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc >>= 1;
            }
        }
        return crc;
    }

    private static string ReadString(byte[] data, int offset, int length)
    {
        var bytes = new byte[length];
        Array.Copy(data, offset, bytes, 0, length);
        var str = Encoding.UTF8.GetString(bytes);
        return str.TrimEnd('\0');
    }
}
