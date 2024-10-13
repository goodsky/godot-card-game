using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RandomGenerator
{
    private Random _rnd;

    public int Seed { get; private set; }
    public int N { get; private set; } // keep track of how far along we are for this generator

    public RandomGenerator(int? seed = null, int? n = null)
    {
        Seed = seed ?? Random.Shared.Next();
        N = n ?? 0;
        _rnd = new Random(Seed);

        for (int i = 0; i < N; i++)
        {
            _rnd.Next();
        }
    }

    public int Next()
    {
        ++N;
        return _rnd.Next();
    }

    public int Next(int max)
    {
        ++N;
        return _rnd.Next(max);
    }

    public int Next(int min, int max)
    {
        ++N;
        return _rnd.Next(min, max);
    }

    public float Nextf(float min, float max)
    {
        ++N;
        return _rnd.NextSingle() * (max - min) + min;
    }

    public T SelectRandom<T>(IEnumerable<T> e)
    {
        ++N;
        var list = e.ToList();
        return list[_rnd.Next(list.Count)];
    }

    public T SelectRandom<T>(List<T> list)
    {
        ++N;
        return list[_rnd.Next(list.Count)];
    }

    public T SelectRandom<T>(T[] arr)
    {
        ++N;
        return arr[_rnd.Next(arr.Length)];
    }
}