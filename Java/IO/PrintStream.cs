namespace java.io; 

public class PrintStream : FilterOutputStream {
    
    public void println(java.lang.String text) {
        Console.WriteLine(text.BackingString);
    }
    
    public void println(int value) {
        Console.WriteLine(value);
    }
    
    public void println() {
        Console.WriteLine();
    }

    public void print(java.lang.String text) {
        Console.Write(text.BackingString);
    }

    public void print(int value) {
        Console.Write(value);
    }

    public PrintStream(OutputStream stream) : base(stream) {
    }
}