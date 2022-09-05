namespace java.io; 

public class PrintStream {
    
    public void println(java.lang.String text) {
        Console.WriteLine(text.BackingString);
    }
}