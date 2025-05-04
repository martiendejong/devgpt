using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public abstract class BaseStore<T> : IDictionary<string, T>
    {
        public abstract void Store(string key, T value);
        public abstract bool Remove(string key);

        public event EventHandler<StoreUpdateEventArgs<T>> BeforeUpdate;
        public event EventHandler<StoreUpdateEventArgs<T>> AfterUpdate;
        public event EventHandler<StoreRemoveEventArgs> BeforeRemove;
        public event EventHandler<StoreRemoveEventArgs> AfterRemove;

        public Dictionary<string, T> Data = new Dictionary<string, T>();

        public T this[string key] { get => Data[key]; set => Store(key, value); }

        public ICollection<string> Keys => Data.Keys;

        public ICollection<T> Values => Data.Values;

        public int Count => Data.Count;

        public bool IsReadOnly => false;

        public void Add(string key, T value) => Store(key, value);

        public void Add(KeyValuePair<string, T> item) => Store(item.Key, item.Value);

        public void Clear() => Data.Clear();

        public bool Contains(KeyValuePair<string, T> item) => Data.Contains(item);

        public bool ContainsKey(string key) => Data.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex) => Data.ToList().CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => Data.GetEnumerator();

        public bool Remove(KeyValuePair<string, T> item) => Remove(item.Key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value) => Data.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
    }
}
