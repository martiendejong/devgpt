namespace DevGPT.NewAPI
{
    public interface IObjectListFile<T>
    {
        bool Exists { get; }
        List<T> Load();
        void Save(List<T> embeddings);
    }
}