using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbolism;
using Symbolism.AlgebraicExpand;

namespace Lagrange
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Missing input file");
            }

            try
            {
                string[] lines = File.ReadAllLines(args[0]);
                if (lines.Length < 0)
                {
                    throw new Exception("No input");
                }

                var xs = new double[lines.Length];
                var ys = new double[lines.Length];

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] lineData = lines[i].Split(new char[] { ' ' },StringSplitOptions.RemoveEmptyEntries);
                    xs[i] = Double.Parse(lineData[0], CultureInfo.InvariantCulture.NumberFormat);
                    ys[i] = Double.Parse(lineData[1], CultureInfo.InvariantCulture.NumberFormat);
                }

                Console.WriteLine(GenerateLagrangePolynomial(xs, ys));
            }
            catch
            {
                Console.WriteLine("Could not parse input file " + args[0]);
                return;
            }
        }

        static MathObject GenerateLagrangePolynomial(double[] xs, double[] ys)
        {
            var xsSet = new HashSet<double>(xs);

            if (xsSet.Count < xs.Length)
            {
                throw new ArgumentException("x values not unique");
            }

            var ysSet = new HashSet<double>(ys);

            if (ysSet.Count < ys.Length)
            {
                throw new ArgumentException("y values not unique");
            }

            var x = new Symbol("x");

            var members = new MathObject[ys.Length];

            for (int i = 0; i < ys.Length; i++)
            {
                members[i] = ys[i] * GenerateBasisPolynomial(x, xs, i);
            }

            var sum = new Sum(members);
 
            return sum.AlgebraicExpand();
        }

        static MathObject GenerateBasisPolynomial(Symbol x, double[] xs, int index)
        {
            MathObject member = 1;
            for (int i = 0; i < xs.Length; i++)
            {
                if (i != index)
                {
                    member *= (x - xs[i]) / (xs[index] - xs[i]); 
                }
            }

            return member;
        }
    }
}
