using System;
using FoPra.model;

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
        }
    }
}