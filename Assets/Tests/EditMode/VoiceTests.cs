using System;
using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    public class VoiceTests
    {
        static float[] Sine(int count, float amplitude, float frequency = 440f)
        {
            var samples = new float[count];
            for (var i = 0; i < count; i++)
                samples[i] = amplitude * (float)Math.Sin(2 * Math.PI * frequency * i / VoiceCodec.SampleRate);
            return samples;
        }

        [Test]
        public void ACodedVoice_IsOneBytePerSample_AndComesBackClose()
        {
            var original = Sine(VoiceCodec.ChunkSamples, 0.5f);
            var bytes = VoiceCodec.Encode(original, original.Length);
            Assert.That(bytes.Length, Is.EqualTo(VoiceCodec.ChunkSamples));

            var back = new float[bytes.Length];
            VoiceCodec.Decode(bytes, back);
            for (var i = 0; i < original.Length; i++)
                Assert.That(back[i], Is.EqualTo(original[i]).Within(0.02f), "sample " + i);
        }

        [Test]
        public void QuietSound_StaysQuiet_AndLoudSoundDoesNotWrapAround()
        {
            Assert.That(Math.Abs(VoiceCodec.Decode(VoiceCodec.Encode(0f))), Is.LessThan(0.005f));
            Assert.That(VoiceCodec.Decode(VoiceCodec.Encode(1.5f)), Is.GreaterThan(0.9f), "too loud is cut off at full");
            Assert.That(VoiceCodec.Decode(VoiceCodec.Encode(-1.5f)), Is.LessThan(-0.9f));
        }

        [Test]
        public void TheBuffer_WaitsForALittle_BeforePlaying()
        {
            var buffer = new VoiceBuffer(1000, 200);
            buffer.Write(new float[100].Select(1f), 100);

            var output = new float[50];
            buffer.Read(output);
            Assert.That(output, Is.All.EqualTo(0f), "still filling up");

            buffer.Write(new float[150].Select(1f), 150);
            buffer.Read(output);
            Assert.That(output, Is.All.EqualTo(1f), "now it plays");
        }

        [Test]
        public void TheBuffer_PlaysInOrder_AndGoesQuietWhenEmpty()
        {
            var buffer = new VoiceBuffer(1000, 1);
            buffer.Write(new[] { 1f, 2f, 3f }, 3);

            var output = new float[5];
            buffer.Read(output);
            Assert.That(output, Is.EqualTo(new[] { 1f, 2f, 3f, 0f, 0f }));
            Assert.That(buffer.Available, Is.EqualTo(0));
        }

        [Test]
        public void TheBuffer_DropsTheOldest_WhenItOverflows()
        {
            var buffer = new VoiceBuffer(4, 1);
            buffer.Write(new[] { 1f, 2f, 3f }, 3);
            buffer.Write(new[] { 4f, 5f, 6f }, 3);

            var output = new float[4];
            buffer.Read(output);
            Assert.That(output, Is.EqualTo(new[] { 3f, 4f, 5f, 6f }));
        }

        [Test]
        public void TheGate_PassesSpeechAndABitAfter_ButNotSilence()
        {
            var gate = new VoiceGate(0.02f, 2);
            var speech = Sine(VoiceCodec.ChunkSamples, 0.3f);
            var silence = new float[VoiceCodec.ChunkSamples];

            Assert.That(gate.ShouldSend(silence, silence.Length), Is.False);
            Assert.That(gate.ShouldSend(speech, speech.Length), Is.True);
            Assert.That(gate.ShouldSend(silence, silence.Length), Is.True, "the end of a word");
            Assert.That(gate.ShouldSend(silence, silence.Length), Is.True);
            Assert.That(gate.ShouldSend(silence, silence.Length), Is.False, "then quiet again");
        }
    }

    static class VoiceTestExtensions
    {
        public static float[] Select(this float[] array, float value)
        {
            for (var i = 0; i < array.Length; i++) array[i] = value;
            return array;
        }
    }
}
