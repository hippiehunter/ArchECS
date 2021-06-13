using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchECS.Query;

namespace ArchECS
{
    public abstract class System : IDisposable
    {
        //Create component list - micro world - component buffers
        //Delete list (id : type mask)

        //List of explicit dependencies
        //List of components that might be added/deleted (updates ok)

        protected Query _query;

        public System(Query query)
        {
            _query = query;
        }

        public abstract void Run();

        public void Dispose()
        {
            _query?.Dispose();
            _query = null;
        }
    }

    public sealed class System<T> : System
    {
        EachDelegate<T> _each;
        public System(Query query, EachDelegate<T> each) : base(query)
        {
            _each = each;
        }

        public override void Run()
        {
            _query.Each(_each);
        }
    }

    public sealed class System<T1, T2> : System
    {
        EachDelegate<T1, T2> _each;
        public System(Query query, EachDelegate<T1, T2> each) : base(query)
        {
            _each = each;
        }

        public override void Run()
        {
            _query.Each(_each);
        }
    }

    public sealed class System<T1, T2, T3> : System
    {
        EachDelegate<T1, T2, T3> _each;
        public System(Query query, EachDelegate<T1, T2, T3> each) : base(query)
        {
            _each = each;
        }

        public override void Run()
        {
            _query.Each(_each);
        }
    }

    public sealed class System<T1, T2, T3, T4> : System
    {
        EachDelegate<T1, T2, T3, T4> _each;
        public System(Query query, EachDelegate<T1, T2, T3, T4> each) : base(query)
        {
            _each = each;
        }

        public override void Run()
        {
            _query.Each(_each);
        }
    }
}
