using System.Reflection;
using System.Runtime.CompilerServices;
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

public sealed class Translator {
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

        if ((method.Flags & AccessFlags.Native) == 0) {
            CilBody body = methodDef.Body = new CilBody();
            body.KeepOldMaxStack = true;
            CodeAttribute code = method.GetAttribute<CodeAttribute>()
                                 ?? throw new NullReferenceException("Couldn't find code attribute!");

            LocalManager locals = new LocalManager(this, methodDef, method);

            foreach (IOpcode op in code.Code) {
                switch (op.Operation) {
                    case Operation.IntLoadVar0:
                    case Operation.LongLoadVar0:
                    case Operation.FloatLoadVar0:
                    case Operation.DoubleLoadVar0:
                    case Operation.RefLoadVar0:
                        body.Instructions.AddRange(locals.Load(0, op.Offset));
                        break;
                    case Operation.IntLoadVar1:
                    case Operation.LongLoadVar1:
                    case Operation.FloatLoadVar1:
                    case Operation.DoubleLoadVar1:
                    case Operation.RefLoadVar1:
                        body.Instructions.AddRange(locals.Load(1, op.Offset));
                        break;
                    case Operation.IntLoadVar2:
                    case Operation.LongLoadVar2:
                    case Operation.FloatLoadVar2:
                    case Operation.DoubleLoadVar2:
                    case Operation.RefLoadVar2:
                        body.Instructions.AddRange(locals.Load(2, op.Offset));
                        break;
                    case Operation.IntLoadVar3:
                    case Operation.LongLoadVar3:
                    case Operation.FloatLoadVar3:
                    case Operation.DoubleLoadVar3:
                    case Operation.RefLoadVar3:
                        body.Instructions.AddRange(locals.Load(3, op.Offset));
                        break;
                    case Operation.IntStoreVar0:
                    case Operation.LongStoreVar0:
                    case Operation.FloatStoreVar0:
                    case Operation.DoubleStoreVar0:
                    case Operation.RefStoreVar0:
                        body.Instructions.AddRange(locals.Store(0, op.Offset));
                        break;
                    case Operation.IntStoreVar1:
                    case Operation.LongStoreVar1:
                    case Operation.FloatStoreVar1:
                    case Operation.DoubleStoreVar1:
                    case Operation.RefStoreVar1:
                        body.Instructions.AddRange(locals.Store(1, op.Offset));
                        break;
                    case Operation.IntStoreVar2:
                    case Operation.LongStoreVar2:
                    case Operation.FloatStoreVar2:
                    case Operation.DoubleStoreVar2:
                    case Operation.RefStoreVar2:
                        body.Instructions.AddRange(locals.Store(2, op.Offset));
                        break;
                    case Operation.IntStoreVar3:
                    case Operation.LongStoreVar3:
                    case Operation.FloatStoreVar3:
                    case Operation.DoubleStoreVar3:
                    case Operation.RefStoreVar3:
                        body.Instructions.AddRange(locals.Store(3, op.Offset));
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
                                body.Instructions.Add(Instruction.Create(OpCodes.Call, Module.Import(
                                    typeof(java.lang.String).GetMethod(nameof(java.lang.String.FromNetString)))));
                                break;
                        }

                        break;
                    }
                    case Operation.IntConstM1:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_M1));
                        break;
                    case Operation.IntConst0:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                        break;
                    case Operation.IntConst1:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                        break;
                    case Operation.IntConst2:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_2));
                        break;
                    case Operation.IntConst3:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_3));
                        break;
                    case Operation.IntConst4:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_4));
                        break;
                    case Operation.IntConst5:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_5));
                        break;
                    case Operation.LongConst0:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I8, 0));
                        break;
                    case Operation.LongConst1:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I8, 1));
                        break;
                    case Operation.FloatConst0:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, 0.0f));
                        break;
                    case Operation.FloatConst1:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, 1.0f));
                        break;
                    case Operation.FloatConst2:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, 2.0f));
                        break;
                    case Operation.DoubleConst0:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, 0.0));
                        break;
                    case Operation.DoubleConst1:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R8, 1.0));
                        break;
                    case Operation.BytePush or Operation.ShortPush: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, opcode.Value));
                        break;
                    }
                    case Operation.Return or Operation.IntReturn or Operation.LongReturn or Operation.FloatReturn
                        or Operation.DoubleReturn or Operation.RefReturn:
                        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                        break;
                    case Operation.New: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        string className = file.GetClassName((ushort) opcode.Value)!;
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, Resolve(className)));
                        body.Instructions.Add(Instruction.Create(OpCodes.Call, Module.Import(
                            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)))));
                        body.Instructions.Add(Instruction.Create(OpCodes.Call, Module.Import(
                            typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject)))));
                        break;
                    }
                    case Operation.Dup:
                        body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                        break;
                    case Operation.Pop:
                        body.Instructions.Add(Instruction.Create(OpCodes.Pop));
                        break;
                    case Operation.InvokeSpecial or Operation.InvokeStatic or Operation.InvokeInterface
                        or Operation.InvokeVirtual: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        ClassFile.RefConstant constant = file.GetConstant<ClassFile.RefConstant>((ushort) opcode.Value);
                        ClassFile.NameAndType nameAndType =
                            file.GetConstant<ClassFile.NameAndType>(constant.NameAndTypeIndex);
                        string className = file.GetClassName(constant.ClassIndex)
                                           ?? throw new Exception();
                        string name = file.GetStringConstant(nameAndType.NameIndex) ?? throw new Exception();
                        string type = file.GetStringConstant(nameAndType.TypeIndex) ?? throw new Exception();
                        MethodSig methodSignature = MethodSignature(false, type);
                        body.Instructions.Add(Instruction.Create(
                            op.Operation is Operation.InvokeSpecial or Operation.InvokeStatic
                                ? OpCodes.Call
                                : OpCodes.Callvirt, Resolve(className, name, methodSignature)));

                        break;
                    }
                    case Operation.GetField or Operation.GetStatic or Operation.PutField or Operation.PutStatic: {
                        OpcodeOneValue opcode = (OpcodeOneValue) op;
                        ClassFile.RefConstant constant = file.GetConstant<ClassFile.RefConstant>((ushort) opcode.Value);
                        ClassFile.NameAndType nameAndType =
                            file.GetConstant<ClassFile.NameAndType>(constant.NameAndTypeIndex);
                        string className = file.GetClassName(constant.ClassIndex)
                                           ?? throw new Exception();
                        string name = file.GetStringConstant(nameAndType.NameIndex) ?? throw new Exception();
                        body.Instructions.Add(Instruction.Create(op.Operation switch {
                            Operation.GetField => OpCodes.Ldfld,
                            Operation.GetStatic => OpCodes.Ldsfld,
                            Operation.PutField => OpCodes.Stfld,
                            Operation.PutStatic => OpCodes.Stsfld,
                        }, Resolve(className, name)));
                        break;
                    }
                }
            }
        }
    }

    private ITypeDefOrRef Resolve(string className) {
        if (JavaTypes.TryGetValue(className, out Type? javaType)) return Module.Import(javaType);
        return Classes.Where(x => x.Item2.Name == className).Select(x => x.Item1).FirstOrDefault() ??
               throw new KeyNotFoundException($"Could not find class {className}");
    }

    private IMethod Resolve(string className, string methodName, MethodSig methodSig) {
        if (JavaTypes.TryGetValue(className, out Type? javaType)) {
            IEnumerable<MethodBase> methods = javaType.GetMethods(BindingFlags.Static
                                                                  | BindingFlags.Instance
                                                                  | BindingFlags.Public
                                                                  | BindingFlags.NonPublic)
                .Where(x => x.Name == TranslateSpecialName(methodName))
                .Concat(javaType.GetConstructors(BindingFlags.Static
                                                 | BindingFlags.Instance
                                                 | BindingFlags.Public
                                                 | BindingFlags.NonPublic).Cast<MethodBase>());
            return Module.Import(methods.FirstOrDefault(x =>
                                     Module.Import(x).MethodSig.ToString() == methodSig.ToString()) ??
                                 throw new KeyNotFoundException($"Could not find method {methodName} on {className}"));
        }

        TypeDefUser otherTypeDef =
            Classes.Where(x => x.Item2.Name == TranslateSpecialName(className)).Select(x => x.Item1).FirstOrDefault() ??
            throw new KeyNotFoundException($"Could not find class {className}");
        return otherTypeDef.Methods.FirstOrDefault(x => x.Name == TranslateSpecialName(methodName)) ??
               throw new KeyNotFoundException($"Could not find method {methodName} on {className}");
    }

    private IField Resolve(string className, string fieldName) {
        if (JavaTypes.TryGetValue(className, out Type? javaType)) {
            FieldInfo field = javaType.GetFields(BindingFlags.Static
                                                 | BindingFlags.Instance
                                                 | BindingFlags.Public
                                                 | BindingFlags.NonPublic)
                                  .FirstOrDefault(x => x.Name == fieldName) ??
                              throw new KeyNotFoundException($"Could not find field {fieldName} on {className}");
            return Module.Import(field);
        }

        TypeDefUser otherTypeDef =
            Classes.Where(x => x.Item2.Name == className).Select(x => x.Item1).FirstOrDefault() ??
            throw new KeyNotFoundException($"Could not find class {className}");
        return otherTypeDef.Fields.FirstOrDefault(x => x.Name == fieldName) ??
               throw new KeyNotFoundException($"Could not find field {fieldName} on {className}");
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
        return new MethodSig(isStatic ? CallingConvention.Default : CallingConvention.HasThis, 0, ComponentSignature(
            signature[
                (args.Length + 2)..], true), argSignatures);
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
        fieldDef.Signature = new FieldSig(ComponentSignature(field.Type, false));
    }

    public TypeSig ComponentSignature(string type, bool isReturnType) {
        int pos = 0;
        return ComponentSignature(type, isReturnType, ref pos);
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
                return Resolve(typeName).ToTypeSig();
            }
            case '[':
                return new ArraySig(ComponentSignature(type, isReturnType, ref pos), 1);
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
                    methodDef.ImplAttributes |= MethodImplAttributes.InternalCall;

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
            if (file.GetAttribute<NestHostAttribute>() is { } host) {
                type.Attributes |= TypeAttributes.NestedPublic;
                Classes.First(x => x.Item2.Name == host.ClassName).Item1.NestedTypes.Add(type);
            } else
                Module.Types.Add(type);

            type.BaseType = Resolve(file.SuperClass);

            TranslateClass(file, type);

            foreach (MethodDefUser methodDef in type.Methods.Cast<MethodDefUser>()) {
                Method method = methods[methodDef];
                TranslateMethod(file, methodDef, method);
            }
        }

        return Module;
    }
}