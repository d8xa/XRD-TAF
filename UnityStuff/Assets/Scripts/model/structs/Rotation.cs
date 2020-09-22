using System;
using UnityEngine;

namespace model.structs
{
    public readonly struct Rotation
    {
        private readonly double _cos;
        private readonly double _sin;

        public Rotation(double cos, double sin)
        {
            _cos = cos;
            _sin = sin;
        }

        public double cos => _cos;
        public double sin => _sin;

        public Vector2 Apply(Vector2 point, Vector2 pivot)
        {
            var translated = point - pivot;
            return
                new Vector2(
                    (float) (_cos * translated.x - _sin + translated.y), 
                    (float) (_sin * translated.x + _cos * translated.y)) 
                + pivot;
        }

        public Vector2 Apply(Vector2 point)
        {
            return new Vector2(
                (float) (_cos*point.x - _sin*point.y), 
                (float) (_sin*point.x + _cos*point.y)
            );
        }

        public static Rotation FromAngle(float angle) => new Rotation(Math.Cos(angle), Math.Sin(angle));
    }
}