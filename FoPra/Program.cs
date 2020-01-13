using System;
using System.Linq;
using FoPra.model;
using FoPra.math;
using FoPra.util;
using Math = FoPra.math.Math;

namespace FoPra
{
    static class Program
    {
        private static void Main()
        {
            var detector = new Detector(resolution: (2048, 2048), pixelsize: (200.0, 200.0));
            var sample = new Sample(
                totalDiameter: 1.0, 
                cellThickness: 0.01,
                muSample: 6.70333, 
                muCell: 0.54747, 
                detectorDistance: 240.18, 
                detectorOffset: (4.3, 4.6)
                );
            var model = new Model(detector, sample);
            var model1 = new Model(detector, sample, name:"Test");
            
            Console.WriteLine(model);
            Console.WriteLine(model1);
            
            // Math
            var rnd = new Random();
            double[] fake_distSample = Enumerable
                .Repeat(0, 100)
                .Select(i => rnd.NextDouble())
                .ToArray();
            double[] fake_distCell = Enumerable
                .Repeat(0, 100)
                .Select(i => rnd.NextDouble())
                .ToArray();
            var fake_intensities = Enumerable
                .Repeat(0, 100)
                .Select(i => rnd.Next(0,3)+rnd.NextDouble())
                .ToArray();

            var MathHelper = new Math(); 
            double[] absorptions = MathHelper.absorptions(
                sample.MuSample, 
                sample.MuCell,
                fake_distSample,
                fake_distCell
            );

            var intensity = MathHelper.intensity(absorptions, fake_intensities);
            Console.WriteLine($"\nIntensity: {intensity}");
            
            
            // Segments
            var segments = new Segments(260,sample);

            Console.WriteLine("\nSegment material:");
            for (int i = 0; i < segments.Size; i++)
            {
                Console.WriteLine(string.Join(", ",
                    Enumerable.Range(0,segments.Size)
                        .Select(j => segments.Material[i,j])
                        .ToArray()
                    ));
            }
            
            Console.WriteLine($"\nDistance between two points ({segments.Count:g} segments):");
            var (i1, j1) = (5, 5);
            var (i2, j2) = (11, 11);
            Console.WriteLine($"Coordinates: [{i1},{j1}], [{i2},{j2}]");
            Console.WriteLine($"Distance: {segments.distance((i1, j1), (i2, j2))}");
            
            
            var bigsegments = new Segments(5000, sample);
            Console.WriteLine($"\nDistance between two points ({bigsegments.Count:g} segments):");
            int i3,j3; i3 = j3 = Convert.ToInt16(bigsegments.Size*0.15);
            int i4,j4; i4 = j4 = Convert.ToInt16(bigsegments.Size*0.75);
            Console.WriteLine($"Indices: [{i3},{j3}], [{i4},{j4}]");
            Console.WriteLine($"Distance: {bigsegments.distance((i3,j3), (i4,j4))}");
            
            bigsegments = new Segments(500000, sample);
            Console.WriteLine($"\nDistance between two points ({bigsegments.Count:g} segments):");
            i3 = j3 = Convert.ToInt16(bigsegments.Size*0.15);
            i4 = j4 = Convert.ToInt16(bigsegments.Size*0.75);
            Console.WriteLine($"Indices: [{i3},{j3}], [{i4},{j4}]");
            Console.WriteLine($"Distance: {bigsegments.distance((i3,j3), (i4,j4))}");
        }
    }
}