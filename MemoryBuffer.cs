using System.IO;
using System;

namespace InterProcessCommunication {
    public class MemoryBuffer {
        public MemoryStream Memory { get; private set; } = null;
        public long Length { get { return Memory.Length; } }

        public MemoryBuffer() {
            Memory = new MemoryStream();
        }

        public MemoryBuffer(int capacity) {
            Memory = new MemoryStream(capacity);
        }

        public void Write(bool value) {
            Memory.WriteByte(Convert.ToByte(value));
        }

        public void Write(byte value) {
            Memory.WriteByte(value);
        }

        public void Write(int value) {
            byte[] bytes = BitConverter.GetBytes(value);
            Memory.Write(bytes, 0, bytes.Length);
        }

        public void Write(float value) {
            byte[] bytes = BitConverter.GetBytes(value);
            Memory.Write(bytes, 0, bytes.Length);
        }

        public void Write(double value) {
            byte[] bytes = BitConverter.GetBytes(value);
            Memory.Write(bytes, 0, bytes.Length);
        }

        public void Write(string value) {
            foreach(char c in value)
                Memory.Write(BitConverter.GetBytes(c), 0, sizeof(char));
            Memory.Write(BitConverter.GetBytes(char.MinValue), 0, sizeof(char));
        }

        public void Write(byte[] value) =>
            Memory.Write(value, 0, value.Length);

        public void Read(out bool value) =>
            value = Convert.ToBoolean(Memory.ReadByte());

        public void Read(out byte value) =>
            value = Convert.ToByte(Memory.ReadByte());

        public void Read(out int value) {
            byte[] bytes = new byte[sizeof(int)];
            Memory.Read(bytes, 0, bytes.Length);
            value = Convert.ToInt32(bytes);
        }

        public void Read(out string value) {
            value = string.Empty;
            byte[] chr = new byte[sizeof(char)];
            char? c = null;

            while(c != char.MinValue) {
                Memory.Read(chr, 0, chr.Length);
                c = BitConverter.ToChar(chr, 0);
                value += c;
            }
        }

        public void Read(out byte[] value, int length) {
            value = new byte[length];
            Memory.Read(value, 0, length);
        }
    }
}
