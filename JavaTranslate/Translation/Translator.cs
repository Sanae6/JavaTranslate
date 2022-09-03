using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using JavaTranslate.Parsing;
using JavaTranslate.Parsing.Attributes;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using Object = java.lang.Object;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace JavaTranslate.Translation;

public class Translator {
    private readonly ModuleDefUser Module = new ModuleDefUser("JavaProgram");
    private readonly List<ClassFile> Files = new List<ClassFile>();
    private readonly List<(TypeDefUser, ClassFile)> Classes = new List<(TypeDefUser, ClassFile)>();
    private static readonly Dictionary<string, Type> JavaTypes = new Dictionary<string, Type>();
    static Translator() {
        Type[] allTypes = typeof(Object).Assembly.GetTypes();

        static string GetTypeName(Type type) {
            return type.IsNested
                ? $"{GetTypeName(type)}${type.Name.Remove(type.DeclaringType!.Name.Length + 1)}"
                : type.Name;
        }
        foreach (Type type in allTypes) {
            JavaTypes.Add(
                (type.Namespace == null ? string.Empty : type.Namespace?.Replace('.', '/') + "/") + GetTypeName(type),
                type);
        }
    }

    public void AddClassFile(ClassFile classFile) {
        Files.Add(classFile);
    }

    private static void TranslateClass(ClassFile file, TypeDef type) {
        if ((file.Flags & AccessFlags.Public) != 0)
            type.Attributes |= TypeAttributes.Public;
        if ((file.Flags & AccessFlags.Private) != 0)
            type.Attributes |= TypeAttributes.NestedPrivate;
        if ((file.Flags & AccessFlags.Static) != 0)
            type.Attributes |= TypeAttributes.Abstract | TypeAttributes.Sealed;
        if ((file.Flags & AccessFlags.Abstract) != 0)
            type.Attributes |= TypeAttributes.Abstract;
    }

    private void TranslateMethod(ClassFile file, MethodDefUser methodDef, Method method) {
        // todo! stack opcodes, store and duplicate lists to permit reordering of translated code if needed
        // not sure how java stack works, hoping it's not required for anything and it's 1:1 translation

        if (methodDef.IsIL) {
            CilBody body = methodDef.Body = new CilBody();
            body.KeepOldMaxStack = true;
            CodeAttribute code = method.GetAttribute<CodeAttribute>()
                                 ?? throw new NullReferenceException("Couldn't find code attribute!");
            foreach (IOpcode op in code.Code) {
                switch (op.Operation) {
                    case Operation.IntLoadVar0:
                    case Operation.LongLoadVar0:
                    case Operation.FloatLoadVar0:
                    case Operation.DoubleLoadVar0:
                    case Operation.RefLoadVar0:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        break;
                    case Operation.IntLoadVar1:
                    case Operation.LongLoadVar1:
                    case Operation.FloatLoadVar1:
                    case Operation.DoubleLoadVar1:
                    case Operation.RefLoadVar1:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                        break;
                    case Operation.IntLoadVar2:
                    case Operation.LongLoadVar2:
                    case Operation.FloatLoadVar2:
                    case Operation.DoubleLoadVar2:
                    case Operation.RefLoadVar2:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
                        break;
                    case Operation.IntLoadVar3:
                    case Operation.LongLoadVar3:
                    case Operation.FloatLoadVar3:
                    case Operation.DoubleLoadVar3:
                    case Operation.RefLoadVar3:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
                        break;
                    case Operation.IntAdd:
                    case Operation.LongAdd:
                    case Operation.FloatAdd:
                    case Operation.DoubleAdd:
                        body.Instructions.Add(Instruction.Create(OpCodes.Add));
                        break;
                    case Operation.IntSub:
                    case Operation.LongSub:
                    case Operation.FloatSub:
                    case Operation.DoubleSub:
                        body.Instructions.Add(Instruction.Create(OpCodes.Sub));
                        break;
                    case Operation.IntMul:
                    case Operation.LongMul:
                    case Operation.FloatMul:
                    case Operation.DoubleMul:
                        body.Instructions.Add(Instruction.Create(OpCodes.Mul));
                        break;
                    case Operation.IntDiv:
                    case Operation.LongDiv:
                    case Operation.FloatDiv:
                    case Operation.DoubleDiv:
                        body.Instructions.Add(Instruction.Create(OpCodes.Div));
                        break;
                    case Operation.IntRem:
                    case Operation.LongRem:
                    case Operation.FloatRem:
                    case Operation.DoubleRem:
                        body.Instructions.Add(Instruction.Create(OpCodes.Rem));
                        break;
                    case Operation.IntNeg:
                    case Operation.LongNeg:
                    case Operation.FloatNeg:
                    case Operation.DoubleNeg:
                        body.Instructions.Add(Instruction.Create(OpCodes.Neg));
                        break;
                    case Operation.LoadConst: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        object? constant = file.GetConstant((ushort) opcode.Value);
                        switch (constant) {
                            case int i:
                                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                                break;
                            case float f:
                                body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, f));
                                break;
                            case string s:
                                body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, s));
                                break;
                        }
                        break;
                    }
                    case Operation.Return or Operation.IntReturn or Operation.LongReturn or Operation.FloatReturn or Operation.DoubleReturn or Operation.RefReturn:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                        break;
                    case Operation.InvokeSpecial or Operation.InvokeStatic: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        ClassFile.RefConstant constant = file.GetConstant<ClassFile.RefConstant>((ushort) opcode.Value);
                        ClassFile.NameAndType nameAndType =
                            file.GetConstant<ClassFile.NameAndType>(constant.NameAndTypeIndex);
                        string className = file.GetStringConstant(file.GetConstant<ushort>(constant.ClassIndex))
                                           ?? throw new Exception();
                        string name = file.GetStringConstant(nameAndType.NameIndex) ?? throw new Exception();
                        string type = file.GetStringConstant(nameAndType.TypeIndex) ?? throw new Exception();
                        MethodSig methodSignature = MethodSignature(false, type);
                        if (JavaTypes.TryGetValue(className, out Type? javaType)) {
                            IEnumerable<MethodBase> methods = javaType.GetMethods(BindingFlags.Instance
                                    | BindingFlags.Static
                                    | BindingFlags.Public
                                    | BindingFlags.NonPublic)
                                .Concat(javaType.GetConstructors().Cast<MethodBase>())
                                .Where(x => x.Name == TranslateSpecialName(name));
                            IMethod methodRef = Module.Import(methods.First(x =>
                                Module.Import(x).MethodSig.ToString() == methodSignature.ToString()));
                            body.Instructions.Add(Instruction.Create(OpCodes.Call, methodRef));
                        } else {
                            (TypeDefUser? otherTypeDef, ClassFile? _) = Classes.First(x => x.Item2.Name == className);
                            body.Instructions.Add(Instruction.Create(OpCodes.Call, otherTypeDef.Methods.First(x =>
                                x.Name == TranslateSpecialName(name)
                                && x.MethodSig.ToString() == methodSignature.ToString())));
                        }
                        break;
                    }
                    case Operation.InvokeVirtual or Operation.InvokeInterface: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        ClassFile.RefConstant constant = file.GetConstant<ClassFile.RefConstant>((ushort) opcode.Value);
                        ClassFile.NameAndType nameAndType =
                            file.GetConstant<ClassFile.NameAndType>(constant.NameAndTypeIndex);
                        string className = file.GetStringConstant(file.GetConstant<ushort>(constant.ClassIndex))
                                           ?? throw new Exception();
                        string name = file.GetStringConstant(nameAndType.NameIndex) ?? throw new Exception();
                        string type = file.GetStringConstant(nameAndType.TypeIndex) ?? throw new Exception();
                        MethodSig methodSignature = MethodSignature(false, type);
                        if (JavaTypes.TryGetValue(className, out Type? javaType)) {
                            IEnumerable<MethodBase> methods = javaType.GetMethods(BindingFlags.Static
                                    | BindingFlags.Instance
                                    | BindingFlags.Public
                                    | BindingFlags.NonPublic)
                                .Where(x => x.Name == TranslateSpecialName(name));
                            IMethod methodRef = Module.Import(methods.First(x =>
                                Module.Import(x).MethodSig.ToString() == methodSignature.ToString()));
                            body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, methodRef));
                        } else {
                            (TypeDefUser? otherTypeDef, ClassFile? _) = Classes.First(x => x.Item2.Name == className);
                            body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, otherTypeDef.Methods.First(x =>
                                x.Name == TranslateSpecialName(name)
                                && x.MethodSig.ToString() == methodSignature.ToString())));
                        }
                        break;
                    }
                    case Operation.GetField or Operation.GetStatic or Operation.PutField or Operation.PutStatic: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        ClassFile.RefConstant constant = file.GetConstant<ClassFile.RefConstant>((ushort) opcode.Value);
                        ClassFile.NameAndType nameAndType =
                            file.GetConstant<ClassFile.NameAndType>(constant.NameAndTypeIndex);
                        string className = file.GetStringConstant(file.GetConstant<ushort>(constant.ClassIndex))
                                           ?? throw new Exception();
                        string name = file.GetStringConstant(nameAndType.NameIndex) ?? throw new Exception();
                        string type = file.GetStringConstant(nameAndType.TypeIndex) ?? throw new Exception();
                        int pos = 0;
                        TypeSig fieldSignature = ComponentSignature(type, false, ref pos);
                        OpCode finalOpCode = op.Operation switch {
                            Operation.GetField => OpCodes.Ldfld,
                            Operation.GetStatic => OpCodes.Ldsfld,
                            Operation.PutField => OpCodes.Stfld,
                            Operation.PutStatic => OpCodes.Stsfld,
                            _ => throw new ArgumentOutOfRangeException() // impossible
                        };
                        if (JavaTypes.TryGetValue(className, out Type? javaType)) {
                            IEnumerable<FieldInfo> fields = javaType.GetFields(BindingFlags.Static
                                                                               | BindingFlags.Instance
                                                                               | BindingFlags.Public
                                                                               | BindingFlags.NonPublic)
                                .Where(x => x.Name == TranslateSpecialName(name));
                            IField fieldRef = Module.Import(fields.First(x =>
                                Module.Import(x).FieldSig.ToString() == fieldSignature.ToString()));
                            body.Instructions.Add(Instruction.Create(finalOpCode, fieldRef));
                        } else {
                            (TypeDefUser? otherTypeDef, ClassFile? _) = Classes.First(x => x.Item2.Name == className);
                            body.Instructions.Add(Instruction.Create(finalOpCode, otherTypeDef.Fields.First(x =>
                                x.Name == TranslateSpecialName(name)
                                && x.FieldSig.ToString() == fieldSignature.ToString())));
                        }
                        break;
                    }
                }
            }
        }
    }

    private static string TranslateSpecialName(string name) => name switch {
        "<init>" => ".ctor",
        "<clinit>" => ".cctor",
        _ => name
    };

    private MethodSig MethodSignature(bool isStatic, string signature) {
        string args = signature[1..signature.IndexOf(")", StringComparison.Ordinal)];
        int pos = 0;
        List<TypeSig> argSignatures = new List<TypeSig>();
        while (pos < args.Length) argSignatures.Add(ComponentSignature(args, false, ref pos));
        pos = 0;
        return new MethodSig(isStatic ? CallingConvention.Default : CallingConvention.HasThis, 0, ComponentSignature(
            signature[
                (args.Length + 2)..], true, ref pos), argSignatures);
    }

    private void TranslateField(FieldDef fieldDef, Field field) {
        if (fieldDef.Name.String != field.Name)
            fieldDef.Attributes |= FieldAttributes.SpecialName | FieldAttributes.RTSpecialName;
        if ((field.Flags & AccessFlags.Public) != 0)
            fieldDef.Attributes |= FieldAttributes.Public;
        if ((field.Flags & AccessFlags.Private) != 0)
            fieldDef.Attributes |= FieldAttributes.Private;
        if ((field.Flags & AccessFlags.Final) != 0)
            fieldDef.Attributes |= FieldAttributes.InitOnly;
        if ((field.Flags & AccessFlags.Static) != 0)
            fieldDef.Attributes |= FieldAttributes.Static;
        int pos = 0;
        fieldDef.Signature = new FieldSig(ComponentSignature(field.Type, false, ref pos));
    }

    public TypeSig ComponentSignature(string type, bool isReturnType, ref int pos) {
        switch (type[pos++]) {
            case 'B':
                return Module.ImportAsTypeSig(typeof(sbyte));
            case 'C':
                return Module.ImportAsTypeSig(typeof(char));
            case 'D':
                return Module.ImportAsTypeSig(typeof(double));
            case 'F':
                return Module.ImportAsTypeSig(typeof(float));
            case 'I':
                return Module.ImportAsTypeSig(typeof(int));
            case 'J':
                return Module.ImportAsTypeSig(typeof(long));
            case 'S':
                return Module.ImportAsTypeSig(typeof(short));
            case 'V' when isReturnType:
                return Module.ImportAsTypeSig(typeof(void));
            case 'Z':
                return Module.ImportAsTypeSig(typeof(bool));
            case 'L': {
                string typeName = type[pos..type.IndexOf(';', pos)];
                pos += typeName.Length + 1;
                if (JavaTypes.TryGetValue(typeName, out Type? javaType))
                    return Module.ImportAsTypeSig(javaType);
                if (Classes.Any(tuple => tuple.Item2.Name == typeName))
                    return Classes.First(tuple => tuple.Item2.Name == typeName).Item1.ToTypeSig();
                throw new KeyNotFoundException($"Could not find type {typeName}");
            }
            case '[':
                return new ArraySig(ComponentSignature(type, isReturnType, ref pos));
        }
        throw new InvalidDataException($"Bad type descriptor {type[pos - 1]}");
    }

    public ModuleDefUser Translate() {
        Dictionary<MethodDefUser, Method> methods = new Dictionary<MethodDefUser, Method>();

        foreach (ClassFile file in Files) {
            TypeDefUser type = file.Name.LastIndexOf('/') < 0
                ? new TypeDefUser(file.Name.Replace('$', '.'))
                : new TypeDefUser(file.Name[..file.Name.LastIndexOf('/')].Replace('/', '.'),
                    file.Name[(file.Name.LastIndexOf('/') + 1)..].Replace('$', '.'));

            foreach (Method method in file.Methods) {
                MethodDefUser methodDef = new MethodDefUser(TranslateSpecialName(method.Name),
                    MethodSignature((method.Flags & AccessFlags.Static) != 0, method.Type));

                if (methodDef.Name.String != method.Name)
                    methodDef.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                if ((method.Flags & AccessFlags.Public) != 0)
                    methodDef.Attributes |= MethodAttributes.Public;
                if ((method.Flags & AccessFlags.Private) != 0)
                    methodDef.Attributes |= MethodAttributes.Private;
                if ((method.Flags & AccessFlags.Final) != 0)
                    methodDef.Attributes |= MethodAttributes.Final;
                if ((method.Flags & AccessFlags.Abstract) != 0)
                    methodDef.Attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual;
                if ((method.Flags & AccessFlags.Static) != 0)
                    methodDef.Attributes |= MethodAttributes.Static;
                if ((method.Flags & AccessFlags.Native) != 0)
                    methodDef.ImplAttributes |= MethodImplAttributes.Native;

                type.Methods.Add(methodDef);
                methods.Add(methodDef, method);
            }

            foreach (Field field in file.Fields) {
                FieldDefUser fieldDef = new FieldDefUser(field.Name);
                TranslateField(fieldDef, field);
                type.Fields.Add(fieldDef);
            }

            Classes.Add((type, file));
        }

        // second pass
        foreach ((TypeDefUser type, ClassFile file) in Classes) {
            if (type.Name.Contains(".")) {
                TypeDef parent = Module.Types.First(x =>
                    x.Name.String == type.Name.String[..type.Name.LastIndexOf('.')] && x.Namespace == type.Namespace);
                parent.NestedTypes.Add(type);
            } else
                Module.Types.Add(type);

            TranslateClass(file, type);

            foreach (MethodDefUser methodDef in type.Methods.Cast<MethodDefUser>()) {
                Method method = methods[methodDef];
                TranslateMethod(file, methodDef, method);
            }
        }

        return Module;
    }
}