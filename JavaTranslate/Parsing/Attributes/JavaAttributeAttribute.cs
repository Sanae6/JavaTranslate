namespace JavaTranslate.Parsing.Attributes; 

[AttributeUsage(AttributeTargets.Class)]
internal class JavaAttributeAttribute : System.Attribute {
    public string Name { get; }
    public JavaAttributeAttribute(string name) {
        Name = name;
    }
}