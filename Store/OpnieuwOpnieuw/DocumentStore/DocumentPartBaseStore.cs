using System.Linq;

namespace Store.OpnieuwOpnieuw.DocumentStore
{
    public class DocumentPartBaseStore : BaseStore<IEnumerable<string>>, IDocumentPartStore
    {
        override public void Store(string name, IEnumerable<string> partKeys)
        {
            _BeforeUpdate(name, partKeys);
            Data[name] = partKeys;
            _AfterUpdate(name, partKeys);
        }

        override public bool Remove(string key)
        {
            _BeforeRemove(key);
            Data.Remove(key);
            _AfterRemove(key);
            return true;
        }
    }
}
