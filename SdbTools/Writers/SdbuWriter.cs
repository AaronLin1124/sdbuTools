using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SdbTools.Models;

namespace SdbTools.Writers;

public static class SdbuWriter
{
    private const string MagicNumber = "sdbu";
    private const byte HeaderLength = 50;
    private const byte SignalBodyLength = 56;

    public static void Write(string outputPath, SdbuProject project)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteString(bw, MagicNumber, 4);
        bw.Write(HeaderLength);
        bw.Write(SignalBodyLength);
        bw.Write(project.Protocol.ProtocolConfig);
        bw.Write(project.Protocol.HeaderMagic);
        bw.Write(project.Protocol.FooterMagic);
        bw.Write((ushort)0);

        var allSignals = new List<SdbuSignal>();
        foreach (var msg in project.Messages)
        {
            allSignals.AddRange(msg.Signals);
        }
        bw.Write((ushort)allSignals.Count);

        foreach (var msg in project.Messages)
        {
            foreach (var sig in msg.Signals)
            {
                bw.Write(sig.MessageId);
                bw.Write(msg.MessageDlc);
                WriteString(bw, sig.Name, 16);
                WriteString(bw, sig.MessageName, 16);
                ushort packed = (ushort)((sig.Length << 9) | (sig.StartBit & 0x1FF));
                if (sig.ByteOrder == ByteOrderEnum.Intel) packed |= 0x8000;
                bw.Write(packed);
                bw.Write(sig.Factor);
                bw.Write(sig.Offset);
                WriteString(bw, sig.Unit, 8);
                byte flags = (byte)sig.ValueType;
                bw.Write(flags);
                bw.Write((byte)0);
            }
        }

        bw.Flush();
        var data = ms.ToArray();

        ushort crc = Crc16(data, HeaderLength, data.Length - HeaderLength);
        data[48] = (byte)(crc & 0xFF);
        data[49] = (byte)((crc >> 8) & 0xFF);

        File.WriteAllBytes(outputPath, data);
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

    private static void WriteString(BinaryWriter bw, string value, int fixedLength)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var buffer = new byte[fixedLength];
        int copyLen = Math.Min(bytes.Length, fixedLength);
        Array.Copy(bytes, buffer, copyLen);
        bw.Write(buffer);
    }
}
