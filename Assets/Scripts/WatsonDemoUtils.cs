using System.Collections.Generic;
using System;

public class WatsonDemoUtils
{
    /// <summary>
    /// Concatenate two byte arrays together.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static byte[] ConcatenateByteArrays(byte[] a, byte[] b)
    {
        if (a == null || a.Length == 0)
        {
            return b;
        }
        else if (b == null || b.Length == 0)
        {
            return a;
        }
        else
        {
            List<byte> list1 = new List<byte>(a);
            List<byte> list2 = new List<byte>(b);
            list1.AddRange(list2);
            byte[] result = list1.ToArray();
            return result;
        }
    }

    /// <summary>
    /// Converts single channel pcm audio (Signed L16 ints in byte encoding) to -1 to 1 floats for audio playback in unity.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static float[] PCM2Floats(byte[] bytes)
    {
        // See pcm2float in https://github.com/mgeier/python-audio/blob/master/audio-files/utility.py
        float max = -short.MinValue;
        float[] samples = new float[bytes.Length / 2];

        for (int i = 0; i < samples.Length; i++)
        {
            short int16sample = BitConverter.ToInt16(bytes, i * 2);
            samples[i] = int16sample / max;
        }

        return samples;
    }

}
