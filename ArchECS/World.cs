﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchECS
{
    public class World : IDisposable
    {
        internal Component[] _components = new Component[256];
        Dictionary<Type, int> _componentIndex = new Dictionary<Type, int>();
        Table[] _tables;
        int _tableCount = 0;
        Dictionary<Table.TableLookup, int> _tableLookup = new Dictionary<Table.TableLookup, int>(new Table.TableLookupComparer());
        private Entity[] _entities = ArrayPool<Entity>.Shared.Rent(1024);
        private int _currentId = 0;
        private Queue<long> _reuseIds = new Queue<long>();
        internal Table ZeroTable()
        {
            return _tables[0];
        }

        internal int TableCount => _tableCount;

        public World()
        {
            _tables = ArrayPool<Table>.Shared.Rent(1024);
            _tables[0] = new Table(this, new int[0]);
            _tableCount = 1;
            _tableLookup.Add(new Table.TableLookup(), 0);
        }

        public void Reset()
        {
            Array.Clear(_entities, 0, _currentId);
            _currentId = 0;

            for(int i = 0; i < _tableCount; i++)
            {
                _tables[i].Clear();
            }

            _reuseIds.Clear();

        }

        protected void Dispose(bool finalizer)
        {
            if (!finalizer)
                GC.SuppressFinalize(this);

            ArrayPool<Entity>.Shared.Return(_entities, true);
            _entities = null;

            for (int i = 0; i < _tableCount; i++)
            {
                _tables[i].Dispose();
                _tables[i] = null;
            }
        }

        ~World()
        {
            Dispose(true);
        }

        const long indexMask = 0x000ffff0;

        internal List<Table> GetTablesForComponents(Span<int> componentIds)
        {
            var result = new List<Table>();
            HashSet<int> tables = new HashSet<int>();
            foreach (var componentId in componentIds)
            {
                foreach (var tableId in _components[componentId].MemberOfTables)
                {
                    if (!tables.Contains(tableId))
                        tables.Add(tableId);
                }
            }

            foreach(var table in tables)
            {
                var realTable = TableFromId(table);
                bool hasAllComponents = true;
                foreach (var componentId in componentIds)
                {
                    if (!realTable.HasComponent(componentId))
                    {
                        hasAllComponents = false;
                        break;
                    }
                }

                if(hasAllComponents)
                {
                    result.Add(realTable);
                }
            }
            return result;
        }

        internal (Table newTable, int newTableId) TableFromKey(in Table.TableLookup tableLookup, Span<byte> componentIds)
        {
            if(_tableLookup.TryGetValue(tableLookup, out var newTableId))
            {
                return (_tables[newTableId], newTableId);
            }
            else
            {
                newTableId = _tableCount++;
                var newComponentIds = new int[componentIds.Length];
                for (int i = 0; i < componentIds.Length; i++)
                    newComponentIds[i] = componentIds[i];

                var newTable = new Table(this, newComponentIds);
                _tables[newTableId] = newTable;
                foreach (var component in newComponentIds)
                {
                    _components[component].MemberOfTables.Add(newTableId);
                }
                _tableLookup.Add(tableLookup, newTableId);
                return (newTable, newTableId);
            }
        }

        public unsafe long CreateEntity()
        {
            var newId = AllocateEntityId();
            var ptr = (AllocationId*)&newId;
            ref var targetEntity = ref _entities[ptr->index];

            targetEntity.TableId = 0;
            targetEntity.TableIndex = (uint)ZeroTable().AddSlot(newId);
            targetEntity.Id = newId;
            targetEntity.World = this;
            return newId;
        }

        public EntityHandle CreateEntityHandle()
        {
            return new EntityHandle { Id = CreateEntity(), World = this };
        }

        public void RegisterComponent<T>()
        {
            if (!_componentIndex.ContainsKey(typeof(T)))
            {
                var i = ComponentRegistry.ComponentId<T>.Index;
                if (i > 255)
                {
                    throw new ApplicationException("Too many components");
                }
                _components[i] = Component.MakeComponent<T>(this);
                _componentIndex.Add(typeof(T), i);
            }
            
        }

        const long generationMask = 0x0000000f;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        unsafe internal ref AllocationId DecomposeIndex(long index)
        {
            unchecked
            {
                var ptr = (AllocationId* )&index;
                return ref *ptr;
            }
        }

        
        public ref Entity this[long index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get
            {
                ref var result = ref _entities[DecomposeIndex(index).index];
                if (result.Id != index)
                    throw new InvalidOperationException();

                return ref result;
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal unsafe ref Entity InternalIndex(ulong index)
        {
            unchecked
            {
                return ref _entities[(uint)index];
            }
        }
        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(_entities, (uint)_currentId);
        }

        public ref struct EntityEnumerator
        {
            Entity[] entities;
            uint index;
            uint maxId;
            internal EntityEnumerator(Entity[] internalEntities, uint maxId)
            {
                this.entities = internalEntities;
                this.index = UInt32.MaxValue;
                this.maxId = maxId;
            }

            public bool MoveNext()
            {
                unchecked
                {
                    while (++index < maxId)
                    {
                        if (entities[index].Id != 0)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public ref Entity Current => ref entities[index];
        }

        internal Table TableFromId(int tableId)
        {
            return _tables[tableId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal ComponentBuffer<T> GetBufferForComponent<T>(int tableId)
        {
            var table = _tables[tableId];
            return table.GetUntypedComponentBuffer(ComponentRegistry.ComponentId<T>.Index) as ComponentBuffer<T>;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal ComponentBuffer GetBufferForComponent(int tableId, Type ty)
        {
            var table = _tables[tableId];
            return table.GetUntypedComponentBuffer(_componentIndex[ty]);
        }

        internal bool HasComponent<T>(int tableId)
        {
            var table = _tables[tableId];
            for (int i = 0; i < _components.Length; i++)
            {
                if (_components[i].TargetType == typeof(T))
                {
                    return true;
                }
                else if (_components[i].TargetType == null)
                    break;
            }
            return false;
        }

        internal int GetComponentID<T>()
        {
            return ComponentRegistry.ComponentId<T>.Index;
        }

        internal int GetComponentID(Type ty)
        {
            if (_componentIndex.TryGetValue(ty, out var componentIndex))
            {
                return componentIndex;
            }
            else
                throw new IndexOutOfRangeException();
        }

        internal ComponentBuffer GetBufferForComponent(int componentId, Table table)
        {
            return _components[componentId].BufferPrototype.MakeNewBuffer(table);
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        internal struct AllocationId
        {
            [FieldOffset(0)]
            public long full;
            [FieldOffset(5)]
            public byte generation;
            [FieldOffset(0)]
            public int index;
        }

        private unsafe long AllocateEntityId()
        {
            unchecked
            {
                long newId = 0;
                if (!_reuseIds.TryDequeue(out newId))
                {
                    newId = Interlocked.Increment(ref _currentId);
                    if (_entities.Length <= newId)
                    {
                        lock (this)
                        {
                            if (_entities.Length <= newId)
                            {
                                var newArray = ArrayPool<Entity>.Shared.Rent((int)newId * 2);
                                _entities.CopyTo(newArray, 0);
                                ArrayPool<Entity>.Shared.Return(_entities);
                                _entities = newArray;

                            }
                        }
                    }
                    return newId;
                }
                else
                {
                    (*(((byte*)&newId) + 5))++;
                    return newId;
                }
            }
        }

        public void DestroyEntity(long entityUID)
        {
            throw new NotImplementedException();
            //var dsEntityData = UIDsToEntityDatas[entityUID];
            //ArchetypePool pool = archetypePools_[dsEntityData.ArchetypeFlags];
            //var replacerUID = pool.Remove(dsEntityData.IndexInPool);
            //UIDsToEntityDatas[replacerUID] = dsEntityData;
            //reuseIds.Enqueue(entityUID);
        }

        public void Dispose()
        {
            Dispose(false);
        }
    }
}