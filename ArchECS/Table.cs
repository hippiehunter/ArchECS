using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace ArchECS
{
   

    public class Table : IDisposable
    {
        internal class VectorComparer256 : IEqualityComparer<Vector256<ulong>>, IComparer<Vector256<ulong>>
        {
            public int Compare(Vector256<ulong> x, Vector256<ulong> y)
            {
                var compareResult = Avx2.CompareEqual(x, y);
                return (int)(compareResult.GetElement(0) ^ (compareResult.GetElement(1) ^ (compareResult.GetElement(2) ^ compareResult.GetElement(3))));
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization )]
            public bool Equals(Vector256<ulong> x, Vector256<ulong> y)
            {
                //return x.Equals(y);
                var compareResult = Avx2.CompareEqual(x, y);
                var result =  Sse41.TestC(compareResult.GetLower(), Vector128<ulong>.AllBitsSet) &&
                    Sse41.TestC(compareResult.GetUpper(), Vector128<ulong>.AllBitsSet);

                return result;
            }
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public int GetHashCode([DisallowNull] Vector256<ulong> obj)
            {
                return obj.GetHashCode();
            }
        }

        internal class VectorComparer128 : IEqualityComparer<Vector256<ulong>>
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public bool Equals(Vector256<ulong> x, Vector256<ulong> y)
            {
                //return x.Equals(y);
                var compareResult = Avx2.CompareEqual(x, y);
                var result = Sse41.TestC(compareResult.GetLower(), Vector128<ulong>.AllBitsSet);
                //if (result != x.Equals(y))
                //    throw new Exception();
                return result;
            }
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public int GetHashCode([DisallowNull] Vector256<ulong> obj)
            {
                return obj.GetLower().GetHashCode();
            }
        }

        internal class VectorComparer64 : IEqualityComparer<Vector256<ulong>>
        {

            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public bool Equals(Vector256<ulong> x, Vector256<ulong> y)
            {

                var result = x.ToScalar<ulong>() == y.ToScalar<ulong>();

                //if (result != x.Equals(y))
                //    throw new Exception();

                //return x.Equals(y);
                return result;
            }
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            public int GetHashCode([DisallowNull] Vector256<ulong> obj)
            {
                return obj.ToScalar<ulong>().GetHashCode();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static Vector256<ulong> MakeTableLookup(Span<byte> componentIds)
        {
            var table = new Vector256<ulong>();

            for(int i = 0; i < componentIds.Length; i++)
            {
                var componentByte = componentIds[i];
                int mod = (componentByte & (64 - 1));
                //power of 2 mod to determine in which element we're putting our bit
                var slot = componentByte >> 6;
                var targetElement = table.GetElement(slot) | (1u << mod);
                table = table.WithElement(slot, targetElement);
            }
    
            return table;
        }


        internal void Clear()
        {
            Array.Clear(_indicesToUIDs, 0, _count);
            _count = 0;

            foreach (var buf in _buffers)
            {
                buf.Clear();
            }
            _emptySlots.Clear();
        }

        private World _world;
        private int[] _components;
        private Dictionary<int, int> _componentLookup;
        public int Count => _count;
        public int RealCount => _count - _emptySlots.Count;
        private int _count = 0;
        private long[] _indicesToUIDs = ArrayPool<long>.Shared.Rent(1024);
        internal SortedSet<int> _emptySlots;
        private ComponentBuffer[] _buffers; //index using _components
        internal int[] ComponentIds => _components;
        internal Type[] ComponentTypes;
        public Table(World world, int[] components)
        {
            _emptySlots = new SortedSet<int>();
            _world = world;
            int bufferCount;

            ComponentTypes = new Type[components.Length];
            for(int i = 0; i < components.Length; i++)
            {
                ComponentTypes[i] = world._components[components[i]].TargetType;
            }

            _componentLookup = new Dictionary<int, int>();
            (_components, bufferCount) = Component.RealComponentCount(components, _world);
            _buffers = new ComponentBuffer[Math.Max(0, bufferCount)];

            for (int i = 0; i < bufferCount; i++)
            {
                _componentLookup.Add(components[i], i);
                _buffers[i] = _world.GetBufferForComponent(_components[i], this);
            }
        }

        protected void Dispose(bool finalizer)
        {
            if (!finalizer)
                GC.SuppressFinalize(this);

            ArrayPool<long>.Shared.Return(_indicesToUIDs, true);
            _indicesToUIDs = null;

            foreach (var buff in _buffers)
            {
                buff.Dispose();
            }
            Array.Clear(_buffers, 0, _buffers.Length);
        }

        ~Table()
        {
            Dispose(true);
        }

        public unsafe struct EntityEnumerator
        {
            public MemoryHandle dataHandle;
            public long* data;
            public uint length;
            public uint current;
            SortedSet<int>.Enumerator emptySlotEnumerator;
            bool hasEmptySlots;
            public EntityEnumerator(Memory<long> data, SortedSet<int>.Enumerator enumerator, uint startCurrent = uint.MaxValue)
            {
                current = startCurrent;
                dataHandle = data.Pin();
                this.data = (long*)dataHandle.Pointer;
                length = (uint)data.Length;
                emptySlotEnumerator = enumerator;
                hasEmptySlots = emptySlotEnumerator.MoveNext();
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                unchecked
                {
                    while (++current < length)
                    {
                        if (hasEmptySlots && emptySlotEnumerator.Current == current)
                        {
                            hasEmptySlots = emptySlotEnumerator.MoveNext();
                            continue;
                        }
                        else
                            return true;
                    }
                    return false;
                }
            }

            public void Dispose()
            {
                dataHandle.Dispose();
            }
        }

        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(new Memory<long>(_indicesToUIDs, 0, _count), _emptySlots.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal void CopyComponentsTo(int oldTableIndex, Table newTable, int newTableIndex)
        {
            for (int i = 0; i < _components.Length; i++)
            {
                if (Component.IsRealComponent(_components[i], _world))
                {
                    if (newTable._componentLookup.TryGetValue(_components[i], out var newTableBufferIndex))
                    {
                        var oldBuffer = _buffers[i];
                        var newBuffer = newTable._buffers[newTableBufferIndex];
                        oldBuffer.MoveTo(oldTableIndex, newBuffer, newTableIndex);
                    }
                }
            }
        }

        public bool HasComponent(int component)
        {
            return Array.IndexOf(_components, component) != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        internal Span<T> GetComponentBuffer<T>(int component)
        {
            return (_buffers[_componentLookup[component]] as ComponentBuffer<T>).Buffer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentBuffer GetUntypedComponentBuffer(int component)
        {
            var componentIndex = Array.IndexOf(_components, component);
            if (componentIndex != -1)
            {
                return _buffers[componentIndex];
            }
            else
                return null;
        }

        internal int IndexForComponent(int component)
        {
            return _componentLookup[component];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ComponentBuffer GetUntypedComponentBufferFromIndex(int index)
        {
            return _buffers[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long GlobalIDFromIndex(uint indexInPool)
        {
            return _indicesToUIDs[indexInPool];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal int AddSlot(long uid)
        {
            int result = 0;
            if (_emptySlots.Count > 0)
            {
                result = _emptySlots.Min;
                _emptySlots.Remove(result);
            }
            else
            {
                result = _count++;
                for (int i = 0; i < _buffers.Length; i++)
                {
                    _buffers[i].AssureRoomFor(result + 1);
                }

                if (result >= _indicesToUIDs.Length)
                {
                    var newArray = ArrayPool<long>.Shared.Rent(_indicesToUIDs.Length * 2);
                    _indicesToUIDs.CopyTo(newArray, 0);
                    ArrayPool<long>.Shared.Return(_indicesToUIDs);
                    _indicesToUIDs = newArray;
                }
            }

            _indicesToUIDs[result] = uid;
            return result;
        }

        internal void Remove(int index)
        {
            foreach (var buffer in _buffers)
                buffer.Remove(index);

            _emptySlots.Add(index);
            _indicesToUIDs[index] = 0;
        }

        public void Sort()
        {

        }

        public void Dispose()
        {
            Dispose(false);
        }
    }
}
