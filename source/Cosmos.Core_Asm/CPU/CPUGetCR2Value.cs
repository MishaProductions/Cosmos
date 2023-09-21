using XSharp.Assembler;
using XSharp;
using static XSharp.XSRegisters;
using XSharp.Assembler.x86;

namespace Cosmos.Core_Asm
{
    public class CPUGetCR2Value : AssemblerMethod
    {
        public override void AssembleNew(Assembler aAssembler, object aMethodInfo)
        {
            XS.Set(EAX, CR2);
            XS.Push(EAX);
        }
    }
}
