using JavaTranslate.Parsing.Attributes;

namespace JavaTranslate.Parsing; 

public class Attribute {
    public string Name { get; }
    public AttributeData? Data { get; }
    public Attribute(string name, ClassFile classFile, ref SpanReader data, int length) {
        Name = name;
        Data = AttributeData.CreateAttributeData(name, classFile, ref data, length);
    }
}