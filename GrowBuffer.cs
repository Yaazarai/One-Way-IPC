using System.Runtime.CompilerServices;
using StringBuilder = System.Text.StringBuilder;
using BitConverter = System.BitConverter;
using Encoding = System.Text.Encoding;
using Array = System.Array;

namespace InterProcessCommunication {
    public class GrowBuffer {
        private enum BufferTypeSize {
            None = 0, Bool = 1, Byte = 1, Int32 = 4,
            Single = 4, Double = 8, String = -1, Bytes = -1
        }
        
        private const byte bTrue = 1, bFalse = 0;
        private const int minBlockSize = 8;
        private int iterator, length, blockSize;
        private byte[] memory;

        public int Iterator { get { return iterator; } }
        public int Length { get { return length; } }
        public int BlockSize { get { return blockSize; } }

        public GrowBuffer(int blockSize) {
            this.blockSize = (blockSize < minBlockSize) ? minBlockSize : blockSize;
            memory = new byte[blockSize];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FastAlignment(int iterator, int alignment) {
            return ((iterator + (alignment - 1)) & ~(alignment - 1));
        }

        public void Grow(int size) {
            if(iterator + (int)size < length)
                return;

            int newSize = FastAlignment(length + (int)size + blockSize, blockSize);
            Array.Resize<byte>(ref memory, newSize);
            length = newSize;
        }

        public byte[] GetBuffer() {
            return memory;
        }

        public void Write(bool value) {
            Grow((int)BufferTypeSize.Bool);
            memory[iterator++] = (value) ? bTrue : bFalse;
        }

        public void Write(byte value) {
            Grow((int)BufferTypeSize.Byte);
            memory[iterator++] = value;
        }

        public void Write(int value) {
            Grow((int)BufferTypeSize.Int32);
            memory[iterator++] = (byte)value;
            memory[iterator++] = (byte)(value >> 8);
            memory[iterator++] = (byte)(value >> 16);
            memory[iterator++] = (byte)(value >> 24);
        }

        public void Write(float value) {
            byte[] bytes = BitConverter.GetBytes(value);
            Grow(bytes.Length);

            for(int i = 0; i < bytes.Length; i++)
                memory[iterator++] = bytes[i];
        }

        public void Write(double value) {
            byte[] bytes = BitConverter.GetBytes(value);
            Grow(bytes.Length);

            for(int i = 0; i < bytes.Length; i++)
                memory[iterator++] = bytes[i];
        }

        public void Write(string value) {
            Grow(value.Length + 1);

            byte[] bytes = Encoding.ASCII.GetBytes((string)value);
            for(int i = 0; i < bytes.Length; i++)
                memory[iterator++] = bytes[i];
            memory[iterator++] = 0;
        }

        public void Write(byte[] value) {
            Grow(value.Length);

            for(int i = 0; i < value.Length; i++)
                memory[iterator++] = value[i];
        }

        public void Read(out bool value) {
            value = memory[iterator++] >= 0;
        }

        public void Read(out byte value) {
            value = memory[iterator++];
        }

        public void Read(out int value) {
            value = BitConverter.ToInt32(memory, iterator);
        }

        public void Read(out string value) {
            StringBuilder str = new StringBuilder();

            for(char c = '\0'; iterator < length;) {
                c = (char)memory[iterator++];
                if(c == '\0' || iterator == length)
                    break;
                str.Append(c);
            }

            value = str.ToString();
        }

        public void Read(out byte[] value, int length) {
            value = new byte[length];

            for(int i = 0; i < length; i++) {
                value[i] = memory[iterator++];
                if(iterator == length)
                    break;
            }
        }
    }
}
