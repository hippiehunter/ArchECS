using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        public EntityHandle(long id, World world)
        {
            World.AllocationId aid = new World.AllocationId { full = id, generation = 1 };
            Id = aid.full;
            World = world;
        }
    }

    public interface IEntityWrapper
    {
        public ref Entity Value(World world);
        public long Id();
        public bool IsValid { get; }
    }

    public static class EntityWrapperExtensions
    {
        public static EntityWrapper<T> Cast<T, WrapperType>(this WrapperType wrapper, World world) where T : struct where WrapperType : IEntityWrapper
        {
            if (!wrapper.Value(world).HasComponent<T>())
                throw new InvalidCastException();

            return new EntityWrapper<T>(wrapper.Id());
        }

        public static EntityWrapper<T1, T2> Cast<T1, T2, WrapperType>(this WrapperType wrapper, World world) where T1 : struct where T2 : struct where WrapperType : IEntityWrapper
        {
            if (!wrapper.Value(world).HasComponent<T1>() || !wrapper.Value(world).HasComponent<T2>())
                throw new InvalidCastException();

            return new EntityWrapper<T1, T2>(wrapper.Id());
        }
        public static EntityWrapper<T1, T2> MaybeCast<T1, T2, WrapperType>(this WrapperType wrapper, World world) where T1 : struct where T2 : struct where WrapperType : IEntityWrapper
        {
            if (!wrapper.IsValid || !wrapper.Value(world).HasComponent<T1>() || !wrapper.Value(world).HasComponent<T2>())
                return new EntityWrapper<T1, T2> { Id = 0 };

            return new EntityWrapper<T1, T2>(wrapper.Id());
        }

        public static EntityWrapper<T1, T2> Cast<T1, T2>(this EntityWrapper<T1> wrapper, World world) where T1 : struct where T2 : struct 
        {
            return Cast<T1, T2, EntityWrapper<T1>>(wrapper, world);
        }

        public static EntityWrapper<T> Cast<T>(this EntityWrapper wrapper, World world) where T : struct
        {
            return Cast<T, EntityWrapper>(wrapper, world);
        }

        public static EntityWrapper<T> UnsafeCast<T, WrapperType>(this WrapperType wrapper) where T : struct where WrapperType : IEntityWrapper
        {
            return new EntityWrapper<T>(wrapper.Id());
        }

        public static EntityWrapper<T> UnsafeCast<T>(this EntityWrapper wrapper) where T : struct
        {
            return new EntityWrapper<T>(wrapper.Id);
        }

        public static EntityWrapper<T1, T2> UnsafeCast<T1, T2, WrapperType>(this WrapperType wrapper) where T1 : struct where T2 : struct where WrapperType : IEntityWrapper
        {
            return new EntityWrapper<T1, T2>(wrapper.Id());
        }

        public static EntityWrapper<T1, T2> UnsafeCast<T1, T2>(this EntityWrapper<T1> wrapper) where T1 : struct where T2 : struct
        {
            return new EntityWrapper<T1, T2>(wrapper.Id);
        }

        public static EntityWrapper RemoveType<WrapperType>(this WrapperType wrapper) where WrapperType : IEntityWrapper
        {
            return new EntityWrapper(wrapper.Id());
        }

        public static bool TryCast<T, WrapperType>(this WrapperType wrapper, World world, out EntityWrapper<T> result) where T : struct where WrapperType : IEntityWrapper
        {
            ref var entity = ref wrapper.Value(world);
            if (entity.HasComponent<T>())
            {
                result = wrapper.UnsafeCast<T, WrapperType>();
                return true;
            }
            else
            {
                result = default(EntityWrapper<T>);
                return false;
            }
        }

        public static bool TryCast<T1, T2, WrapperType>(this WrapperType wrapper, World world, out EntityWrapper<T1, T2> result) where T1 : struct where T2 : struct where WrapperType : IEntityWrapper
        {
            ref var entity = ref wrapper.Value(world);
            if (entity.HasComponent<T1>() && entity.HasComponent<T2>())
            {
                result = wrapper.UnsafeCast<T1, T2, WrapperType>();
                return true;
            }
            else
            {
                result = default(EntityWrapper<T1, T2>);
                return false;
            }
        }

        public static bool TryCast<T>(this EntityWrapper wrapper, World world, out EntityWrapper<T> result) where T : struct
        {
            return TryCast<T, EntityWrapper>(wrapper, world, out result);
        }

        public static bool TryCast<T1, T2>(this EntityWrapper wrapper, World world, out EntityWrapper<T1, T2> result) where T1 : struct where T2 : struct
        {
            return TryCast<T1, T2, EntityWrapper>(wrapper, world, out result);
        }

        public static bool HasComponent<T, WrapperType>(this WrapperType wrapper, World world) where T : struct where WrapperType : IEntityWrapper
        {
            ref var entity = ref wrapper.Value(world);
            return entity.HasComponent<T>();
        }

        public static bool HasComponent<T>(this EntityWrapper wrapper, World world) where T : struct
        {
            ref var entity = ref wrapper.Value(world);
            return entity.HasComponent<T>();
        }
    }
    [DebuggerTypeProxy(typeof(EntityWrapperDebugProxy))]
    public struct EntityWrapper : IEntityWrapper
    {
        public EntityWrapper(long id)
        {
            World.AllocationId aid = new World.AllocationId { full = id, generation = 1 };
            Id = aid.full;
        }
        public long Id;
        public ref Entity Value(World world)
        {
            return ref world[Id];
        }

        public ref T UnsafeValue<T>(World world) where T : struct
        {
            return ref world[Id].GetComponentRef<T>();
        }

        long IEntityWrapper.Id()
        {
            return Id;
        }


        public bool IsValid => Id != 0;
    }
    [DebuggerTypeProxy(typeof(EntityWrapperDebugProxy))]
    public struct EntityWrapper<T> : IEntityWrapper where T : struct
    {
        public EntityWrapper(long id)
        {
            World.AllocationId aid = new World.AllocationId { full = id, generation = 1 };
            Id = aid.full;
        }

        public long Id;

        public ref Entity UntypedValue(World world)
        {
            return ref world[Id];
        }

        public ref T Value(World world)
        {
            return ref world[Id].GetComponentRef<T>();
        }

        ref Entity IEntityWrapper.Value(World world)
        {
            return ref UntypedValue(world);
        }

        long IEntityWrapper.Id()
        {
            return Id;
        }
        public bool IsValid => Id != 0;
    }

    [DebuggerTypeProxy(typeof(EntityWrapperDebugProxy))]
    public struct EntityWrapper<T1, T2> : IEntityWrapper where T1 : struct where T2 : struct
    {
        public EntityWrapper(long id)
        {
            World.AllocationId aid = new World.AllocationId { index = (int)id, generation = 1 };
            Id = aid.full;
        }

        public long Id;

        public ref Entity UntypedValue(World world)
        {
            return ref world[Id];
        }

        public ref T Value<T>(World world)
        {
            if (typeof(T) != typeof(T1) && typeof(T) != typeof(T2))
                throw new InvalidOperationException();

            return ref world[Id].GetComponentRef<T>();
        }

        ref Entity IEntityWrapper.Value(World world)
        {
            return ref UntypedValue(world);
        }

        long IEntityWrapper.Id()
        {
            return Id;
        }
        public bool IsValid => Id != 0;
    }

    public class EntityWrapperDebugProxy
    {
        IEntityWrapper _wrapper;
        public EntityWrapperDebugProxy(IEntityWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Values
        {
            get
            {
                try
                {
                    if (_wrapper.Id() != 0 && World.ActiveWorld != null)
                    {
                        var currentWorld = World.ActiveWorld;
                        ref var targetEntity = ref currentWorld[_wrapper.Id()];
                        var targetTable = currentWorld.TableFromId(targetEntity.TableId);
                        var results = new object[targetTable.ComponentTypes.Length + 2];
                        for (int i = 0; i < targetTable.ComponentTypes.Length; i++)
                        {
                            var component = targetTable.GetUntypedComponentBufferFromIndex(i);
                            results[i] = component.BoxedValue((int)targetEntity.TableIndex);
                        }

                        results[results.Length - 2] = targetEntity;
                        results[results.Length - 1] = _wrapper.Id();

                        return results;
                    }
                }
                catch { }
                return new object[] { _wrapper.Id() };
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

        public ref T GetOrAddComponentRef<T>() where T : new()
        {
            var buffer = World.GetBufferForComponent<T>(TableId);
            if (buffer == null)
            {
                buffer = AddComponentInternal<T>();
                buffer[TableIndex] = new T();
            }
                
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
            if(buffer.TriggerEvents)
                buffer.TriggerSet(this, TableIndex);

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
            var (newTable, newTableId) = World.TableFromKey(Table.MakeTableLookup(newComponentIds), newComponentIds);
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
                else
                {
                    var oldBuffer = oldTable.GetUntypedComponentBufferFromIndex(i);
                    if (oldBuffer.TriggerEvents)
                        oldBuffer.TriggerRemoval(this, indexInOldPool);
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
