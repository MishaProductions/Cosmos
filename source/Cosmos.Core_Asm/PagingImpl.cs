using System;
using IL2CPU.API.Attribs;
using XSharp.Assembler.x86;
using XSharp.Assembler;
using XSharp;

namespace Cosmos.Core_Asm
{
    [Plug(Target = typeof(Core.Paging))]
    public unsafe class PagingImpl
    {
        [PlugMethod(Assembler = typeof(GetBootPageDirectoryPlug))]
        public static uint* GetBootPageDirectory()
        {
            throw new NotImplementedException();
        }
        [PlugMethod(Assembler = typeof(GetBootPageTable1Plug))]
        public static uint* GetBootPageTable1()
        {
            throw new NotImplementedException();
        }
        [PlugMethod(Assembler = typeof(EnablePagingAsm))]
        public static void DoEnable()
        {
            throw new NotImplementedException();
        }
        [PlugMethod(Assembler = typeof(RefreshPagesAsm))]
        public static void RefreshPages()
        {
            throw new NotImplementedException();
        }
    }

    class GetBootPageDirectoryPlug : AssemblerMethod
    {
        public override void AssembleNew(Assembler aAssembler, object aMethodInfo)
        {
            XS.Push("boot_page_directory");
        }
    }
    class GetBootPageTable1Plug : AssemblerMethod
    {
        public override void AssembleNew(Assembler aAssembler, object aMethodInfo)
        {
            XS.Push("boot_page_table1");
        }
    }
    class EnablePagingAsm : AssemblerMethod
    {
        public override void AssembleNew(Assembler aAssembler, object aMethodInfo)
        {
            //Set CR3 to the address of the page directory
            XS.Set(XSRegisters.EAX, "boot_page_directory");
            XS.Set(XSRegisters.CR3, XSRegisters.EAX);
           
            //XS.Exchange(XSRegisters.BX, XSRegisters.BX); //bochs magic break for debugging

            //Enable PSE bit in CR4 (4MiB pages)
            XS.Set(XSRegisters.EAX, XSRegisters.CR4);
            XS.Or(XSRegisters.EAX, 0x00000010);
            XS.Set(XSRegisters.CR4, XSRegisters.EAX);

            //Set the paging bit in CR0
            XS.Set(XSRegisters.EAX, XSRegisters.CR0);
            XS.Or(XSRegisters.EAX, 0x80000000);
            XS.Set(XSRegisters.CR0, XSRegisters.EAX);
        }
    }
    class RefreshPagesAsm : AssemblerMethod
    {
        public override void AssembleNew(Assembler aAssembler, object aMethodInfo)
        {
            //reload CR3
            new Mov
            {
                DestinationReg = RegistersEnum.EAX,
                SourceReg = RegistersEnum.CR3
            };
            new Mov
            {
                DestinationReg = RegistersEnum.CR3,
                SourceReg = RegistersEnum.EAX
            };
        }
    }
}
