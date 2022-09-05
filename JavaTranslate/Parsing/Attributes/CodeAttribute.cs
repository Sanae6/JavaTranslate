using System.Collections.Immutable;
using System.Reflection;

namespace JavaTranslate.Parsing.Attributes;

[JavaAttribute("Code")]
public sealed class CodeAttribute : AttributeData, IAttributeContainer {
    public int MaxStack { get; private set; }
    public int MaxLocals { get; private set; }
    public IOpcode[] Code { get; private set; } = null!;
    public ExceptionHandler[] ExceptionTable { get; private set; } = null!;
    public Attribute[] Attributes { get; private set; } = null!;
    public T? GetAttribute<T>() where T : AttributeData {
        return (T?) Attributes
            .FirstOrDefault(x => typeof(T).GetCustomAttribute<JavaAttributeAttribute>()?.Name == x.Name)?.Data;
    }

    protected override void Read(ClassFile classFile, ref SpanReader reader) {
        MaxStack = reader.ReadU16();
        MaxLocals = reader.ReadU16();
        Code = GetOpcodes(ref reader).ToArray();
        int exceptionCount = reader.ReadU16();
        ExceptionTable = new ExceptionHandler[exceptionCount];
        for (int i = 0; i < exceptionCount; i++) {
            ExceptionTable[i] = new ExceptionHandler(reader.ReadU16(), reader.ReadU16(),
                reader.ReadU16(), classFile.ReadConstant<ushort>(ref reader));
        }
        Attributes = classFile.ReadAttributes(ref reader);
    }

    public readonly struct ExceptionHandler {
        public readonly ushort Start;
        public readonly ushort End;
        public readonly ushort Handler;
        public readonly ushort CatchClass;
        public ExceptionHandler(ushort start, ushort end, ushort handler, ushort catchClass) {
            Start = start;
            End = end;
            Handler = handler;
            CatchClass = catchClass;
        }
    }

    private static IEnumerable<IOpcode> GetOpcodes(ref SpanReader reader) {
        int end = reader.ReadI32() + reader.Position;
        int start = reader.Position;
        List<IOpcode> opcodes = new List<IOpcode>();
        while (reader.Position < end) {
            Operation op = reader.Read<Operation>();
            opcodes.Add(ReadOpcode(ref reader, op, reader.Position - start, false));
        }
        return opcodes;
    }

    private static IOpcode ReadOpcode(ref SpanReader reader, Operation op, int offset, bool wide) {
        if (op == Operation.Wide) {
            if (wide) throw new Exception("Wide opcode recursion!");
            return ReadOpcode(ref reader, reader.Read<Operation>(), offset, true);
        }

        OperationType type = Types[(int) op];
        return type switch {
            OperationType.Simple => new OpcodeSimple(op, offset),
            OperationType.Branch => new OpcodeBranch(op, offset,
                op is Operation.GotoWide or Operation.JsrWide ? reader.ReadI32() : reader.ReadI16()),
            OperationType.ValueExtra => new OpcodeValueExtra(op, offset, reader.ReadI16(), reader.Read<byte>()),
            OperationType.OneValue => new OpcodeOneValue(op, offset, wide ? reader.ReadI16() : reader.Read<byte>()),
            OperationType.TwoValue => new OpcodeTwoValue(op, offset, wide ? reader.ReadI16() : reader.Read<byte>(),
                wide ? reader.ReadI16() : reader.Read<byte>()),
            OperationType.WideOneValue => new OpcodeOneValue(op, offset, reader.ReadI16()),
            OperationType.Table =>
                throw new NotImplementedException("Jump table opcodes are not yet supported"),
            _ => throw new NotImplementedException()
        };
    }

    #region Types
    private enum OperationType {
        Simple,
        Branch,
        ValueExtra,
        OneValue,
        WideOneValue,
        TwoValue,
        Table
    }

    private static readonly ImmutableArray<OperationType> Types = ImmutableArray.Create(
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.OneValue,
        OperationType.WideOneValue,
        OperationType.OneValue,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.OneValue,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.TwoValue,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.OneValue,
        OperationType.Table,
        OperationType.Table,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.ValueExtra, // treat zeroes as NOPs
        OperationType.OneValue, // treat zeroes as NOPs
        OperationType.WideOneValue,
        OperationType.OneValue,
        OperationType.WideOneValue,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.WideOneValue,
        OperationType.WideOneValue,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.WideOneValue, // special case
        OperationType.ValueExtra,
        OperationType.Branch,
        OperationType.Branch,
        OperationType.Branch, // goto_w - wide check occurs in branch switch
        OperationType.Branch, // jsr_w - wide check occurs in branch switch
        OperationType.Simple,
        OperationType.Simple, // reserved start
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple,
        OperationType.Simple, // reserved end
        OperationType.Simple, // imp def
        OperationType.Simple // imp def
    );
    #endregion
}