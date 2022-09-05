using dnlib.DotNet;
using dnlib.DotNet.Emit;
using JavaTranslate.Parsing;
using JavaTranslate.Parsing.Attributes;

namespace JavaTranslate.Translation;

public class LocalManager {
    private LocalVariableTableAttribute.LocalVariable[] Locals;

    private Dictionary<int, List<LocalVariableTableAttribute.LocalVariable>> LocalIndexMap =
        new Dictionary<int, List<LocalVariableTableAttribute.LocalVariable>>();

    private MethodDef Method;
    private int ArgumentCount => Method.Parameters.Count;

    public LocalManager(Translator translator, MethodDef methodDef, IAttributeContainer method) {
        Locals = method.GetAttribute<LocalVariableTableAttribute>()?.Locals ??
                 Array.Empty<LocalVariableTableAttribute.LocalVariable>();
        Method = methodDef;
        Dictionary<int, string> maxes = new Dictionary<int, string>();
        ushort nextIndex = (ushort) (Locals.MaxBy(x => x.Index)?.Index + 1 ?? 0);
        foreach (LocalVariableTableAttribute.LocalVariable local in Locals) {
            if (!LocalIndexMap.TryGetValue(local.Index, out List<LocalVariableTableAttribute.LocalVariable>? locals)) {
                locals = LocalIndexMap[local.Index] = new List<LocalVariableTableAttribute.LocalVariable>();
            }
            locals.Add(local);
            if (maxes.TryGetValue(local.Index, out string? type) && type != local.Type) local.Index = nextIndex++;
            maxes[local.Index] = local.Type;
            Method.Body.Variables.Add(new Local(translator.ComponentSignature(local.Type, false), local.Name));
        }
    }

    public IEnumerable<Instruction> Store(ushort index, int offset) {
        LocalVariableTableAttribute.LocalVariable localVariable;
        if (index < ArgumentCount) {
            localVariable = LocalIndexMap[index].Single(x => x.InRange(offset));
            yield return Instruction.Create(OpCodes.Starg, Method.Parameters[localVariable.Index]);
            yield break;
        }

        localVariable = LocalIndexMap[index].Single(x => x.InRange(offset));
        yield return Instruction.Create(OpCodes.Stloc, Method.Body.Variables[localVariable.Index - ArgumentCount]);
    }

    public IEnumerable<Instruction> Load(ushort index, int offset) {
        LocalVariableTableAttribute.LocalVariable localVariable;
        if (index < ArgumentCount) {

            localVariable = LocalIndexMap[index].Single(x => x.InRange(offset));
            yield return Instruction.Create(OpCodes.Ldarg, Method.Parameters[localVariable.Index]);
            yield break;
        }

        localVariable = LocalIndexMap[index].Single(x => x.InRange(offset));
        yield return Instruction.Create(OpCodes.Ldloc, Method.Body.Variables[localVariable.Index - ArgumentCount]);
    }
}