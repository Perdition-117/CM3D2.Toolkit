using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace NeiLib
{
    internal static class Encryption
    {
        internal static byte[] GenerateIV(byte[] ivSeed)
        {
            uint[] seed =
            {
                0x075BCD15,
                0x159A55E5,
                0x1F123BB5,
                BitConverter.ToUInt32(ivSeed, 0) ^ 0xBFBFBFBF
            };

            for (var i = 0; i < 4; i++)
            {
                var n = seed[0] ^ seed[0] << 11;
                seed[0] = seed[1];
                seed[1] = seed[2];
                seed[2] = seed[3];
                seed[3] = n ^ seed[3] ^ (n ^ seed[3] >> 11) >> 8;
            }

            var output = new byte[16];

            Buffer.BlockCopy(seed, 0, output, 0, 16);

            return output;
        }

        internal static byte[] DecryptBytes(byte[] encryptedBytes, byte[] key)
        {
            var extraDataSize = encryptedBytes[encryptedBytes.Length - 5] ^ encryptedBytes[encryptedBytes.Length - 4];
            var ivSeed = new byte[4];
            Array.Copy(encryptedBytes, encryptedBytes.Length - 4, ivSeed, 0, 4);

            var iv = GenerateIV(ivSeed);

            var aes = Aes.Create();
            aes.Key = key;

            var output = aes.DecryptCbc(encryptedBytes.AsSpan(0, encryptedBytes.Length - 5), iv, PaddingMode.None);

            return output;
        }
        internal static byte[] EncryptBytes(byte[] data, byte[] key, byte[] ivSeed = null)
        {
            Random random = new Random();

            if (ivSeed == null)
                ivSeed = BitConverter.GetBytes(random.Next());

            var iv = GenerateIV(ivSeed);

            byte[] extraData = null;
            if ((data.Length & 0xf) != 0) // Rijndael requires padding to 16 bytes in order to encrypt
            {
                var newSize = (data.Length & 0xfffffff0) + 0x10;
                extraData = new byte[newSize - data.Length];
            }

            var rijndael = new RijndaelManaged();
            rijndael.Padding = PaddingMode.None;

            var encryptor = rijndael.CreateEncryptor(key, iv);
            var mem = new MemoryStream();
            using (var stream = new CryptoStream(mem, encryptor, CryptoStreamMode.Write))
            {
                stream.Write(data, 0, data.Length);
                if (extraData != null)
                    stream.Write(extraData, 0, extraData.Length);
                stream.Flush();

                var extraLength = (byte)(extraData?.Length ?? 0);
                mem.Write(new[] { (byte)(extraLength ^ ivSeed[0]) }, 0, 1);
                mem.Write(ivSeed, 0, ivSeed.Length);
            }

            return mem.ToArray();
        }
    }
}