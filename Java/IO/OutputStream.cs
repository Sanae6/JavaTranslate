namespace java.io; 

public abstract class OutputStream : Closeable, Flushable {
    public abstract void write(int b);

    public virtual void write(byte[] b) {
        write(b, 0, b.Length);
    }

    public virtual void write(byte[] b, int offset, int length) {
        for (int i = offset; i < length; i++) {
            write(b[i]);
        }
    }
    public abstract void close();

    public abstract void flush();
}