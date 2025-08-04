using System;

namespace Extensions
{
    public static class SpanExtension
    {
        public static SpanSplitEnumerator Split(this ReadOnlySpan<char> src, ReadOnlySpan<char> separator) => new(src, separator);

        public ref struct SpanSplitEnumerator
        {
            private ReadOnlySpan<char> _string;
        
            private readonly ReadOnlySpan<char> _separator;
            
            public ReadOnlySpan<char> Current { get; private set; }
            
            public SpanSplitEnumerator(ReadOnlySpan<char> source, ReadOnlySpan<char> separator)
            {
                _string = source;
                _separator = separator;
                Current = ReadOnlySpan<char>.Empty;
            }


            public SpanSplitEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (_string.Length <= 0)
                {
                    return false;
                }

                int index = _string.IndexOf(_separator);
                if (index >= 0)
                {
                    Current = _string[..index];
                    _string = _string[(index + _separator.Length)..];
                }
                else
                {
                    Current = _string;
                    _string = ReadOnlySpan<char>.Empty;
                }

                return true;
            }
        }
    }
}