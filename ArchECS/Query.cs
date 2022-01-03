using ArchECS.Collections;
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
                CurrentBuffers = null;
            }

            public bool MoveNext()
            {
                if (++CurrentStep < Steps.Length)
                {
                    ref var step = ref Steps[CurrentStep];
                    CurrentTable = step.Table;
                    CurrentBuffers = new ComponentBuffer[ComponentCount];
                    for (int i = 0; i < ComponentCount; i++)
                    {
                        CurrentBuffers[i] = CurrentTable.GetUntypedComponentBufferFromIndex(Steps[CurrentStep].TableComponentIndices[i]);
                    }
                    return true;
                }
                else
                {
                    CurrentBuffers = null;
                    return false;
                }
            }

            public void Reset()
            {
                CurrentStep = -1;
                CurrentBuffers = null;
            }

            public void Dispose()
            {

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

        public delegate void TransformManyDelegate<T1, RETURN, LIST>(in long entitiy, ref T1 t1, LIST resultList) where LIST : IList<RETURN>;
        public delegate RETURN MergeDelegate<T1, RETURN>(in Span<long> entities, Span<T1> t1s);

        struct SplitSet
        {
            public uint startIndex;
            public uint endIndex;
            public uint outIndex;
            public Table table;
            public ComponentBuffer[] buffers;
        }


        //public unsafe RETURN[] MapReduce<T1, RETURN>(TransformManyDelegate<T1, RETURN, PoolList<RETURN>> map, MergeDelegate<T1, RETURN> reduce, Comparison<RETURN> sort, int maxDegreeOfParallelism = int.MaxValue)
        //{
        //    var (elementCount, currentSplit, splits) = BaseParallelTransform(maxDegreeOfParallelism);

        //    var t1BufIndex = Array.IndexOf(Types, typeof(T1));
        //    //use this result array one chunk per parallel factor
        //    var resultArray = ArrayPool<PoolList<RETURN>>.Shared.Rent((int)elementCount);
        //    Parallel.For(0, currentSplit, (i) =>
        //    {
        //        ref var splitElement = ref splits[i];
        //        var t1BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t1BufIndex]);
        //        var t1Buffer = splitElement.buffers[t1BufferIndex] as ComponentBuffer<T1>;
        //        var resultList = resultArray[i];
        //        var componentEnumerator = t1Buffer.GetEnumerator();
        //        while (componentEnumerator.MoveNext())
        //        {
        //            var elementIndex = componentEnumerator.CurrentIndex;
        //            map(splitElement.table.GlobalIDFromIndex(elementIndex), ref t1Buffer[elementIndex], resultList);
        //        }

        //        resultList.Sort(sort);
        //    });

        //    //merge sort the elements of the result list here

        //    ArrayPool<SplitSet>.Shared.Return(splits);

        //    return resultArray;
        //}


        //iterate over the sorted result array chunks in order
        //anytime we see the elements match up we merge all of the matches togeather
        //clear the non first element
        //keep track of the total actual result items
        //allocate real result array
        //re iterate over the sorted result assigning the elements into the real result array

        public unsafe RETURN[] ParallelTransform<T1, RETURN>(TransformDelegate<T1, RETURN> each, int maxDegreeOfParallelism = int.MaxValue)
        {
            var (elementCount, currentSplit, splits) = BaseParallelTransform(maxDegreeOfParallelism);

            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            //use this result array one chunk per parallel factor
            var resultArray = ArrayPool<RETURN>.Shared.Rent((int)elementCount);
            Parallel.For(0, currentSplit, (i) =>
            {
                ref var splitElement = ref splits[i];
                var t1Buffer = splitElement.buffers[t1BufIndex] as ComponentBuffer<T1>;
                var resultIndex = splitElement.outIndex;
                var componentEnumerator = t1Buffer.GetEnumerator(splitElement.startIndex, splitElement.endIndex);
                while (componentEnumerator.MoveNext())
                {
                    var elementIndex = componentEnumerator.CurrentIndex;
                    resultArray[resultIndex++] = each(splitElement.table.GlobalIDFromIndex(elementIndex), ref t1Buffer[elementIndex]);
                }
            });

            ArrayPool<SplitSet>.Shared.Return(splits);

            return resultArray;
        }

        public unsafe RETURN[] ParallelTransform<T1, T2, RETURN>(TransformDelegate<T1, T2, RETURN> each, int maxDegreeOfParallelism = int.MaxValue)
        {
            var (elementCount, currentSplit, splits) = BaseParallelTransform(maxDegreeOfParallelism);

            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            var t2BufIndex = Array.IndexOf(Types, typeof(T2));
            //use this result array one chunk per parallel factor
            var resultArray = ArrayPool<RETURN>.Shared.Rent((int)elementCount);
            Parallel.For(0, currentSplit, (i) =>
            {
                ref var splitElement = ref splits[i];
                var t1Buffer = splitElement.buffers[t1BufIndex] as ComponentBuffer<T1>;
                var t2Buffer = splitElement.buffers[t2BufIndex] as ComponentBuffer<T2>;
                var resultIndex = splitElement.outIndex;
                var componentEnumerator = t1Buffer.GetEnumerator(splitElement.startIndex, splitElement.endIndex);
                while (componentEnumerator.MoveNext())
                {
                    var elementIndex = componentEnumerator.CurrentIndex;
                    resultArray[resultIndex++] = each(splitElement.table.GlobalIDFromIndex(elementIndex), ref t1Buffer[elementIndex], ref t2Buffer[elementIndex]);
                }
            });

            ArrayPool<SplitSet>.Shared.Return(splits);

            return resultArray;
        }

        public unsafe RETURN[] ParallelTransform<T1, T2, T3, RETURN>(TransformDelegate<T1, T2, T3, RETURN> each, int maxDegreeOfParallelism = int.MaxValue)
        {
            var (elementCount, currentSplit, splits) = BaseParallelTransform(maxDegreeOfParallelism);

            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            var t2BufIndex = Array.IndexOf(Types, typeof(T2));
            var t3BufIndex = Array.IndexOf(Types, typeof(T3));
            //use this result array one chunk per parallel factor
            var resultArray = ArrayPool<RETURN>.Shared.Rent((int)elementCount);
            Parallel.For(0, currentSplit, (i) =>
            {
                ref var splitElement = ref splits[i];
                var t1BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t1BufIndex]);
                var t1Buffer = splitElement.buffers[t1BufferIndex] as ComponentBuffer<T1>;

                var t2BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t2BufIndex]);
                var t2Buffer = splitElement.buffers[t2BufferIndex] as ComponentBuffer<T2>;

                var t3BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t3BufIndex]);
                var t3Buffer = splitElement.buffers[t3BufferIndex] as ComponentBuffer<T3>;
                var resultIndex = splitElement.outIndex;
                var componentEnumerator = t1Buffer.GetEnumerator(splitElement.startIndex, splitElement.endIndex);
                while (componentEnumerator.MoveNext())
                {
                    var elementIndex = componentEnumerator.CurrentIndex;
                    resultArray[resultIndex++] = each(splitElement.table.GlobalIDFromIndex(elementIndex), ref t1Buffer[elementIndex], ref t2Buffer[elementIndex], ref t3Buffer[elementIndex]);
                }
            });

            ArrayPool<SplitSet>.Shared.Return(splits);

            return resultArray;
        }

        public unsafe RETURN[] ParallelTransform<T1, T2, T3, T4, RETURN>(TransformDelegate<T1, T2, T3, T4, RETURN> each, int maxDegreeOfParallelism = int.MaxValue)
        {
            var (elementCount, currentSplit, splits) = BaseParallelTransform(maxDegreeOfParallelism);

            var t1BufIndex = Array.IndexOf(Types, typeof(T1));
            var t2BufIndex = Array.IndexOf(Types, typeof(T2));
            var t3BufIndex = Array.IndexOf(Types, typeof(T3));
            var t4BufIndex = Array.IndexOf(Types, typeof(T4));
            //use this result array one chunk per parallel factor
            var resultArray = ArrayPool<RETURN>.Shared.Rent((int)elementCount);
            Parallel.For(0, currentSplit, (i) =>
            {
                ref var splitElement = ref splits[i];
                var t1BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t1BufIndex]);
                var t1Buffer = splitElement.buffers[t1BufferIndex] as ComponentBuffer<T1>;

                var t2BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t2BufIndex]);
                var t2Buffer = splitElement.buffers[t2BufferIndex] as ComponentBuffer<T2>;

                var t3BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t3BufIndex]);
                var t3Buffer = splitElement.buffers[t3BufferIndex] as ComponentBuffer<T3>;

                var t4BufferIndex = splitElement.table.IndexForComponent(ComponentIds[t4BufIndex]);
                var t4Buffer = splitElement.buffers[t3BufferIndex] as ComponentBuffer<T4>;
                var resultIndex = splitElement.outIndex;
                var componentEnumerator = t1Buffer.GetEnumerator(splitElement.startIndex, splitElement.endIndex);
                while(componentEnumerator.MoveNext())
                {
                    var elementIndex = componentEnumerator.CurrentIndex;
                    resultArray[resultIndex++] = each(splitElement.table.GlobalIDFromIndex(elementIndex), ref t1Buffer[elementIndex], ref t2Buffer[elementIndex], ref t3Buffer[elementIndex], ref t4Buffer[elementIndex]);
                }
            });

            ArrayPool<SplitSet>.Shared.Return(splits);

            return resultArray;
        }

        private (uint elementCount, uint maxSplit, SplitSet[] splits) BaseParallelTransform(int maxDegreeOfParallelism)
        {
            MaybeReinit();
            var parallelFactor = Math.Min(Environment.ProcessorCount, maxDegreeOfParallelism);

            uint elementCount = 0;
            foreach (var (table, buffers) in this)
            {
                elementCount += (uint)table.RealCount;
            }

            var unitSize = (uint)Math.Max(elementCount / parallelFactor, 1);
            //allocate the maximum split sets we might need
            //then fill them, startIndex, endIndex, table, buffers
            //startIndex, endIndex into the split sets one per parallelFactor
            uint currentSplit = 0;
            uint currentElementCount = 0;
            uint totalElementCount = 0;
            bool first = true;
            var splits = ArrayPool<SplitSet>.Shared.Rent(parallelFactor * 2);
            Array.Clear(splits, 0, splits.Length);
            foreach (var (table, buffers) in this)
            {
                if (first || splits[currentSplit].table != table)
                {
                    if (!first)
                        currentSplit++;

                    first = false;
                    ref var splitElement = ref splits[currentSplit];
                    splitElement.table = table;
                    splitElement.buffers = buffers;
                    splitElement.startIndex = 0;
                    splitElement.endIndex = 0;
                    splitElement.outIndex = totalElementCount;
                    totalElementCount += currentElementCount;
                    currentElementCount = 0;
                }

                uint remainingCapacity = (uint)table.Count - currentElementCount;
                //If a table doesnt fit inside the split factor it needs to be spread out into multiple split buffers
                while (currentElementCount < table.Count)
                {
                    ref var currentSplitElement = ref splits[currentSplit];
                    remainingCapacity = (uint)table.Count - currentElementCount;
                    var remainingToTake = table.Count - currentSplitElement.startIndex;
                    var takeCount = (uint)Math.Min(unitSize, remainingToTake);
                    currentSplitElement.outIndex = totalElementCount + currentElementCount;
                    currentElementCount += takeCount;
                    currentSplitElement.endIndex = currentElementCount;

                    if (remainingToTake > unitSize)
                    {
                        currentSplit++;
                        ref var nextElement = ref splits[currentSplit];
                        nextElement.table = table;
                        nextElement.buffers = buffers;
                        nextElement.startIndex = currentElementCount;
                    }
                }
            }

#if DEBUG
            //Test assumptions
            uint actualCount = 0;
            foreach (var split in splits)
            {
                actualCount += split.endIndex - split.startIndex;
            }

            if (actualCount != elementCount)
                throw new Exception();
#endif

            return (elementCount, currentSplit, splits);
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
