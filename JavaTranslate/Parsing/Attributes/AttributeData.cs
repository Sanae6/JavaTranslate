using System.Reflection;

namespace JavaTranslate.Parsing.Attributes;

public abstract class AttributeData {
    #region Attribute Factory
    internal delegate AttributeData AttributeCreator(ClassFile classFile, ref SpanReader reader);

    internal static Dictionary<string, AttributeCreator?> AttributeCreators = new Dictionary<string, AttributeCreator?>();
    static AttributeData() {
        foreach (Type type in typeof(AttributeData).Assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<JavaAttributeAttribute>() != null)) {
            AttributeCreators.Add(type.GetCustomAttribute<JavaAttributeAttribute>()!.Name, (ClassFile classFile, ref SpanReader reader) => {
                AttributeData attrib = (AttributeData) Activator.CreateInstance(type)!;
                attrib.Read(classFile, ref reader);
                return attrib;
            });
        }
    }
    internal static AttributeData? CreateAttributeData(string name, ClassFile classFile, ref SpanReader reader, int length) {
        if (AttributeCreators.TryGetValue(name, out AttributeCreator? creator))
            return creator!(classFile, ref reader);
        Console.WriteLine($"Could not find attribute creator for {name}");
        reader.Position += length;
        return null;
    }
    #endregion
    protected abstract void Read(ClassFile classFile, ref SpanReader reader);
}