using System;

namespace VRB.Core
{
    /// <summary>
    /// Squeezes speech into one byte per sample (G.711 mu-law) so a voice costs 16 KB/s at 16 kHz, which a local network
    /// carries easily and which sounds fine for talking.
    /// </summary>
    public static class VoiceCodec
    {
        public const int SampleRate = 16000;

        /// <summary>Samples per message: 40 ms, small enough to fit one network packet.</summary>
        public const int ChunkSamples = 640;

        const int Bias = 0x84;
        const int Clip = 32635;

        public static byte Encode(float sample)
        {
            var value = (int)(Math.Max(-1f, Math.Min(1f, sample)) * 32767f);
            var sign = (value >> 8) & 0x80;
            if (sign != 0) value = -value;
            if (value > Clip) value = Clip;
            value += Bias;

            var exponent = 7;
            for (var mask = 0x4000; (value & mask) == 0 && exponent > 0; exponent--, mask >>= 1) { }
            var mantissa = (value >> (exponent + 3)) & 0x0F;
            return (byte)~(sign | (exponent << 4) | mantissa);
        }

        public static float Decode(byte encoded)
        {
            encoded = (byte)~encoded;
            var sign = encoded & 0x80;
            var exponent = (encoded >> 4) & 0x07;
            var mantissa = encoded & 0x0F;
            var value = (((mantissa << 3) + Bias) << exponent) - Bias;
            return (sign != 0 ? -value : value) / 32768f;
        }

        public static byte[] Encode(float[] samples, int count)
        {
            var bytes = new byte[count];
            for (var i = 0; i < count; i++) bytes[i] = Encode(samples[i]);
            return bytes;
        }

        public static void Decode(byte[] encoded, float[] samples)
        {
            for (var i = 0; i < encoded.Length; i++) samples[i] = Decode(encoded[i]);
        }
    }
}
