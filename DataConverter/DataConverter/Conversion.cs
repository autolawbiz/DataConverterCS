using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jp.autolawbiz.DataConverter
{
    public class Conversion
    {
        protected static void SkipReadByte(int counts, BinaryReader rebr)
        {
            for (int i = 0; i < counts; i++)
            {
                rebr.ReadByte();
            }
        }

        protected static void SkipReadInt(int counts, BinaryReader rebr)
        {
            for (int i = 0; i < counts; i++)
            {
                rebr.ReadInt32();
            }
        }

        protected static byte[] Reverse2ByteShort(byte[] recbytes)
        {
            byte[] bytes = { recbytes[1], recbytes[0] };
            return bytes;
        }

        protected static byte[] Reverse4ByteInt(byte[] recbytes)
        {
            byte[] bytes = { recbytes[3], recbytes[2], recbytes[1], recbytes[0] };
            return bytes;
        }

        protected static byte[] Reverse8ByteDouble(byte[] recbytes)
        {
            byte[] bytes = { recbytes[7], recbytes[6], recbytes[5], recbytes[4], recbytes[3], recbytes[2], recbytes[1], recbytes[0] };
            return bytes;
        }

        protected static int Byte2Int(byte[] recbytes)
        {
            return BitConverter.ToInt32(recbytes, 0);
            //return ByteBuffer.wrap(recbytes).getInt();
        }

        protected static short Byte2Short(byte[] recbytes)
        {
            return BitConverter.ToInt16(recbytes, 0);
            //return ByteBuffer.wrap(recbytes).getShort();
        }

        protected static double Byte2Double(byte[] recbytes)
        {
            return BitConverter.ToDouble(recbytes, 0);
            //return ByteBuffer.wrap(recbytes).getDouble();
        }
    }
}
