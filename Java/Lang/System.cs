using java.io;

namespace java.lang; 

public class System : Object {
    public static PrintStream @out = new PrintStream(null!);
    public static InputStream @in = new InputStream();
}