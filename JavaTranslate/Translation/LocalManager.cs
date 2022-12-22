using dnlib.DotNet;
using dnlib.DotNet.Emit;
using JavaTranslate.Parsing;
using JavaTranslate.Parsing.Attributes;

namespace JavaTranslate.Translation;

public class LocalManager {
    private readonly LocalVariableTableAttribute.LocalVariable[] Locals;

    private readonly Dictionary<int, List<LocalVariableTableAttribute.LocalVariable>> LocalIndexMap =
        new Dictionary<int, List<LocalVariableTableAttribute.LocalVariable>>();

    private readonly Dictionary<LocalVariableTableAttribute.LocalVariable, Local> LocalLocalMap =
        new Dictionary<LocalVariableTableAttribute.LocalVariable, Local>();

    private readonly MethodDef Method;
    private Translator Translator;
    private int ArgumentCount => Method.Parameters.Count;

    public LocalManager(Translator translator, MethodDef methodDef, IAttributeContainer method) {
        Locals = method.GetAttribute<CodeAttribute>()?.GetAttribute<LocalVariableTableAttribute>()?.Locals ??
                 Array.Empty<LocalVariableTableAttribute.LocalVariable>();
        Method = methodDef;
        Translator = translator;

        Dictionary<int, string> maxes = new Dictionary<int, string>();
        ushort nextIndex = (ushort) (Locals.MaxBy(x => x.Index)?.Index + 1 ?? 0);
        foreach (LocalVariableTableAttribute.LocalVariable local in Locals) {
            if (!LocalIndexMap.TryGetValue(local.Index, out List<LocalVariableTableAttribute.LocalVariable>? locals)) {
                locals = LocalIndexMap[local.Index] = new List<LocalVariableTableAttribute.LocalVariable>();
            }

            locals.Add(local);
            if (maxes.TryGetValue(local.Index, out string? type) && type != local.Type) local.Index = nextIndex++;
            maxes[local.Index] = local.Type;
            Local localMeta =
                Method.Body.Variables.Add(new Local(translator.ComponentSignature(local.Type, false), local.Name));
            LocalLocalMap[local] = localMeta;
        }
    }

    public IEnumerable<Instruction> Store(ushort index, int offset, Type type) {
        if (index < ArgumentCount) {
            yield return Instruction.Create(OpCodes.Starg, Method.Parameters[index]);
            yield break;
        }

        yield return Instruction.Create(OpCodes.Stloc, GetLocal(index, offset, type));
    }

    public IEnumerable<Instruction> Load(ushort index, int offset, Type type) {
        if (index < ArgumentCount) {
            yield return Instruction.Create(OpCodes.Ldarg, Method.Parameters[index]);
            yield break;
        }

        yield return Instruction.Create(OpCodes.Ldloc, GetLocal(index, offset, type));
    }

    private Local GetLocal(ushort index, int offset, Type type) {
        if (!LocalIndexMap.TryGetValue(index, out List<LocalVariableTableAttribute.LocalVariable>? localList))
            LocalIndexMap.Add(index, localList = new List<LocalVariableTableAttribute.LocalVariable>());

        LocalVariableTableAttribute.LocalVariable? localVariable = localList.SingleOrDefault(x =>
            x.InRange(offset) && type.Name == Translator.ComponentSignature(x.Type, false).TypeName
        );

        if (localVariable == null) {
            localVariable = new LocalVariableTableAttribute.LocalVariable {
                Index = index,
                Length = ushort.MaxValue
            };
            Local local = Method.Body.Variables.Add(new Local(Method.Module.ImportAsTypeSig(type)));
            LocalLocalMap.Add(localVariable, local);
            return local;
        }
        
        return LocalLocalMap[localVariable];
    }
}