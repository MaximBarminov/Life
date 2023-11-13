using System.Collections.Generic;

public partial class Life
{
    public class Rule
    {
        public readonly float[] B = new float[9];
        public readonly float[] S = new float[9];

        private readonly string _name;
        private readonly string _notation;

        public Rule(string notation, string name = null)
        {
            _notation = notation;
            _name = name;

            var items = _notation.Split('/');

            Parse(items[0], B);
            Parse(items[1], S);

            static void Parse(string input, float[] output)
            {
                var hashSet = new HashSet<int>();

                for (var i = 1; i < input.Length; i++)
                    hashSet.Add(int.Parse(input[i].ToString()));

                for (var i = 0; i < output.Length; i++)
                    if (hashSet.Contains(i))
                        output[i] = 1;
            }
        }

        public override string ToString()
        {
            return _name != null ? _notation + " - " + _name : _notation;
        }
    }
}