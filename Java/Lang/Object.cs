namespace java.lang; 

public class Object {
    public Object() {
        
    }

    public virtual Object clone() {
        return (Object) MemberwiseClone();
    }

    public virtual bool equals(Object other) {
        return Equals(other);
    }

    protected virtual void finalize() {
        
    }
}