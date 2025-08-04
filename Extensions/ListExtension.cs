using System.Collections.Generic;

namespace Extensions
{
    public static class ListExtension
    {
        public static IListReverseEnumerable<T> Reverse<T>(this IList<T> list) => new (list);

        public struct IListReverseEnumerable<T>
        {
            private IList<T> _list;

            public IListReverseEnumerable(IList<T> list)
            {
                _list = list;
            }

            public IListReverseEnumerator<T> GetEnumerator() => new IListReverseEnumerator<T>(_list);
            
            public struct IListReverseEnumerator<T>
            {
                private IList<T> _list;
                private int _index;

                public IListReverseEnumerator(IList<T> list)
                {
                    _list = list;
                    _index = list.Count;
                }

                public T Current => _list[_index];

                public bool MoveNext() => --_index >= 0;
            }
        }
    }
}