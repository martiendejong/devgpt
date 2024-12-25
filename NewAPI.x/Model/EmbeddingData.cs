using MathNet.Numerics.LinearAlgebra;



namespace DevGPT.NewAPI
{
    public class EmbeddingData : List<double>
    {
        public EmbeddingData(IEnumerable<double> data)
        {
            AddRange(data);
        }

        public Vector<double> Vector => Vector<double>.Build.DenseOfArray(ToArray());

        public double CosineSimilarity(EmbeddingData compareTo)
        {
            return CosineSimilarity(Vector, compareTo.Vector);
        }

        private static double CosineSimilarity(Vector<double> v1, Vector<double> v2)
        {
            return v1.DotProduct(v2) / (v1.L2Norm() * v2.L2Norm());
        }
    }
}