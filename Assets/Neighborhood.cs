using System;

public partial class Life
{
    public class Neighborhood
    {
        public readonly float[] N = new float[9];

        private readonly string _name;

        public Neighborhood(string name, params float[] n)
        {
            _name = name;
            Array.Copy(n, N, n.Length);
        }

        public override string ToString()
        {
            return _name;
        }
    }
}