using System.Linq;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentPartBaseStore : BaseStore<string[]>, IDocumentPartStore
    {
        public event EventHandler<StoreUpdateEventArgs<string[]>> BeforeUpdate;
        public event EventHandler<StoreUpdateEventArgs<string[]>> AfterUpdate;
        public event EventHandler<StoreRemoveEventArgs> BeforeRemove;
        public event EventHandler<StoreRemoveEventArgs> AfterRemove;

        override public void Store(string name, string[] partKeys)
        {
            BeforeUpdate?.Invoke(this, new StoreUpdateEventArgs<string[]>(name, partKeys));
            Data[name] = partKeys;
            AfterUpdate?.Invoke(this, new StoreUpdateEventArgs<string[]>(name, partKeys));
        }

        override public bool Remove(string key)
        {
            BeforeRemove?.Invoke(this, new StoreRemoveEventArgs(key));
            Data.Remove(key);
            AfterRemove?.Invoke(this, new StoreRemoveEventArgs(key));
            return true;
        }
    }
}
