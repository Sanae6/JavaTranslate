using System.Buffers.Binary;

namespace JavaTranslate.Parsing; 

internal static class ParsingExtensions {
    internal static ushort ReadU16(this ReadOnlySpan<byte> data, ref int pos) {
        ushort result = BinaryPrimitives.ReadUInt16BigEndian(data[pos..]);
        pos += 2;
        return result;
    }
    internal static uint ReadU32(this ReadOnlySpan<byte> data, ref int pos) {
        uint result = BinaryPrimitives.ReadUInt32BigEndian(data[pos..]);
        pos += 4;
        return result;
    }
    internal static int ReadI32(this ReadOnlySpan<byte> data, ref int pos) {
        int result = BinaryPrimitives.ReadInt32BigEndian(data[pos..]);
        pos += 4;
        return result;
    }
}