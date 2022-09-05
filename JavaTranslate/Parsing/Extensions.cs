using System.Buffers.Binary;
using dnlib.DotNet.Emit;

namespace JavaTranslate.Parsing; 

internal static class Extensions {
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

    internal static void AddRange(this IList<Instruction> list, IEnumerable<Instruction> instructions) {
        foreach (Instruction inst in instructions) {
            list.Add(inst);
        }
    }
}