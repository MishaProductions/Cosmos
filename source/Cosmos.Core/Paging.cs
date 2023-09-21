using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmos.Core.Memory;
using IL2CPU.API.Attribs;

namespace Cosmos.Core
{
    public unsafe class Paging
    {
        private static bool IsInitCalled = false;
        private static bool IsEnabled = false;
        private static uint* PageDirectory;
        public static void Init()
        {
            if (IsInitCalled)
            {
                return;
            }
            IsInitCalled = true;

            var buf = (byte*)0xB8000;
            buf[0] = (byte)'A';
            buf[1] = 0x0f;

            PageDirectory = GetBootPageDirectory();
            var firstTable = (uint*)new ManagedMemoryBlock(4096, 4096).Offset;

            Map(0, 0, PageSize._4MB, PageFlags.RW); //Map the first 4MB of memory

            // Map kernel
            for (ulong i = 0x2000000; i < 0x2500000; i += 0x400000)
            {
                Map(i, i, PageSize._4MB, PageFlags.RW);
            }

            // Map Memory Manager
            for (ulong i = (ulong)RAT.RamStart; i < (ulong)(RAT.HeapEnd); i += 0x400000)
            {
                Map(i, i, PageSize._4MB, PageFlags.RW);
            }




            buf[0] = (byte)'E';
            buf[1] = 0x0f;

            CPU.UpdateIDT(true); //Before enabling paging, setup IDT to catch issues
            DoEnable();
            IsEnabled = true;
            buf[0] = (byte)'!';
            buf[1] = 0x0f;
        }

        public static void Map(ulong PhysicalAddress, ulong VirtualAddress, PageSize size, PageFlags flags)
        {
            var pml2Entry = (VirtualAddress >> 22);
            var pml1Entry = (VirtualAddress >> 12) & 0x03FF;

            if (size == PageSize._4MB)
            {
                PageDirectory[pml2Entry] = (uint)(PhysicalAddress | (uint)(PageFlags.Present | flags) | (1 << 7));
                return;
            }
            else if (size == PageSize._4KB)
            {
                var pd = GetNextLevel(PageDirectory, pml2Entry, true);
                var pt = GetNextLevel(pd, pml1Entry, true);
                pt[pml1Entry] = (uint)(PhysicalAddress | (uint)(PageFlags.Present | flags));
            }
        }
        public static void Unmap(ulong PhysicalAddress, ulong VirtualAddress, PageSize size)
        {
            var pml2Entry = (VirtualAddress >> 22);
            var pml1Entry = (VirtualAddress >> 12) & 0x03FF;



            if (size == PageSize._4KB)
            {
                var pml2 = GetNextLevel(PageDirectory, pml2Entry, false);
                if (pml2 == null)
                {
                    return;
                }
                var pml1 = GetNextLevel(pml2, pml1Entry, false);
                if (pml1 == null)
                {
                    return;
                }

                pml1[pml1Entry] = 0;
            }
            else if (size == PageSize._4MB)
            {
                PageDirectory[pml2Entry] = 0;
            }
        }
        /// <summary>
        /// Returns pointer to the level
        /// </summary>
        /// <param name="topLevel"></param>
        /// <param name="idx"></param>
        /// <param name="allocate"></param>
        /// <returns></returns>
        private static uint* GetNextLevel(uint* topLevel, ulong idx, bool allocate)
        {
            if ((PageDirectory[idx] & 1) != 0)
            {
                //The next level is present, return it
                return (uint*)(PageDirectory[idx] & ~((ulong)0xFFF));
            }

            if (!allocate)
            {
                return null;
            }

            uint* nextLevel = (uint*)new ManagedMemoryBlock(4096, 4096).Offset;

            for (int i = 0; i < 1024; i++)
            {
                // This sets the following flags to the pages:
                //   Supervisor: Only kernel-mode can access them
                //   Write Enabled: It can be both read from and written to
                //   Not Present: The page table is not present
                nextLevel[i] = 2;
            }
            topLevel[idx] = (uint)((uint)nextLevel | 0b111);
            return nextLevel;
        }

        //plugged
        [PlugMethod(PlugRequired = true)]
        public static uint* GetBootPageDirectory()
        {
            throw null;
        }
        [PlugMethod(PlugRequired = true)]
        public static uint* GetBootPageTable1()
        {
            throw null;
        }
        [PlugMethod(PlugRequired = true)]
        public static void DoEnable()
        {

        }
        [PlugMethod(PlugRequired = true)]
        public static void RefreshPages()
        {

        }
    }
    [Flags]
    public enum PageFlags : uint
    {
        /// <summary>
        /// Page is present
        /// </summary>
        Present = 0b1,
        /// <summary>
        /// Page is read/write
        /// </summary>
        RW = 0b10,
        /// <summary>
        /// Userspace can access the page
        /// </summary>
        User = 0b100,
    }
    /// <summary>
    /// Size of a page
    /// </summary>
    public enum PageSize
    {
        _4KB,
        _4MB
    }
}
