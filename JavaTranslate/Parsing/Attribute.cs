namespace JavaTranslate.ClassFile; 

public class Attribute {
    public string Name { get; }
    public byte[] Data { get; }
    public Attribute(string name, byte[] data) {
        Name = name;
        Data = data;
    }
}