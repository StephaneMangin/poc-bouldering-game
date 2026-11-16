using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

/// <summary>Minimal RGBA PNG encoder (no image library on Linux .NET).</summary>
public static class PngWriter
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>Pixels are given bottom row first (Unity texture order); PNG rows go top first.</summary>
    public static void Write(string path, int width, int height, Color32[] pixels)
    {
        using var file = File.Create(path);
        file.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var header = new byte[13];
        WriteBigEndian(header, 0, (uint)width);
        WriteBigEndian(header, 4, (uint)height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(file, "IHDR", header);
        WriteChunk(file, "IDAT", Compress(width, height, pixels));
        WriteChunk(file, "IEND", new byte[0]);
    }

    private static byte[] Compress(int width, int height, Color32[] pixels)
    {
        using var buffer = new MemoryStream();
        using (var zlib = new ZLibStream(buffer, System.IO.Compression.CompressionLevel.Optimal, true))
        {
            var row = new byte[1 + width * 4];
            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = 0; x < width; x++)
                {
                    var p = pixels[y * width + x];
                    row[1 + x * 4] = p.r;
                    row[2 + x * 4] = p.g;
                    row[3 + x * 4] = p.b;
                    row[4 + x * 4] = p.a;
                }

                zlib.Write(row, 0, row.Length);
            }
        }

        return buffer.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        WriteBigEndian(length, 0, (uint)data.Length);
        stream.Write(length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);
        var crc = 0xFFFFFFFFu;
        crc = Update(crc, typeBytes);
        crc = Update(crc, data);
        var crcBytes = new byte[4];
        WriteBigEndian(crcBytes, 0, crc ^ 0xFFFFFFFFu);
        stream.Write(crcBytes);
    }

    private static uint Update(uint crc, byte[] data)
    {
        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static void WriteBigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
