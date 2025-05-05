using System.Linq;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentPartMemoryStore : AbstractStore<IEnumerable<string>>, IDocumentPartStore
    {
        override public async Task Store(string name, IEnumerable<string> partKeys)
        {
            InvokeBeforeUpdate(name, partKeys);
            Data[name] = partKeys;
            InvokeAfterUpdate(name, partKeys);
        }

        override public bool Remove(string key)
        {
            InvokeBeforeRemove(key);
            Data.Remove(key);
            InvokeAfterRemove(key);
            return true;
        }
    }
}
