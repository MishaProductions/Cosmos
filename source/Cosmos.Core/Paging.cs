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
            //const string xHex = "0123456789ABCDEF";
            //var lastKnownAddressValue = CPU.GetEndOfKernel();
            //if (lastKnownAddressValue != 0)
            //{
            //    PutErrorString(1, 0, "End of kernel addr: 0x");

            //    PutErrorChar(1, 22, xHex[(int)((lastKnownAddressValue >> 28) & 0xF)]);
            //    PutErrorChar(1, 23, xHex[(int)((lastKnownAddressValue >> 24) & 0xF)]);
            //    PutErrorChar(1, 24, xHex[(int)((lastKnownAddressValue >> 20) & 0xF)]);
            //    PutErrorChar(1, 25, xHex[(int)((lastKnownAddressValue >> 16) & 0xF)]);
            //    PutErrorChar(1, 26, xHex[(int)((lastKnownAddressValue >> 12) & 0xF)]);
            //    PutErrorChar(1, 27, xHex[(int)((lastKnownAddressValue >> 8) & 0xF)]);
            //    PutErrorChar(1, 28, xHex[(int)((lastKnownAddressValue >> 4) & 0xF)]);
            //    PutErrorChar(1, 29, xHex[(int)(lastKnownAddressValue & 0xF)]);
            //}

            CPU.UpdateIDT(true); //Before enabling paging, setup IDT to catch issues
            DoEnable();
            IsEnabled = true;
            buf[0] = (byte)'!';
            buf[1] = 0x0f;
            
        }

        public static void Map(ulong PhysicalAddress, ulong VirtualAddress, PageSize size, PageFlags flags)
        {
            var pml2Entry = (VirtualAddress >> 22) & 0x03FF;
            var pml1Entry = (VirtualAddress >> 12) & 0x03FF;

            if (size == PageSize._4MB)
            {
                PageDirectory[pml2Entry] = (uint)(PhysicalAddress | ((uint)PageFlags.Present | (uint)flags | (1 << 7)));
                return;
            }
            else if (size == PageSize._4KB)
            {
                var pd = GetNextLevel(PageDirectory, pml2Entry, true);
                var pt = GetNextLevel(pd, pml1Entry, true);
                pt[pml1Entry] = (uint)(PhysicalAddress | (uint)(PageFlags.Present | flags));
            }

            if (IsEnabled)
            {
                RefreshPages();
            }
        }
        public static void Unmap(ulong PhysicalAddress, ulong VirtualAddress, PageSize size)
        {
            var pml2Entry = (VirtualAddress >> 22) & 0x03FF;
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

            if (IsEnabled)
            {
                RefreshPages();
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
        private static void PutErrorChar(int line, int col, char c)
        {
            unsafe
            {
                byte* xAddress = (byte*)0xB8000;

                xAddress += (line * 80 + col) * 2;

                xAddress[0] = (byte)c;
                xAddress[1] = 0x0C;
            }
        }

        /// <summary>
        /// Put error string.
        /// </summary>
        /// <param name="line">Line to put the error string at.</param>
        /// <param name="startCol">Starting column to put the error string at.</param>
        /// <param name="error">Error string to put.</param>
        /// <exception cref="System.OverflowException">Thrown if error length in greater then Int32.MaxValue.</exception>
        private static void PutErrorString(int line, int startCol, string error)
        {
            for (int i = 0; i < error.Length; i++)
            {
                PutErrorChar(line, startCol + i, error[i]);
            }
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
