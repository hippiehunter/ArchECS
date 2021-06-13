using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ArchECS
{
    public unsafe class Query : IDisposable
    {
        internal struct QueryStep
        {
            public Table Table;
            public int[] TableComponentIndices;
        }

        internal int[] ComponentIds;
        int ComponentCount;
        internal Type[] Types;
        private int MadeWithTableCount;
        private QueryStep[] Steps;
        public World World;

        public Query(World world, Type[] types)
        {
            InitQuery(world, types);
        }

        protected void Dispose(bool finalizer)
        {
            if (!finalizer)
                GC.SuppressFinalize(this);

            ArrayPool<int>.Shared.Return(ComponentIds);
            ComponentIds = null;

            for (int i = 0; i < Steps.Length; i++)
            {
                ArrayPool<int>.Shared.Return(Steps[i].TableComponentIndices);
                Steps[i].TableComponentIndices = null;
            }
        }

        ~Query()
        {
            Dispose(true);
        }

        private void InitQuery(World world, Type[] types)
        {
            MadeWithTableCount = world.TableCount;
            World = world;
            Types = types;
            ComponentCount = types.Length;
            //get componentids for types
            ComponentIds = ArrayPool<int>.Shared.Rent(ComponentCount);
            for (int i = 0; i < ComponentCount; i++)
            {
                ComponentIds[i] = World.GetComponentID(types[i]);
            }

            //get tables for the components
            var tables = World.GetTablesForComponents(new Span<int>(ComponentIds, 0, ComponentCount));

            Steps = new QueryStep[tables.Count];

            //foreach table, get component indices
            for (int i = 0; i < tables.Count; i++)
            {
                Steps[i].Table = tables[i];
                Steps[i].TableComponentIndices = ArrayPool<int>.Shared.Rent(ComponentCount);
                for (int compoIndex = 0; compoIndex < ComponentCount; compoIndex++)
                {
                    Steps[i].TableComponentIndices[compoIndex] = tables[i].IndexForComponent(ComponentIds[compoIndex]);
                }
            }
        }

        private void MaybeReinit()
        {
            if (MadeWithTableCount != World.TableCount)
                InitQuery(World, Types);
        }

        public struct QueryEnumerator : IEnumerator<ValueTuple<Table, ComponentBuffer[]>>
        {
            QueryStep[] Steps;
            World World;
            int CurrentStep;
            int ComponentCount;
            ComponentBuffer[] CurrentBuffers;
            Table CurrentTable;

            internal QueryEnumerator(QueryStep[] steps, int componentCount, World world)
            {
                ComponentCount = componentCount;
                World = world;
                CurrentStep = -1;
                Steps = steps;
                CurrentTable = null;
                CurrentBuffers = Steps.Length > 0 ? ArrayPool<ComponentBuffer>.Shared.Rent(ComponentCount) : null;
            }

            public bool MoveNext()
            {
                if (++CurrentStep < Steps.Length)
                {
                    ref var step = ref Steps[CurrentStep];
                    CurrentTable = step.Table;
                    for (int i = 0; i < ComponentCount; i++)
                    {
                        CurrentBuffers[i] = CurrentTable.GetUntypedComponentBufferFromIndex(Steps[CurrentStep].TableComponentIndices[i]);
                    }
                    return true;
                }
                else
                {
                    if (CurrentBuffers != null)
                    {
                        ArrayPool<ComponentBuffer>.Shared.Return(CurrentBuffers);
                        CurrentBuffers = null;
                    }
                    return false;
                }
            }

            public void Reset()
            {
                CurrentStep = -1;
                CurrentBuffers = CurrentBuffers == null ? (Steps.Length > 0 ? ArrayPool<ComponentBuffer>.Shared.Rent(ComponentCount) : null) : CurrentBuffers;
            }

            public void Dispose()
            {
                if (CurrentBuffers != null)
                {
                    ArrayPool<ComponentBuffer>.Shared.Return(CurrentBuffers);
                    CurrentBuffers = null;
                }
            }

            public (Table, ComponentBuffer[]) Current
            {
                get
                {
                    return (CurrentTable, CurrentBuffers);
                }
            }

            object IEnumerator.Current => Current;
        }

        public QueryEnumerator GetEnumerator()
        {
            return new QueryEnumerator(Steps, ComponentCount, this.World);
        }


        public delegate void EachDelegate<T1>(long entitiy, ref T1 t1);
        public delegate void EachDelegate<T1, T2>(long entitiy, ref T1 t1, ref T2 t2);
        public delegate void EachDelegate<T1, T2, T3>(long entitiy, ref T1 t1, ref T2 t2, ref T3 t3);
        public delegate void EachDelegate<T1, T2, T3, T4>(long entitiy, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);

        public delegate RETURN TransformDelegate<T1, RETURN>(in long entitiy, ref T1 t1);
        public delegate RETURN TransformDelegate<T1, T2, RETURN>(in long entitiy, ref T1 t1, ref T2 t2);
        public delegate RETURN TransformDelegate<T1, T2, T3, RETURN>(in long entitiy, ref T1 t1, ref T2 t2, ref T3 t3);
        public delegate RETURN TransformDelegate<T1, T2, T3, T4, RETURN>(in long entitiy, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);

        public delegate bool RemovalDelegate<T1, R>(in long entitiy, ref T1 t1);
        public delegate bool RemovalDelegate<T1, T2, R>(in long entitiy, ref T1 t1, ref T2 t2);
        public delegate bool RemovalDelegate<T1, T2, T3, R>(in long entitiy, ref T1 t1, ref T2 t2, ref T3 t3);
        public delegate bool RemovalDelegate<T1, T2, T3, T4, R>(in long entitiy, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);

        struct SplitSet
        {
            public uint startIndex;
            public uint endIndex;
            public Table table;
            public ComponentBuffer[] buffers;
        }
        public unsafe RETURN[] ParallelTransform<T1, RETURN>(TransformDelegate<T1, RETURN> each, int maxDegreeOfParallelism = int.MaxValue)
        {
            MaybeReinit();
            var parallelFactor = Math.Min(Environment.ProcessorCount, maxDegreeOfParallelism);
            var t1BufIndex = Array.IndexOf(Types, typeof(T1));

            uint elementCount = 0;
            foreach (var (table, buffers) in this)
            {
                elementCount += (uint)table.Count;
            }

            var unitSize = Math.Max(elementCount / parallelFactor, 1);
            //allocate the maximum split sets we might need
            //then fill them, startIndex, endIndex, table, buffers
            //startIndex, endIndex into the split sets one per parallelFactor
            uint currentSplit = 0;
            uint currentElementCount = 0;
            var parallelIndicies = stackalloc ValueTuple<uint, uint>[parallelFactor];
            parallelIndicies[0].Item1 = 0;
            parallelIndicies[0].Item2 = 0;
            var splits = ArrayPool<SplitSet>.Shared.Rent(parallelFactor * 2);
            foreach (var (table, buffers) in this)
            {
                if(splits[currentSplit].table != table)
                {
                    currentSplit++;
                    ref var splitElement = ref splits[currentSplit];
                    splitElement.table = table;
                    splitElement.buffers = buffers;
                    splitElement.startIndex = 0;
                    splitElement.endIndex = 0;
                    currentElementCount = 0;
                }

                var remainingCapacity = unitSize - currentElementCount;
                var takeCount = Math.Min(remainingCapacity, table.Count);
                
            }

            foreach (var (table, buffers) in this)
            {
                var entItr = table.GetEnumerator();
                var t1 = (buffers[t1BufIndex] as ComponentBuffer<T1>);
                while (entItr.MoveNext())
                {
                    each(entItr.data[entItr.current], ref t1[entItr.current]);
                }
                entItr.Dispose();
            }
            //use this result array one chunk per parallel factor
            var resultArray = ArrayPool<RETURN>.Shared.Rent((int)elementCount);

            //iterate over the sorted result array chunks in order
            //anytime we see the elements match up we merge all of the matches togeather
            //clear the non first element
            //keep track of the total actual result items
            //allocate real result array
            //re iterate over the sorted result assigning the elements into the real result array
            return resultArray;
        }

        public void Each<T1>(EachDelegate<T1> each)
        {
            MaybeReinit();
            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            foreach (var (table, buffers) in this)
            {
                var entItr = table.GetEnumerator();
                var t1 = (buffers[t1BufIndex] as ComponentBuffer<T1>);
                while (entItr.MoveNext())
                {
                    each(entItr.data[entItr.current], ref t1[entItr.current]);
                }
                entItr.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Each<T1, T2>(EachDelegate<T1, T2> each)
        {
            MaybeReinit();
            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            var t2BufIndex = Array.IndexOf(Types, typeof(T2));
            foreach (var (table, buffers) in this)
            {
                var entItr = table.GetEnumerator();
                var t1 = (buffers[t1BufIndex] as ComponentBuffer<T1>);
                var t2 = (buffers[t2BufIndex] as ComponentBuffer<T2>);
                while (entItr.MoveNext())
                {
                    each(entItr.data[entItr.current], ref t1[entItr.current], ref t2[entItr.current]);
                }
                entItr.Dispose();
            }
        }

        public void Each<T1, T2, T3>(EachDelegate<T1, T2, T3> each)
        {
            MaybeReinit();
            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            var t2BufIndex = Array.IndexOf(Types, typeof(T2));
            var t3BufIndex = Array.IndexOf(Types, typeof(T3));
            foreach (var (table, buffers) in this)
            {
                var entItr = table.GetEnumerator();
                var t1 = (buffers[t1BufIndex] as ComponentBuffer<T1>);
                var t2 = (buffers[t2BufIndex] as ComponentBuffer<T2>);
                var t3 = (buffers[t3BufIndex] as ComponentBuffer<T3>);
                while (entItr.MoveNext())
                {
                    unchecked
                    {
                        each(entItr.data[entItr.current], ref t1[entItr.current], ref t2[entItr.current], ref t3[entItr.current]);
                    }
                }
                entItr.Dispose();
            }
        }

        public void Each<T1, T2, T3, T4>(EachDelegate<T1, T2, T3, T4> each)
        {
            MaybeReinit();
            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            var t2BufIndex = Array.IndexOf(Types, typeof(T2));
            var t3BufIndex = Array.IndexOf(Types, typeof(T3));
            var t4BufIndex = Array.IndexOf(Types, typeof(T4));
            foreach (var (table, buffers) in this)
            {
                var entItr = table.GetEnumerator();
                var t1 = (buffers[t1BufIndex] as ComponentBuffer<T1>);
                var t2 = (buffers[t2BufIndex] as ComponentBuffer<T2>);
                var t3 = (buffers[t3BufIndex] as ComponentBuffer<T3>);
                var t4 = (buffers[t4BufIndex] as ComponentBuffer<T4>);
                while (entItr.MoveNext())
                {
                    each(entItr.data[entItr.current], ref t1[entItr.current], ref t2[entItr.current], ref t3[entItr.current], ref t4[entItr.current]);
                }
                entItr.Dispose();
            }
        }
        
        public struct SingleTypeQueryEnumerator<T>
        {
            internal int BufIndex;
            internal QueryEnumerator StepEnumerator;
            internal Table.EntityEnumerator EntityEnumerator;
            internal ComponentBuffer<T> CurrentBuffer;

            internal bool MoveNextInternal()
            {
                if (StepEnumerator.MoveNext())
                {
                    var (table, buffers) = StepEnumerator.Current;
                    CurrentBuffer = buffers[BufIndex] as ComponentBuffer<T>;
                    EntityEnumerator = table.GetEnumerator();
                    return true;
                }
                else
                {
                    BufIndex = -1;
                    return false;
                }
            }

            public bool MoveNext()
            {
                if (BufIndex == -1)
                    return false;

                if (!EntityEnumerator.MoveNext())
                {
                    if (!MoveNextInternal())
                    {
                        return false;
                    }
                    else
                    {
                        return EntityEnumerator.MoveNext();
                    }
                }
                else
                    return true;
            }

            public T Current
            {
                get
                {
                    return CurrentBuffer[EntityEnumerator.current];
                }
            }
        }

        public struct QueryEnumerable<T>
        {
            internal int bufferIndex;
            internal Query query;
            public SingleTypeQueryEnumerator<T> GetEnumerator()
            {
                var result = new SingleTypeQueryEnumerator<T> { BufIndex = bufferIndex, StepEnumerator = query.GetEnumerator() };
                result.MoveNextInternal();
                return result;
            }
        }

        public QueryEnumerable<T> SingleTypeEnumerable<T>()
        {
            MaybeReinit();
            var t1BufIndex = Array.IndexOf(Types, typeof(T));
            return new QueryEnumerable<T> { bufferIndex = t1BufIndex, query = this };
        }

        public void Dispose()
        {
            Dispose(false);
        }
    }
}
