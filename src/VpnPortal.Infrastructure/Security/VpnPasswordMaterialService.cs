using System.Security.Cryptography;
using System.Text;
using VpnPortal.Application.Contracts.Users;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Infrastructure.Security;

public sealed class VpnPasswordMaterialService(IPasswordHasher passwordHasher) : IVpnPasswordMaterialService
{
    public VpnPasswordMaterial Create(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return new VpnPasswordMaterial(passwordHasher.Hash(password), Md4HashUtf16Le(password));
    }

    private static string Md4HashUtf16Le(string value)
    {
        var input = Encoding.Unicode.GetBytes(value);
        var hash = Md4.Hash(input);
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    // Minimal MD4 implementation for NT hash generation.
    private static class Md4
    {
        public static byte[] Hash(byte[] input)
        {
            var state = new uint[]
            {
                0x67452301,
                0xefcdab89,
                0x98badcfe,
                0x10325476
            };

            var padded = Pad(input);
            var block = new uint[16];

            for (var offset = 0; offset < padded.Length; offset += 64)
            {
                for (var i = 0; i < 16; i++)
                {
                    block[i] = BitConverter.ToUInt32(padded, offset + (i * 4));
                }

                var a = state[0];
                var b = state[1];
                var c = state[2];
                var d = state[3];

                Round1(ref a, b, c, d, block[0], 3);
                Round1(ref d, a, b, c, block[1], 7);
                Round1(ref c, d, a, b, block[2], 11);
                Round1(ref b, c, d, a, block[3], 19);
                Round1(ref a, b, c, d, block[4], 3);
                Round1(ref d, a, b, c, block[5], 7);
                Round1(ref c, d, a, b, block[6], 11);
                Round1(ref b, c, d, a, block[7], 19);
                Round1(ref a, b, c, d, block[8], 3);
                Round1(ref d, a, b, c, block[9], 7);
                Round1(ref c, d, a, b, block[10], 11);
                Round1(ref b, c, d, a, block[11], 19);
                Round1(ref a, b, c, d, block[12], 3);
                Round1(ref d, a, b, c, block[13], 7);
                Round1(ref c, d, a, b, block[14], 11);
                Round1(ref b, c, d, a, block[15], 19);

                Round2(ref a, b, c, d, block[0], 3);
                Round2(ref d, a, b, c, block[4], 5);
                Round2(ref c, d, a, b, block[8], 9);
                Round2(ref b, c, d, a, block[12], 13);
                Round2(ref a, b, c, d, block[1], 3);
                Round2(ref d, a, b, c, block[5], 5);
                Round2(ref c, d, a, b, block[9], 9);
                Round2(ref b, c, d, a, block[13], 13);
                Round2(ref a, b, c, d, block[2], 3);
                Round2(ref d, a, b, c, block[6], 5);
                Round2(ref c, d, a, b, block[10], 9);
                Round2(ref b, c, d, a, block[14], 13);
                Round2(ref a, b, c, d, block[3], 3);
                Round2(ref d, a, b, c, block[7], 5);
                Round2(ref c, d, a, b, block[11], 9);
                Round2(ref b, c, d, a, block[15], 13);

                Round3(ref a, b, c, d, block[0], 3);
                Round3(ref d, a, b, c, block[8], 9);
                Round3(ref c, d, a, b, block[4], 11);
                Round3(ref b, c, d, a, block[12], 15);
                Round3(ref a, b, c, d, block[2], 3);
                Round3(ref d, a, b, c, block[10], 9);
                Round3(ref c, d, a, b, block[6], 11);
                Round3(ref b, c, d, a, block[14], 15);
                Round3(ref a, b, c, d, block[1], 3);
                Round3(ref d, a, b, c, block[9], 9);
                Round3(ref c, d, a, b, block[5], 11);
                Round3(ref b, c, d, a, block[13], 15);
                Round3(ref a, b, c, d, block[3], 3);
                Round3(ref d, a, b, c, block[11], 9);
                Round3(ref c, d, a, b, block[7], 11);
                Round3(ref b, c, d, a, block[15], 15);

                state[0] += a;
                state[1] += b;
                state[2] += c;
                state[3] += d;
            }

            var result = new byte[16];
            Buffer.BlockCopy(state, 0, result, 0, 16);
            return result;
        }

        private static byte[] Pad(byte[] input)
        {
            var bitLength = (ulong)input.Length * 8;
            var paddingLength = (56 - ((input.Length + 1) % 64) + 64) % 64;
            var output = new byte[input.Length + 1 + paddingLength + 8];
            Buffer.BlockCopy(input, 0, output, 0, input.Length);
            output[input.Length] = 0x80;
            Buffer.BlockCopy(BitConverter.GetBytes(bitLength), 0, output, output.Length - 8, 8);
            return output;
        }

        private static uint F(uint x, uint y, uint z) => (x & y) | (~x & z);
        private static uint G(uint x, uint y, uint z) => (x & y) | (x & z) | (y & z);
        private static uint H(uint x, uint y, uint z) => x ^ y ^ z;
        private static uint RotateLeft(uint value, int bits) => (value << bits) | (value >> (32 - bits));

        private static void Round1(ref uint a, uint b, uint c, uint d, uint xk, int s) => a = RotateLeft(a + F(b, c, d) + xk, s);
        private static void Round2(ref uint a, uint b, uint c, uint d, uint xk, int s) => a = RotateLeft(a + G(b, c, d) + xk + 0x5a827999, s);
        private static void Round3(ref uint a, uint b, uint c, uint d, uint xk, int s) => a = RotateLeft(a + H(b, c, d) + xk + 0x6ed9eba1, s);
    }
}
