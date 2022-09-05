namespace java.lang; 

public class String : Object {
    internal string BackingString { get; }

    private String(string str) {
        BackingString = str;
    }

    public static String FromNetString(string str) => new String(str);
}