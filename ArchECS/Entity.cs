using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArchECS
{
    public struct EntityHandle
    {
        public World World;
        public long Id;
        public ref Entity Value
        {
            get
            {
                return ref World[Id];
            }
        }
    }

    //never store an entity anywhere other than inside world
    public struct Entity
    {
        const long generationMask = 0xfffffff0;
        public long Id;
        internal uint TableIndex;
        internal int TableId;
        public World World;

        internal Entity(long Id, int tableIndex, int TableId, World world)
        {
            this.Id = Id;
            this.World = world;
            this.TableId = 0;
            this.TableIndex = (uint)tableIndex;
        }

        public Type[] Components => World.TableFromId(TableId).ComponentTypes;

        public T GetComponent<T>()
        {
            var buffer = World.GetBufferForComponent<T>(TableId);
            if (buffer == null)
                return default(T);
            else
                return buffer[TableIndex];
        }

        public ref T GetComponentRef<T>()
        {
            var buffer = World.GetBufferForComponent<T>(TableId);
            if (buffer == null)
                throw new InvalidOperationException();
            else
                return ref buffer[TableIndex];
        }

        public object GetComponent(Type ty)
        {
            return World.GetBufferForComponent(TableId, ty).BoxedValue((int)TableIndex);
        }

        public bool HasComponent<T>()
        {
            return World.HasComponent<T>(TableId);
        }

        public ref T SetComponent<T>(in T t)
        {
            return ref SetComponentInternal(t);
        }

        public ref T SetComponentInternal<T>(in T t)
        {
            var buffer = World.GetBufferForComponent<T>(TableId) ?? AddComponentInternal<T>();
            buffer[TableIndex] = t;
            return ref buffer[TableIndex];
        }

        public void SetComponent(object t)
        {
            AddComponentInternalHandle.MakeGenericMethod(new Type[] { t.GetType() }).Invoke(this, new object[] { t });
        }

        public ref T AddComponent<T>()
        {
            return ref AddComponentInternal<T>()[TableIndex];
        }

        private static MethodInfo SetComponentInternalHandle = typeof(Entity).GetMethod("SetComponentInternal");
        private static MethodInfo AddComponentInternalHandle = typeof(Entity).GetMethod("AddComponentInternal");
        public void AddComponent(Type ty)
        {
            AddComponentInternalHandle.MakeGenericMethod(new Type[] { ty }).Invoke(this, null);
        }

        private unsafe ComponentBuffer<T> AddComponentInternal<T>()
        {
            var oldTable = World.TableFromId(TableId);
            var indexInOldPool = TableIndex;
            var oldComponentIds = oldTable.ComponentIds;
            Span<byte> newComponentIds = stackalloc byte[oldComponentIds.Length + 1];
            for (int i = 0; i < oldComponentIds.Length; i++)
            {
                newComponentIds[i] = (byte)oldComponentIds[i];
            }
            var newComponentId = World.GetComponentID<T>();
            newComponentIds[oldComponentIds.Length] = (byte)newComponentId;
            var (newTable, newTableIndex) = MoveToNewTable(oldTable, (int)indexInOldPool, oldComponentIds, newComponentIds);
            return newTable.GetUntypedComponentBuffer(newComponentId) as ComponentBuffer<T>;
        }

        private unsafe (Table newTable, int newTableIndex) MoveToNewTable(Table oldTable, int indexInOldPool, int[] oldComponentIds, Span<byte> newComponentIds)
        {
            var (newTable, newTableId) = World.TableFromKey(new Table.TableLookup(newComponentIds), newComponentIds);
            var newTableIndex = newTable.AddSlot(Id);
            oldTable.CopyComponentsTo(indexInOldPool, newTable, newTableIndex);
            TableId = newTableId;
            TableIndex = (uint)newTableIndex;
            return (newTable, newTableIndex);
        }

        public void RemoveComponent<T>()
        {
            RemoveComponent(World.GetComponentID<T>());
        }

        public void RemoveComponent(Type ty)
        {
            RemoveComponent(World.GetComponentID(ty));
        }

        private void RemoveComponent(int componentId)
        {
            var oldTable = World.TableFromId(TableId);
            var indexInOldPool = TableIndex;
            var oldComponentIds = oldTable.ComponentIds;
            Span<byte> newComponentIds = stackalloc byte[oldComponentIds.Length - 1];
            var removeId = (byte)componentId;
            for (int i = 0, r = 0; i < oldComponentIds.Length; i++)
            {
                if (oldComponentIds[i] != removeId)
                {
                    newComponentIds[r++] = (byte)oldComponentIds[i];
                }
            }
            MoveToNewTable(oldTable, (int)indexInOldPool, oldComponentIds, newComponentIds);
        }

        public byte Generation()
        {
            unchecked
            {
                return (byte)(Id);
            }
        }

        public void IncrementGeneration()
        {
            var currentGeneration = Generation();
            var newGeneration = currentGeneration++;
            Id = (Id & generationMask) | newGeneration;
        }
    }

    public struct Named
    {
        public string Name { get; set; }
    }
}
