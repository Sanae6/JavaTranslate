using System.Reflection;
using JavaTranslate.Parsing.Attributes;

namespace JavaTranslate.Parsing;

public class Method : IAttributeContainer {
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

    public T? GetAttribute<T>() where T : AttributeData {
        return (T?) Attributes
            .FirstOrDefault(x => typeof(T).GetCustomAttribute<JavaAttributeAttribute>()?.Name == x.Name)?.Data;
    }
}