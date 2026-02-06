using System;
using System.Reflection;
using System.Text;

namespace LootWithFriends
{
    public class PooledBinaryWriterCompat : PooledBinaryWriter
    {
        public PooledBinaryWriterCompat() : base()
        {
        }

        // Write an int directly using the internal buffer
        public void WriteIntCompat(int value)
        {
            // Use the protected 'buffer' array
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);

            // Protected 'OutStream' is accessible from subclass
            OutStream.Write(buffer, 0, 4);
        }

        // Optional: write string as UTF8 with length prefix
        public void WriteStringCompat(string value)
        {
            if (value == null) value = "";
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteIntCompat(bytes.Length); // write length prefix
            OutStream.Write(bytes, 0, bytes.Length);
        }
    }
    
    public class PooledBinaryWriterCompatWrapper : PooledBinaryWriterCompat
    {
        public PooledBinaryWriterCompatWrapper(PooledBinaryWriter baseWriter)
        {
            // Use reflection to set the protected OutStream
            var outStreamField = typeof(PooledBinaryWriter).GetField("OutStream", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (outStreamField == null)
                throw new Exception("Cannot find OutStream field");

            outStreamField.SetValue(this, baseWriter.BaseStream);
        }
    }

}