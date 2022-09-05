namespace JavaTranslate.Parsing.Attributes;

[JavaAttribute("LocalVariableTable")]
public class LocalVariableTableAttribute : AttributeData {
    public LocalVariable[] Locals { get; private set; }

    protected override void Read(ClassFile classFile, ref SpanReader reader) {
        int length = reader.ReadU16();
        Locals = new LocalVariable[length];
        for (int i = 0; i < length; i++) {
            Locals[i] = new LocalVariable {
                Start = reader.ReadU16(),
                Length = reader.ReadU16(),
                Name = classFile.GetStringConstant(reader.ReadU16())!,
                Type = classFile.GetStringConstant(reader.ReadU16())!,
                Index = reader.ReadU16()
            };
        }
    }

    public class LocalVariable {
        public ushort Start;
        public ushort Length;
        public string Name = null!;
        public string Type = null!;
        public ushort Index;

        public bool InRange(int offset) {
            return offset >= Start && offset < Start + Length;
        }
    }
}