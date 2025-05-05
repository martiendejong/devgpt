using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public abstract class AbstractStore<T> : IDictionary<string, T>
    {
        public abstract Task Store(string key, T value);
        public abstract bool Remove(string key);

        public event EventHandler<StoreUpdateEventArgs<T>> BeforeUpdate;
        public event EventHandler<StoreUpdateEventArgs<T>> AfterUpdate;
        public event EventHandler<StoreRemoveEventArgs> BeforeRemove;
        public event EventHandler<StoreRemoveEventArgs> AfterRemove;

        protected void InvokeBeforeUpdate(string key, T value) => BeforeUpdate?.Invoke(this, new StoreUpdateEventArgs<T>(key, value));
        protected void InvokeAfterUpdate(string key, T value) => AfterUpdate?.Invoke(this, new StoreUpdateEventArgs<T>(key, value));
        protected void InvokeBeforeRemove(string key) => BeforeRemove?.Invoke(this, new StoreRemoveEventArgs(key));
        protected void InvokeAfterRemove(string key) => AfterRemove?.Invoke(this, new StoreRemoveEventArgs(key));

        public Dictionary<string, T> Data = [];

        #region implementation of IDictionary

        public T this[string key] { get => Data[key]; set => Store(key, value).RunSynchronously(); }

        public ICollection<string> Keys => Data.Keys;

        public ICollection<T> Values => Data.Values;

        public int Count => Data.Count;

        public bool IsReadOnly => false;

        public void Add(string key, T value) => Store(key, value).RunSynchronously();

        public void Add(KeyValuePair<string, T> item) => Store(item.Key, item.Value).RunSynchronously();

        public void Clear() => Data.Clear();

        public bool Contains(KeyValuePair<string, T> item) => Data.Contains(item);

        public bool ContainsKey(string key) => Data.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex) => Data.ToList().CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => Data.GetEnumerator();

        public bool Remove(KeyValuePair<string, T> item) => Remove(item.Key);

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value) => Data.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();

        #endregion
    }
}