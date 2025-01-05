namespace DevGPT.NewAPI
{
    public interface IGeneratorStore
    {
        Task<string> Generator_Question(string query);
        Task<string> Generator_UpdateStore(string query);
    }
}