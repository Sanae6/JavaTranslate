namespace java.io; 

public abstract class OutputStream : Closeable, Flushable {
    public abstract void write(int b);
    public abstract void write(byte[] b);
    public abstract void write(byte[] b, int offset, int length);
    public abstract void close();

    public abstract void flush();
}