namespace DevGPT.NewAPI
{
    public class DocumentSplitter
    {
        public int TokensPerPart { get; set; } = 1000;

        public List<string> SplitDocument(string absOrgPath, string split = "\n")
        {
            var partNr = 0;
            var tokenCounter = new TokenCounter();
            var content = File.ReadAllText(absOrgPath);
            var remainingLines = content.Split(split).ToList();

            var result = new List<string>();
            while (remainingLines.Count > 0)
            {
                var partLines = new List<string>();
                bool partComplete = false;
                var moveLineToPart = () => {
                    partLines.Add(remainingLines[0]);
                    remainingLines.RemoveAt(0);
                    var partTokens = tokenCounter.CountTokens(string.Join(split, partLines));
                    partComplete = partTokens >= TokensPerPart;
                };
                moveLineToPart();
                while (!partComplete && remainingLines.Count > 0)
                {
                    moveLineToPart();
                }

                result.Add(string.Join(split, partLines));

                partNr++;
            }

            return result;
        }
    }
}