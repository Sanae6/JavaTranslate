namespace JavaTranslate.ClassFile; 

public class Class {
    public string Name { get; }
    public List<Class> Children { get; }
    public Attribute[] Attributes { get; }
    public Class(string name, List<Class> children, Attribute[] attributes) {
        Name = name;
        Children = children;
        Attributes = attributes;
    }
}