using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchECS
{

    class ComponentRegistry
    {
        private static int MaxComponentId = 0;
        internal class ComponentId<T>
        {
            static ComponentId()
            {
                Index = Interlocked.Increment(ref MaxComponentId);
            }
            public static int Index;
        }
    }

    struct Component
    {
        public Type TargetType;
        public bool IsTag;
        public List<int> MemberOfTables;
        internal ComponentBuffer BufferPrototype;

        public static Component MakeComponent<T>(World world)
        {
            return new Component 
            { 
                TargetType = typeof(T), 
                BufferPrototype = new ComponentBuffer<T>(0, world.ZeroTable()), 
                IsTag = RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? false : Marshal.SizeOf<T>() == 0, 
                MemberOfTables = new List<int>() 
            };
        }

        internal static (int[] components, int bufferCount) RealComponentCount(int[] components, World world)
        {
            Array.Sort<int>(components, (itm1, itm2) => IsRealComponent(itm1, world) ? 1 : 0);
            return (components, Array.FindLastIndex(components, (itm) => IsRealComponent(itm, world)) + 1);
        }

        internal static bool IsRealComponent(int componentId, World world)
        {
            return !world._components[componentId].IsTag;
        }
    }
}
