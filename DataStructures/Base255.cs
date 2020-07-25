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
    public class Base255
    {
        public BigInteger Number { get; }
        public byte[] byteArr { get; }  //Inverted buffer

        ///<summary>Stores the number and stores its conversion to base 255.</summary>
        public Base255(BigInteger number)
        {
            int size = (int)Math.Ceiling(BigInteger.Log(number, 255));
            this.byteArr = new byte[size];
            Number = number;
            var tempNumber = number;

            for (int i = 0; i < size; i++)
            {
                byteArr[i] = (byte)(tempNumber % 255);
                tempNumber /= 255;
            }
        }
        ///<summary>Stores the buffer and stores its conversion to BigInteger.</summary>
        public Base255(byte[] buffer)
        {
            this.byteArr = new byte[buffer.Length];
            buffer.CopyTo(byteArr, 0);
            Number = 0;

            for (int i = 0; i < this.byteArr.Length; i++)
            {
                Number += this.byteArr[i] * BigInteger.Pow(255, i);
            }
        }

        public override string ToString()
        {
            return Number.ToString();
        }
    }
}