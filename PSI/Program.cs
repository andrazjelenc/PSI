using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace PSI
{
    class Program
    {
        static void Main()
        {
            ulong polyModulusDegree = 8192; // must be 2^n
            ulong plainModulus = 1009; // must be prime

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var xs = GenerateRandomIntArray(20, (int)plainModulus);
            var ys = GenerateRandomIntArray(35, (int)plainModulus);

            var privateIntersection = Scenario.Run(polyModulusDegree, plainModulus, xs, ys);

            stopWatch.Stop();
            Console.WriteLine($"Test took {stopWatch.Elapsed.TotalSeconds} seconds.");

            var intersection = CalculateIntersection(xs, ys);

            if (HaveSameElements(privateIntersection, intersection))
            {
                Console.WriteLine("Correct!");
            }
            else
            {
                Console.WriteLine("Error!");
            }
        }

        static List<int> CalculateIntersection(int[] a, int[] b)
        {
            var intersection = new List<int>();

            for (int i = 0; i < a.Length; i++)
            {
                if (b.Contains(a[i]))
                {
                    intersection.Add(a[i]);
                }
            }
            return intersection;
        }

        static bool HaveSameElements(List<int> a, List<int> b)
        {
            return a.Count == b.Count && a.All(b.Contains);
        }

        static int[] GenerateRandomIntArray(int size, int maxValue)
        {
            Random rnd = new Random();
            var array = new int[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = rnd.Next(1, maxValue);
            }
            return array;
        }
    }
}