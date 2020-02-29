using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FoPra.model 

{ 
    public class Detector 
    {
        public (int,int) Resolution { get; }
        public (double, double) Pixelsize { get; }

        public Detector(
            (int, int) resolution,
            (double, double) pixelsize
            ) 
        { 
            Resolution = resolution; 
            Pixelsize = pixelsize;
        }

        public override string ToString() 
        { 
            return $"Detector: [res={Resolution}, px_size={Pixelsize}]";
        }
    }
}
