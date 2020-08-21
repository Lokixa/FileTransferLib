using System;
using System.Numerics;

namespace FtLib
{
    ///<summary> 
    /// Base255 stateful converter.
    ///</summary>
    ///<remarks>
    /// This class is intended to be used as a storage for Base255-BigInteger related conversions.
    ///</remarks>
    public static class Base255
    {
        ///<summary>Stores the number and stores its conversion to base 255.</summary>
        public static byte[] ToByteArr(BigInteger number, uint size = 0)
        {
            if (number > 0 && size == 0)
                size = (uint)Math.Ceiling(BigInteger.Log(number, 255));
            else if (number < 0)
            {
                return new byte[size];
            }

            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = (byte)(number % 255);
                number /= 255;
            }

            return buffer;
        }
        ///<summary>Stores the buffer and stores its conversion to BigInteger.</summary>
        public static BigInteger ToBigInt(byte[] buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length == 0)
            {
                return 0;
            }
            BigInteger number = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                number += buffer[i] * BigInteger.Pow(255, i);
            }
            return number;
        }

    }
}