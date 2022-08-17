namespace JavaTranslate.ClassFile;

public class Method {
    public AccessFlags Flags { get; }
    public string Name { get; }
    public string Type { get; }
    public Attribute[] Attributes { get; }

    public Method(AccessFlags flags, string name, string type, Attribute[] attributes) {
        Flags = flags;
        Name = name;
        Type = type;
        Attributes = attributes;
    }
}