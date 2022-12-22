namespace java.io; 

public class FilterOutputStream : OutputStream {
    protected OutputStream @out;

    public FilterOutputStream(OutputStream stream) {
        @out = stream;
    }
    public override void write(int b) {
        @out.write(b);
    }

    public override void close() {
        @out.close();
    }

    public override void flush() {
        @out.flush();
    }
}