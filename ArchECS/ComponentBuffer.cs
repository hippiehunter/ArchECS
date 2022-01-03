using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ArchECS
{
    public abstract class ComponentBuffer : IDisposable
    {
        public bool TriggerEvents;
        public abstract object BoxedValue(int index);
        public abstract void AssureRoomFor(int count);
        public abstract void Remove(int index);
        public abstract void MoveTo(int index, ComponentBuffer target, int targetIndex);
        internal abstract ComponentBuffer MakeNewBuffer(Table table);

        internal abstract void Clear();

        public abstract void Dispose();
        internal abstract void TriggerRemoval(in Entity entity, uint index);
    }


    internal unsafe sealed class ComponentBuffer<T> : ComponentBuffer
    {
        byte* _dataPtr;
        MemoryHandle _dataHandle;
        Memory<T> _dataMem;
        T[] _data;
        Table _table;
        private int Count => _table.Count;
        public ComponentBuffer(int size, Table table)
        {
            _table = table;
            _data = ArrayPool<T>.Shared.Rent(size);
            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _dataMem = new Memory<T>(_data);
                _dataHandle = _dataMem.Pin();
                _dataPtr = (byte*)_dataHandle.Pointer;
            }
            UpdateBuffer(size);
        }

        public override void Dispose()
        {
            Dispose(false);
        }

        void Dispose(bool finalizer)
        {
            if (!finalizer)
                GC.SuppressFinalize(this);

            if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _dataHandle.Dispose();
                _dataPtr = null;
                _dataMem = default(Memory<T>);
            }

            ArrayPool<T>.Shared.Return(_data, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _data = null;
        }

        ~ComponentBuffer()
        {
            Dispose(true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateBuffer(int newSizeInBytes)
        {
            if (_data.Length < newSizeInBytes)
            {
                var newArray = ArrayPool<T>.Shared.Rent(newSizeInBytes);
                if (Count > 0)
                {
                    Buffer().CopyTo(newArray.AsSpan(0, Count));
                }

                if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    _dataHandle.Dispose();
                    _dataMem = new Memory<T>(newArray);
                    _dataHandle = _dataMem.Pin();
                    _dataPtr = (byte*)_dataHandle.Pointer;
                }

                //only need to clear the array if there are references that could be held
                //this evaluates to a constant in the JIT
                ArrayPool<T>.Shared.Return(_data, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                _data = newArray;
            }
        }

        
        public ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization| MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    return ref Unsafe.AsRef<T>(_dataPtr + (Unsafe.SizeOf<T>() * index));
                }
                else
                {
                    return ref _data[index];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public Span<T> Buffer()
        {
#if DEBUG
            if (Count < 0)
                throw new NotImplementedException();
#endif
            return new Span<T>(_data, 0, Count);
        }

        public override void Remove(int index)
        {
#if DEBUG
            if (index > Count)
                throw new IndexOutOfRangeException();
#endif
            _data[index] = default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override void AssureRoomFor(int quantity)
        {
#if DEBUG
            if (Count < 0)
                throw new NotImplementedException();
#endif

            if (quantity >= _data.Length)
                UpdateBuffer(quantity * 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void AssureRoomForMore(int quantity)
        {
#if DEBUG
            if (Count < 0)
                throw new NotImplementedException();
#endif

            if (quantity + Count >= _data.Length)
                UpdateBuffer((quantity + Count) * 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Set(in T element, int position)
        {
            _data[position] = element;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void CopyElement(int src, int dst)
        {
            _data[dst] = _data[src];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public override void MoveTo(int index, ComponentBuffer target, int targetIndex)
        {
            var typedTarget = target as ComponentBuffer<T>;
            //the target is supposed to already be sized
            //typedTarget.AssureRoomForMore(1);
            typedTarget._data[targetIndex] = _data[index];
        }

        internal override ComponentBuffer MakeNewBuffer(Table table)
        {
            return new ComponentBuffer<T>(256, table);
        }

        public ComponentBufferEnumerator GetEnumerator()
        {
            return new ComponentBufferEnumerator(Buffer(), _table._emptySlots.GetEnumerator());
        }

        public ComponentBufferEnumerator GetEnumerator(uint startIndex, uint endIndex)
        {
            return new ComponentBufferEnumerator(Buffer(), _table._emptySlots.GetEnumerator(), startIndex, endIndex);
        }

        internal override void Clear()
        {
            Array.Clear(_data, 0, _data.Length);
        }

        public override object BoxedValue(int index)
        {
            return this[(uint)index];
        }

        internal override void TriggerRemoval(in Entity entity, uint index)
        {
            
        }

        internal void TriggerSet(in Entity entity, uint index)
        {
            
        }

        public ref struct ComponentBufferEnumerator
        {
            Span<T> data;
            internal uint CurrentIndex;
            internal uint EndIndex;
            SortedSet<int>.Enumerator emptySlotEnumerator;
            bool hasEmptySlots;
            public ComponentBufferEnumerator(Span<T> data, SortedSet<int>.Enumerator enumerator) : this()
            {
                this.data = data;
                emptySlotEnumerator = enumerator;
                hasEmptySlots = emptySlotEnumerator.MoveNext();
                CurrentIndex = 0;
                EndIndex = (uint)data.Length;
            }

            public ComponentBufferEnumerator(Span<T> data, SortedSet<int>.Enumerator enumerator, uint startIndex, uint endIndex) : this()
            {
                this.data = data;
                emptySlotEnumerator = enumerator;
                CurrentIndex = startIndex;
                hasEmptySlots = emptySlotEnumerator.MoveNext();
                CurrentIndex = startIndex;
                EndIndex = endIndex;
            }

            public bool MoveNext()
            {
                while(++CurrentIndex < EndIndex)
                {
                    if (hasEmptySlots && emptySlotEnumerator.Current == CurrentIndex)
                    {
                        hasEmptySlots = emptySlotEnumerator.MoveNext();
                        continue;
                    }
                    else
                        return true;
                }
                return false;
            }

            public ref T Current
            {
                get
                {
                    return ref data[(int)CurrentIndex];
                }
            }
        }
    }
}
