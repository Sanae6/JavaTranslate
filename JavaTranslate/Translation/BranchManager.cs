using dnlib.DotNet;
using dnlib.DotNet.Emit;
using JavaTranslate.Parsing;

namespace JavaTranslate.Translation; 

public class BranchManager {
    private MethodDefUser MethodDef;
    private Dictionary<int, int> LocationInstMap = new Dictionary<int, int>();

    public BranchManager(MethodDefUser methodDefUser) {
        MethodDef = methodDefUser;
    }

    public void AppendTargets(int offset, Instruction inst) {
        
    }

    public IEnumerable<Instruction> Branch(OpCode op, int location) {
        yield return Instruction.Create(OpCodes.Nop);
    }

    public void ResolveBranches() {
        
    }
}