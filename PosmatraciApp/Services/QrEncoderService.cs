using PosmatraciApp.Models;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace PosmatraciApp.Services
{
    public class QrEncoderService
    {
        /// <summary>
        /// Encodes BmState into a compact base64url string for the QR code.
        /// Binary format: HEADER(12) + ANSWERS(21) + SEVERITIES(21) + NOTES(variable)
        /// Then deflate-compressed and base64url-encoded.
        /// </summary>
        public string EncodeToBase64Url(BmState state, string emailHash)
        {
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                // HEADER
                bw.Write((byte)0x50); // 'P'
                bw.Write((byte)0x01); // version
                WriteUInt16BE(bw, (ushort)state.BmShortId);
                WriteUInt32BE(bw, (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                var hashBytes = Convert.FromHexString(emailHash.PadRight(8, '0')[..8]);
                bw.Write(hashBytes); // 4 bytes

                // ANSWERS: 2 bits per item, 83 items → 21 bytes
                PackBits(bw, state.Answers.Length, i => (int)state.Answers[i], 2);

                // SEVERITIES: 2 bits per item (None/N=0, S=1, V=2, K=3)
                PackBits(bw, state.Severities.Length, i =>
                {
                    return state.Severities[i] switch
                    {
                        CheckSeverity.None or CheckSeverity.N => 0,
                        CheckSeverity.S => 1,
                        CheckSeverity.V => 2,
                        CheckSeverity.K => 3,
                        _ => 0
                    };
                }, 2);

                // NOTES
                var noteList = new List<(int id, byte[] text)>();
                foreach (var (id, note) in state.Notes)
                {
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        var bytes = Encoding.UTF8.GetBytes(note);
                        noteList.Add((id, bytes[..Math.Min(bytes.Length, 255)]));
                    }
                }
                bw.Write((byte)noteList.Count);
                foreach (var (id, textBytes) in noteList)
                {
                    bw.Write((byte)id);
                    bw.Write((byte)textBytes.Length);
                    bw.Write(textBytes);
                }
            }

            // Deflate compress
            var raw = ms.ToArray();
            using var compressed = new MemoryStream();
            using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal))
                deflate.Write(raw);

            return Base64UrlEncode(compressed.ToArray());
        }

        /// <summary>
        /// Generates a QR code PNG as base64 string from the payload.
        /// </summary>
        public string GenerateQrPngBase64(string payload)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var qrCode = new PngByteQRCode(qrData);
            var pngBytes = qrCode.GetGraphic(10);
            return Convert.ToBase64String(pngBytes);
        }

        private static void PackBits(BinaryWriter bw, int count, Func<int, int> getValue, int bitsPerItem)
        {
            int buffer = 0;
            int bitsInBuffer = 0;
            for (int i = 0; i < count; i++)
            {
                int val = getValue(i) & ((1 << bitsPerItem) - 1);
                buffer = (buffer << bitsPerItem) | val;
                bitsInBuffer += bitsPerItem;
                while (bitsInBuffer >= 8)
                {
                    bitsInBuffer -= 8;
                    bw.Write((byte)(buffer >> bitsInBuffer));
                    buffer &= (1 << bitsInBuffer) - 1;
                }
            }
            if (bitsInBuffer > 0)
            {
                buffer <<= (8 - bitsInBuffer);
                bw.Write((byte)buffer);
            }
        }

        private static void WriteUInt16BE(BinaryWriter bw, ushort value)
        {
            bw.Write((byte)(value >> 8));
            bw.Write((byte)(value & 0xFF));
        }

        private static void WriteUInt32BE(BinaryWriter bw, uint value)
        {
            bw.Write((byte)(value >> 24));
            bw.Write((byte)(value >> 16 & 0xFF));
            bw.Write((byte)(value >> 8 & 0xFF));
            bw.Write((byte)(value & 0xFF));
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}
