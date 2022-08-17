using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;

namespace JavaTranslate.ClassFile;

public class ClassFile {
    private readonly ImmutableArray<byte> Magic = ImmutableArray.Create<byte>(0xCA, 0xFE, 0xBA, 0xBE);

    private object[] Constants;
    private readonly List<ushort> Interfaces = new List<ushort>();
    private Field[] Fields;
    private Method[] Methods;
    private Attribute[] Attributes;

    public ClassFile(ReadOnlySpan<byte> data) {
        if (!data[..4].SequenceEqual(Magic.AsSpan())) {
            throw new ArgumentException("Invalid class magic!", nameof(data));
        }
        Read(data);
    }

    private void Read(ReadOnlySpan<byte> data) {
        int pos = 8; // skip magic and versions
        ReadConstants(data, ref pos);
        AccessFlags flags = (AccessFlags) ReadU16(data, ref pos);
        ushort thisClass = ReadU16(data, ref pos);
        ushort superClass = ReadU16(data, ref pos);
        ushort interfaceCount = ReadU16(data, ref pos);
        ushort i = 0;
        while (i++ < interfaceCount) {
            Interfaces.Add(ReadU16(data, ref pos));
        }
        
        ReadFields(data, ref pos);
        ReadMethods(data, ref pos);
        Attributes = ReadAttributes(data, ref pos);
    }

    private static ushort ReadU16(ReadOnlySpan<byte> data, ref int pos) {
        ushort result = BinaryPrimitives.ReadUInt16BigEndian(data[pos..]);
        pos += 2;
        return result;
    }

    private T ReadConstant<T>(ReadOnlySpan<byte> data, ref int pos) {
        ushort constant = ReadU16(data, ref pos);
        return (T) Constants[constant - 1];
    }

    private void ReadFields(ReadOnlySpan<byte> data, ref int pos) {
        ushort fieldCount = ReadU16(data, ref pos);
        Fields = new Field[fieldCount];
        for (int i = 0; i < fieldCount; i++) {
            AccessFlags flags = (AccessFlags) ReadU16(data, ref pos);
            string name = ReadConstant<string>(data, ref pos);
            string type = ReadConstant<string>(data, ref pos);
            Attribute[] attributes = ReadAttributes(data, ref pos);
            Fields[i] = new Field(flags, name, type, attributes);
        }
    }

    private void ReadMethods(ReadOnlySpan<byte> data, ref int pos) {
        ushort methodCount = ReadU16(data, ref pos);
        Methods = new Method[methodCount];
        for (int i = 0; i < methodCount; i++) {
            AccessFlags flags = (AccessFlags) ReadU16(data, ref pos);
            string name = ReadConstant<string>(data, ref pos);
            string type = ReadConstant<string>(data, ref pos);
            Attribute[] attributes = ReadAttributes(data, ref pos);
            Methods[i] = new Method(flags, name, type, attributes);
        }
    }

    private Attribute[] ReadAttributes(ReadOnlySpan<byte> data, ref int pos) {
        ushort attributeCount = ReadU16(data, ref pos);
        Attribute[] attributes = new Attribute[attributeCount];
        for (int i = 0; i < attributeCount; i++) {
            string name = ReadConstant<string>(data, ref pos);
            int length = BinaryPrimitives.ReadInt32BigEndian(data[pos..]);
            pos += 4;
            Console.WriteLine($"got attribute {name} - {length}");
            attributes[i] = new Attribute(name, data[pos..(pos + length)].ToArray());
            pos += length;
        }
        return attributes;
    }

    private void ReadConstants(ReadOnlySpan<byte> data, ref int pos) {
        ushort constantCount = (ushort) (ReadU16(data, ref pos) - 1);
        Constants = new object[constantCount];
        for (ushort i = 0; i < constantCount; i++) {
            ConstantPoolType type = (ConstantPoolType) data[pos++];
            switch (type) {
                case ConstantPoolType.Utf8: {
                    ushort length = ReadU16(data, ref pos);
                    Constants[i] = Encoding.UTF8.GetString(data[pos..(pos + length)]);
                    pos += length;
                    break;
                }
                case ConstantPoolType.Integer:
                    Constants[i] = BinaryPrimitives.ReadInt32BigEndian(data[pos..]);
                    pos += 4;
                    break;
                case ConstantPoolType.Float:
                    Constants[i] = BinaryPrimitives.ReadSingleBigEndian(data[pos..]);
                    pos += 4;
                    break;
                case ConstantPoolType.Long: {
                    long finalConstant =
                        unchecked((long) ((ulong) BinaryPrimitives.ReadInt32BigEndian(data[pos..]) << 32
                                          | (uint) BinaryPrimitives.ReadInt32BigEndian(data[(pos + 4)..])));
                    Constants[i++] = finalConstant;
                    Constants[i] = finalConstant; // "In retrospect, making 8-byte constants take two constant pool entries was a poor choice."
                    pos += 8;
                    break;
                }
                case ConstantPoolType.Double: {
                    double finalDouble = BitConverter.UInt64BitsToDouble((ulong) BinaryPrimitives.ReadInt32BigEndian(data[pos..]) << 32
                                                                         | (uint) BinaryPrimitives.ReadInt32BigEndian(data[(pos + 4)..]));
                    Constants[i++] = finalDouble;
                    Constants[i] = finalDouble; // "In retrospect, making 8-byte constants take two constant pool entries was a poor choice."
                    pos += 8;
                    break;
                }
                case ConstantPoolType.MethodRef or ConstantPoolType.FieldRef or ConstantPoolType.InterfaceMethodRef:
                    Constants[i] = new RefConstant(data[(pos - 1)..]);
                    pos += 4;
                    break;
                case ConstantPoolType.Class:
                    Constants[i] = ReadU16(data, ref pos);
                    break;
                case ConstantPoolType.NameAndType:
                    Constants[i] = new NameAndType(data[pos..]);
                    pos += 4;
                    break;
                default:
                    throw new InvalidDataException($"Invalid constant type {type}");
            }
        }
    }

    private readonly struct RefConstant {
        public RefConstant(ReadOnlySpan<byte> data) {
            Type = MemoryMarshal.Read<ConstantPoolType>(data);
            ClassIndex = BinaryPrimitives.ReadUInt16BigEndian(data[1..]);
            NameAndTypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[3..]);
        }
        public ConstantPoolType Type { get; }
        public ushort ClassIndex { get; }
        public ushort NameAndTypeIndex { get; }
    }

    private readonly struct NameAndType {
        public NameAndType(ReadOnlySpan<byte> data) {
            NameIndex = BinaryPrimitives.ReadUInt16BigEndian(data);
            TypeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[2..]);
        }
        public ushort NameIndex { get; }
        public ushort TypeIndex { get; }
    }
}