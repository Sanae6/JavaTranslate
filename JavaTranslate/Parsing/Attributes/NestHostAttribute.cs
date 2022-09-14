namespace JavaTranslate.Parsing.Attributes; 

[JavaAttribute("NestHost")]
public class NestHostAttribute : AttributeData {
    public string ClassName { get; private set; } = null!;
    protected override void Read(ClassFile classFile, ref SpanReader reader) {
        ClassName = classFile.GetClassName(reader.ReadU16())!;
    }
}