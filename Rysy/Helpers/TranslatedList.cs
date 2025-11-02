using System.Collections;

namespace Rysy.Helpers;

public sealed class TranslatedList<T>(IReadOnlyList<T> backing, Func<T, string> toLangKey, string prefix) : IReadOnlyList<string> {
    public IEnumerator<string> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public int Count => backing.Count;

    public string this[int index] => toLangKey(backing[index]).TranslateOrHumanize(prefix);

    private struct Enumerator(TranslatedList<T> list) : IEnumerator<string> {
        private int _i = -1;
        
        public bool MoveNext() {
            _i++;
            if (_i >= list.Count)
                return false;

            Current = list[_i];
            return true;
        }

        public void Reset() {
            _i = -1;
        }

        public string Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }
}