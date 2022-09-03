using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JavaTranslate.Parsing;

public ref struct SpanReader {
    public ReadOnlySpan<byte> Data;
    public int Position;
    public int Misalignment => 4 - Position % 4;
    public SpanReader(ReadOnlySpan<byte> data, int start) {
        Data = data;
        Position = start;
    }
    public T Read<T>() where T : unmanaged {
        T result = MemoryMarshal.Read<T>(Data[Position..]);
        Position += Unsafe.SizeOf<T>();
        return result;
    }

    public short ReadI16() {
        short result = BinaryPrimitives.ReadInt16BigEndian(Data[Position..]);
        Position += 2;
        return result;
    }
    public ushort ReadU16() {
        ushort result = BinaryPrimitives.ReadUInt16BigEndian(Data[Position..]);
        Position += 2;
        return result;
    }
    public uint ReadU32() {
        uint result = BinaryPrimitives.ReadUInt32BigEndian(Data[Position..]);
        Position += 4;
        return result;
    }
    public int ReadI32() {
        int result = BinaryPrimitives.ReadInt32BigEndian(Data[Position..]);
        Position += 4;
        return result;
    }
    public void Realign() {
        Position += Misalignment;
    }
    public ReadOnlySpan<byte> ReadData(int count) {
        ReadOnlySpan<byte> final = Data[Position..(Position + count)];
        Position += count;
        return final;
    }
}