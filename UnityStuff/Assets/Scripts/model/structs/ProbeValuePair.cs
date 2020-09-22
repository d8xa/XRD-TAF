using System;
using UnityEngine;

namespace model.structs
{
    public struct ProbeValuePair
    {
        public float cell;
        public float sample;

        public ProbeValuePair(float cell, float sample)
        {
            this.cell = cell;
            this.sample = sample;
        }
        
        public ProbeValuePair(double cell, double sample)
        {
            this.cell = (float) cell;
            this.sample = (float) sample;
        }

        #region Operator overloads

        public static ProbeValuePair operator *(ProbeValuePair a, ProbeValuePair b)
        {
            return new ProbeValuePair(a.cell * b.cell, a.sample * b.sample);
        }
        
        public static ProbeValuePair operator +(ProbeValuePair a, ProbeValuePair b)
        {
            return new ProbeValuePair(a.cell + b.cell, a.sample + b.sample);
        }
        
        public static ProbeValuePair operator -(ProbeValuePair a, ProbeValuePair b)
        {
            return new ProbeValuePair(a.cell - b.cell, a.sample - b.sample);
        }
        
        public static ProbeValuePair operator /(ProbeValuePair a, ProbeValuePair b)
        {
            return new ProbeValuePair(a.cell / b.cell, a.sample / b.sample);
        }

        #endregion

        public ProbeValuePair squared => new ProbeValuePair(Math.Pow(cell, 2), Math.Pow(sample, 2));

        public ProbeValuePair FromVector2(Vector2 vector)
        {
            return new ProbeValuePair(vector.x, vector.y);
        }

        public Vector2 ToVector2()
        {
            return new Vector2(cell, sample);
        }
    }
}