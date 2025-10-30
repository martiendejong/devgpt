public static class StoreFactory
{
    public static ITextStore CreateTextStore(string spec)
    {
        if (spec.StartsWith("postgres:", StringComparison.InvariantCultureIgnoreCase))
        {
            var cs = spec.Substring("postgres:".Length);
            return new PostgresTextStore(cs);
        }
        // default to file path
        return new TextFileStore(spec);
    }

    public static IDocumentPartStore CreatePartStore(string spec)
    {
        if (string.Equals(spec, "memory:", StringComparison.InvariantCultureIgnoreCase) || string.Equals(spec, "memory", StringComparison.InvariantCultureIgnoreCase))
        {
            return new DocumentPartMemoryStore();
        }
        if (spec.StartsWith("postgres:", StringComparison.InvariantCultureIgnoreCase))
        {
            var cs = spec.Substring("postgres:".Length);
            return new PostgresDocumentPartStore(cs);
        }
        // default to file path
        return new DocumentPartFileStore(spec);
    }
}

