namespace FoPra.model
{
    public class Ray
    {
        public (double, double?) Size { get; }

        public Ray((double, double) size)
        {
            Size = size;
        }
        
        public Ray(double size)
        {
            Size = (size, null);
        }
    }
}