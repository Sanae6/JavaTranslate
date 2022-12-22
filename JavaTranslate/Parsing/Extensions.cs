using dnlib.DotNet.Emit;

namespace JavaTranslate.Parsing; 

internal static class Extensions {
    internal static void AddRange(this IList<Instruction> list, IEnumerable<Instruction> instructions) {
        foreach (Instruction inst in instructions) {
            list.Add(inst);
        }
    }
}