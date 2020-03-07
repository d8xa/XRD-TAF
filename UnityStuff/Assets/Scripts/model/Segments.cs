using System;
using System.Linq;
using static FoPra.util.MathTools;

namespace FoPra.model
{
    public class Segments
    {/*
        public int Count { get; }
        public int Size { get; }
        public double[,,] Grid { get; set; } // Precompute grid, or compute only when needed? 
        public int[,] Material { get; }

        public Segments(int approx_count, Sample sample)
        {
            Count = approx_count;
            Size = (int) Math.Ceiling(Math.Sqrt(approx_count));
            Grid = makeGrid(0, (decimal) sample.TotalDiameter);
            Material = makeMask(sample);
        }

        public double distance((double, double) x, (double, double) y)
        {
            return Math.Sqrt(
                Math.Pow(x.Item1 - y.Item1, 2) + 
                Math.Pow(x.Item2 - y.Item2, 2)
                );
        }

        public double distance((int, int) xi, (int, int) yi)
        {
            return distance(
                (Grid[xi.Item1, xi.Item2, 0], Grid[xi.Item1, xi.Item2, 1]),
                (Grid[yi.Item1, yi.Item2, 0], Grid[yi.Item1, yi.Item2, 1])
            );
        }

        public double[,,] makeGrid(decimal a, decimal b)
        {
            var tmp = new double[Size, Size, 2];
            var lsp = LinSpace(Size, a, b);
            for (var i = 0; i < Size; i++)
            {
                for (var j = 0; j < Size; j++)
                {
                    tmp[i, j, 0] = lsp[i];
                    tmp[i, j, 1] = lsp[j];
                }
            }

            return tmp;
        }

        public int[,] makeMask(Sample sample)
        {
            var tmp = new int[Size, Size];
            var center = (Grid[Size / 2, Size / 2, 0], Grid[Size / 2, Size / 2, 1]);
            var cellRadius = sample.TotalDiameter / 2;
            var sampleRadius = cellRadius - sample.CellThickness / 2;
            
            for (var i = 0; i < Size; i++)
            {
                for (var j = 0; j < Size; j++)
                {
                    var d = distance(center, (Grid[i, j, 0], Grid[i, j, 1]));
                    if (d <= sampleRadius)
                        tmp[i, j] = 2;
                    else if (d <= cellRadius)
                        tmp[i, j] = 1;
                    else
                        tmp[i, j] = 0;
                }
            }

            return tmp;
        }

        public double[] GridCoordinates(int i, int j)
        {
            return new[] {Grid[i, j, 0], Grid[i, j, 1]};
        }
        */
    }

}