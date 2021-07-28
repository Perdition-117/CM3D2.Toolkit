using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CM3D2.Toolkit.Guest4168Branch.NUtyLocal
{
    public partial class NUtyLocal
    {
        private static string[] encoToStr = new string[3]
        {
            "shift_jis",
            "utf-8",
            "utf-16"
        };

        public static byte[] ConvStringToByte(string f_strSrc, NUtyLocal.ENCO f_eDestEnc)
        {
            return Encoding.GetEncoding(NUtyLocal.encoToStr[(int)f_eDestEnc]).GetBytes(f_strSrc);
        }

        public static byte[] ConvStringToByte(string f_strSrc, string f_strDestEnc)
        {
            return Encoding.GetEncoding(f_strDestEnc).GetBytes(f_strSrc);
        }

        public static string ConvByteToString(NUtyLocal.ENCO f_eSrcEnc, byte[] f_bySrc)
        {
            return Encoding.UTF8.GetString(Encoding.Convert(Encoding.GetEncoding(NUtyLocal.encoToStr[(int)f_eSrcEnc]), Encoding.UTF8, f_bySrc));
        }

        public static string SjisToUnicode(byte[] sjis_bytes)
        {
            List<byte> byteList = new List<byte>();
            for (int index = 0; index < sjis_bytes.Length; ++index)
            {
                ushort num1 = sjis_bytes[index] >= (byte)129 && sjis_bytes[index] <= (byte)159 || sjis_bytes[index] >= (byte)224 && sjis_bytes[index] <= (byte)234 ? (ushort)((uint)(ushort)((uint)sjis_bytes[index] << 8) + (uint)sjis_bytes[++index]) : (ushort)sjis_bytes[index];
                ushort num2 = NUtyLocal.m_ToUnicodeTable[(int)num1];
                byte num3 = (byte)((uint)num2 >> 8);
                byte num4 = (byte)((uint)num2 & (uint)byte.MaxValue);
                byteList.Add(num4);
                byteList.Add(num3);
            }
            return Encoding.Unicode.GetString(byteList.ToArray());
        }

        public enum ENCO
        {
            // Field SHIFT_JIS with token 040000B2
            SHIFT_JIS,
            // Field UTF_8 with token 040000B3
            UTF_8,
            // Field UTF_16 with token 040000B4
            UTF_16,
            // Field MAX with token 040000B5
            MAX,
        }
    }
}
