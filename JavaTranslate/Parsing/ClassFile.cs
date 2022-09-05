using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using JavaTranslate.Parsing.Attributes;

namespace JavaTranslate.Parsing;

public sealed class ClassFile : IAttributeContainer {
    private readonly ImmutableArray<byte> Magic = ImmutableArray.Create<byte>(0xCA, 0xFE, 0xBA, 0xBE);

    private object[] Constants = null!;
    public string Name = null!;
    public AccessFlags Flags;
    public string SuperClass = null!;
    public string[] Interfaces = null!;
    public Field[] Fields = null!;
    public Method[] Methods = null!;
    public Attribute[] Attributes { get; private set; } = null!;

    public T? GetAttribute<T>() where T : AttributeData {
        return (T?) Attributes
            .FirstOrDefault(x => typeof(T).GetCustomAttribute<JavaAttributeAttribute>()?.Name == x.Name)?.Data;
    }

    public ClassFile(ReadOnlySpan<byte> data) {
        if (!data[..4].SequenceEqual(Magic.AsSpan())) {
            throw new ArgumentException("Invalid class magic!", nameof(data));
        }

        Read(data);
    }

    private void Read(ReadOnlySpan<byte> data) {
        SpanReader reader = new SpanReader(data, 8);
        ReadConstants(ref reader);
        Flags = (AccessFlags) reader.ReadU16();
        Name = ReadClassName(ref reader);
        SuperClass = ReadClassName(ref reader);
        Interfaces = new string[reader.ReadU16()];
        for (int i = 0; i < Interfaces.Length; i++) {
            Interfaces[i] = ReadClassName(ref reader);
        }

        ReadFields(ref reader);
        ReadMethods(ref reader);
        Attributes = ReadAttributes(ref reader);
    }

    public object? GetConstant(ushort index) {
        return index == 0 ? default : Constants[index - 1];
    }

    public T? GetConstant<T>(ushort index) {
        if (index == 0) return default;
        return (T) Constants[index - 1];
    }

    public string? GetStringConstant(ushort index) {
        return GetConstant<string>(index);
    }

    public string? GetClassName(ushort index) {
        return GetStringConstant(GetConstant<ClassConstant>(index).Name);
    }

    internal T ReadConstant<T>(ref SpanReader reader) {
        ushort constant = reader.ReadU16();
        return (T) Constants[constant - 1];
    }

    private string ReadClassName(ref SpanReader reader) {
        return (string) Constants[ReadConstant<ClassConstant>(ref reader).Name - 1];
    }

    private void ReadFields(ref SpanReader reader) {
        ushort fieldCount = reader.ReadU16();
        Fields = new Field[fieldCount];
        for (int i = 0; i < fieldCount; i++) {
            AccessFlags flags = (AccessFlags) reader.ReadU16();
            string name = ReadConstant<string>(ref reader);
            string type = ReadConstant<string>(ref reader);
            Attribute[] attributes = ReadAttributes(ref reader);
            Fields[i] = new Field(flags, name, type, attributes);
        }
    }

    private void ReadMethods(ref SpanReader reader) {
        ushort methodCount = reader.ReadU16();
        Methods = new Method[methodCount];
        for (int i = 0; i < methodCount; i++) {
            AccessFlags flags = (AccessFlags) reader.ReadU16();
            string name = ReadConstant<string>(ref reader);
            string type = ReadConstant<string>(ref reader);
            Attribute[] attributes = ReadAttributes(ref reader);
            Methods[i] = new Method(flags, name, type, attributes);
        }
    }

    internal Attribute[] ReadAttributes(ref SpanReader reader) {
        ushort attributeCount = reader.ReadU16();
        Attribute[] attributes = new Attribute[attributeCount];
        for (int i = 0; i < attributeCount; i++) {
            string name = ReadConstant<string>(ref reader);
            int length = reader.ReadI32();
            attributes[i] = new Attribute(name, this, ref reader, length);
        }

        return attributes;
    }

    private void ReadConstants(ref SpanReader reader) {
        ushort constantCount = (ushort) (reader.ReadU16() - 1);
        Constants = new object[constantCount];
        for (ushort i = 0; i < constantCount; i++) {
            ConstantPoolType type = reader.Read<ConstantPoolType>();
            switch (type) {
                case ConstantPoolType.Utf8: {
                    ushort length = reader.ReadU16();
                    Constants[i] = Encoding.UTF8.GetString(reader.ReadData(length));
                    break;
                }
                case ConstantPoolType.Integer:
                    Constants[i] = reader.ReadI32();
                    break;
                case ConstantPoolType.Float:
                    Constants[i] = BinaryPrimitives.ReadSingleBigEndian(reader.ReadData(4));
                    break;
                case ConstantPoolType.Long: {
                    long finalConstant =
                        unchecked((long) ((ulong) reader.ReadI32() << 32
                                          | (uint) reader.ReadI32()));
                    Constants[i++] = finalConstant;
                    Constants[i] =
                        finalConstant; // "In retrospect, making 8-byte constants take two constant pool entries was a poor choice."
                    break;
                }
                case ConstantPoolType.Double: {
                    double finalDouble = BitConverter.UInt64BitsToDouble((ulong) reader.ReadI32() << 32
                                                                         | (uint) reader.ReadI32());
                    Constants[i++] = finalDouble;
                    Constants[i] =
                        finalDouble; // "In retrospect, making 8-byte constants take two constant pool entries was a poor choice."
                    break;
                }
                case ConstantPoolType.MethodRef or ConstantPoolType.FieldRef or ConstantPoolType.InterfaceMethodRef:
                    Constants[i] = new RefConstant(ref reader);
                    break;
                case ConstantPoolType.String:
                    Constants[i] = new StringConstant(reader.ReadU16());
                    break;
                case ConstantPoolType.Class:
                    Constants[i] = new ClassConstant(reader.ReadU16());
                    break;
                case ConstantPoolType.NameAndType:
                    Constants[i] = new NameAndType(ref reader);
                    break;
                default:
                    throw new InvalidDataException($"Invalid constant type {type}");
            }
        }

        for (ushort i = 0; i < constantCount; i++) {
            switch (Constants[i]) {
                case StringConstant strConst:
                    Constants[i] = GetStringConstant(strConst.Location)!;
                    break;
            }
        }
    }

    public readonly struct RefConstant {
        public RefConstant(ref SpanReader reader) {
            reader.Position--;
            Type = reader.Read<ConstantPoolType>();
            ClassIndex = reader.ReadU16();
            NameAndTypeIndex = reader.ReadU16();
        }

        public ConstantPoolType Type { get; }
        public ushort ClassIndex { get; }
        public ushort NameAndTypeIndex { get; }
    }

    private readonly struct StringConstant {
        public readonly ushort Location;

        public StringConstant(ushort location) {
            Location = location;
        }
    }

    private readonly struct ClassConstant {
        public readonly ushort Name;

        public ClassConstant(ushort name) {
            Name = name;
        }
    }

    public readonly struct NameAndType {
        public NameAndType(ref SpanReader reader) {
            NameIndex = reader.ReadU16();
            TypeIndex = reader.ReadU16();
        }

        public ushort NameIndex { get; }
        public ushort TypeIndex { get; }
    }
}