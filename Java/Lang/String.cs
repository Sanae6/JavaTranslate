using java.util.stream;

namespace java.lang;

public class String : Object {
    internal string BackingString { get; }

    private String(string str) {
        BackingString = str;
    }

    private String(char[] chars) {
        BackingString = new string(chars);
    }

    public char[] toCharArray() {
        return BackingString.ToCharArray();
    }

    public String toLowerCase() {
        return BackingString.ToLower();
    }

    public IntStream chars() {
        throw new NotImplementedException();
    }

    public static String FromNetString(string str) => new String(str);

    public static implicit operator String(string str) => FromNetString(str);
}