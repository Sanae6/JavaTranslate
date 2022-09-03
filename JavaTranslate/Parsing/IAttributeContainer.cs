using System.Reflection;
using JavaTranslate.Parsing.Attributes;

namespace JavaTranslate.Parsing; 

public interface IAttributeContainer {
    Attribute[] Attributes { get; }

    public T? GetAttribute<T>() where T : AttributeData;
}