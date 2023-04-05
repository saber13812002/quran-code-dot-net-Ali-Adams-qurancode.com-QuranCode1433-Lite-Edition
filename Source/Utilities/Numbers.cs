using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Security.Cryptography;

public enum Direction { LeftToRight, RightToLeft };
public enum NumberType
{
    None,                   // not a number (eg infinity, -infinity)
    Unit,                   // indivisible by anything
    Prime,                  // divisible by self only (dividing by 1 doesn't divide into smaller parts and is misleading)
    AdditivePrime,          // prime with prime digit sum
    NonAdditivePrime,       // prime with non-prime digit sum
    Composite,              // divisible by self and other(s)
    AdditiveComposite,      // composite with composite digit sum
    NonAdditiveComposite,   // composite with non-composite digit sum
    Odd,                    // indivisible by 2
    Even,                   // divisible by 2
    Square,                 // n*n
    Cubic,                  // n*n*n
    Quartic,                // n*n*n*n
    Quintic,                // n*n*n*n*n
    Sextic,                 // n*n*n*n*n*n          // also called hexic
    Septic,                 // n*n*n*n*n*n*n        // also called heptic
    Octic,                  // n*n*n*n*n*n*n*n
    Nonic,                  // n*n*n*n*n*n*n*n*n
    Decic,                  // n*n*n*n*n*n*n*n*n*n
    Natural                 // natural number from 1 to MaxValue
};

public enum IndexType { Prime, Composite };
public enum IndexSubType { Any, Additive, NonAdditive };
public enum FactorsType { Any, Duplicate, Unique };

public enum NumberKind { Deficient, Perfect, Abundant };

// = ≠ < ≤ > ≥ ÷ !÷ Σ
public enum ComparisonOperator { Equal, NotEqual, LessThan, LessOrEqual, GreaterThan, GreaterOrEqual, DivisibleBy, IndivisibleBy, EqualSum, Reserved };

// + - * / %
public enum ArithmeticOperator { Plus, Minus, Multiply, Divide, Modulus };

public static class Numbers
{
    public const int MAX_SERIES_LENGTH = 1024;
    public const double ERROR_MARGIN = 0.000000000001D;
    public const int DEFAULT_RADIX = 10;
    public const int RADIX_NINETEEN = 19;
    public const int DEFAULT_DIVISOR = 19;
    public static Color DIVISOR_COLOR = Color.FromArgb(208, 255, 255);              // LightCyan
    public static Color INTERESTING_NUMBER_COLOR = Color.FromArgb(0, 255, 255);     // Cyan
    public static Color CARMICHAELX_NUMBER_COLOR = Color.FromArgb(255, 255, 208);   // LightYellow
    public static Color CARMICHAEL_NUMBER_COLOR = Color.FromArgb(255, 255, 0);      // Yellow

    public static Color[] NUMBER_TYPE_COLORS =
    { 
        /* NumberType.None */                   Color.Black,
        /* NumberType.Unit */                   Color.DarkViolet,
        /* NumberType.Prime */                  Color.Black,
        /* NumberType.AdditivePrime */          Color.Blue,
        /* NumberType.NonAdditivePrime */       Color.Green,
        /* NumberType.Composite */              Color.Black,
        /* NumberType.AdditiveComposite */      Color.FromArgb(240, 32, 32),
        /* NumberType.NonAdditiveComposite */   Color.FromArgb(128, 32, 32),
        /* NumberType.Odd */                    Color.Black,
        /* NumberType.Even */                   Color.Black,
        /* NumberType.Square */                 Color.Navy,
        /* NumberType.Cubic */                  Color.Navy,
        /* NumberType.Quartic */                Color.Navy,
        /* NumberType.Quintic */                Color.Navy,
        /* NumberType.Sextic */                 Color.Navy,
        /* NumberType.Septic */                 Color.Navy,
        /* NumberType.Octic */                  Color.Navy,
        /* NumberType.Nonic */                  Color.Navy,
        /* NumberType.Decic */                  Color.Navy,
        /* NumberType.Natural */                Color.Navy
    };
    public static Color[] NUMBER_TYPE_BACKCOLORS =
    { 
        /* NumberType.None */                   Color.White,
        /* NumberType.Unit */                   Color.FromArgb(255, 248, 255),
        /* NumberType.Prime */                  Color.White,
        /* NumberType.AdditivePrime */          Color.FromArgb(208, 208, 255),
        /* NumberType.NonAdditivePrime */       Color.FromArgb(240, 255, 240),
        /* NumberType.Composite */              Color.White,
        /* NumberType.AdditiveComposite */      Color.FromArgb(255, 224, 224),
        /* NumberType.NonAdditiveComposite */   Color.FromArgb(232, 216, 216),
        /* NumberType.Odd */                    Color.White,
        /* NumberType.Even */                   Color.White,
        /* NumberType.Square */                 Color.White,
        /* NumberType.Cubic */                  Color.White,
        /* NumberType.Quartic */                Color.White,
        /* NumberType.Quintic */                Color.White,
        /* NumberType.Sextic */                 Color.White,
        /* NumberType.Septic */                 Color.White,
        /* NumberType.Octic */                  Color.White,
        /* NumberType.Nonic */                  Color.White,
        /* NumberType.Decic */                  Color.White,
        /* NumberType.Natural */                Color.White
    };
    public static Color GetNumberTypeColor(long number)
    {
        if (number < 0L) number *= -1L;
        if (number > 1000000000000L) return Color.Black;
        return GetNumberTypeColor(number.ToString(), Numbers.DEFAULT_RADIX);
    }
    public static Color GetNumberTypeColor(string text, int radix)
    {
        if (text.Length > 16) return Color.Black;

        // if negative number, remove -ve sign
        if (text.StartsWith("-")) text = text.Remove(0, 1);

        if (IsUnit(text, radix))
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.Unit];
        }

        else if (IsNonAdditivePrime(text, radix))
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.NonAdditivePrime];
        }
        else if (IsAdditivePrime(text, radix))
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.AdditivePrime];
        }
        else if (IsPrime(text, radix))
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.Prime];
        }

        else if (IsNonAdditiveComposite(text, radix))
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.NonAdditiveComposite];
        }
        else if (IsAdditiveComposite(text, radix))
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.AdditiveComposite];
        }
        else if (IsComposite(text, radix))
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.Composite];
        }

        else
        {
            return NUMBER_TYPE_COLORS[(int)NumberType.None];
        }
    }

    public static Color[] NUMBER_KIND_BACKCOLORS =
    { 
        /* NumberKind.Deficient */          Color.FromArgb(255, 224, 255),
        /* NumberKind.Perfect */            Color.FromArgb(255, 128, 255),
        /* NumberKind.Abundant */           Color.FromArgb(255, 192, 255)
    };

    private static int s_max_size = Globals.MAX_NUMBERS;   // for generating big numbers in BigInteger Edition

    static Numbers()
    {
        if (!Directory.Exists(Globals.NUMBERS_FOLDER))
        {
            Directory.CreateDirectory(Globals.NUMBERS_FOLDER);
        }

        LoadPrimes();
        LoadAdditivePrimes();
        LoadNonAdditivePrimes();
        LoadComposites();
        LoadAdditiveComposites();
        LoadNonAdditiveComposites();
        LoadDeficientNumbers();
        LoadAbundantNumbers();
        LoadPerfectNumbers();
    }

    private static double s_tolerance = 0.000001D;
    public static bool IsMultiple(this int source, int target)
    {
        if (source == 0) return false;
        if (target == 0) return false;
        return ((source % target) == 0);
    }
    public static bool IsMultiple(this long source, long target)
    {
        if (source == 0) return false;
        if (target == 0) return false;
        return ((source % target) == 0);
    }
    public static bool IsMultiple(this float source, float target)
    {
        return source.IsMultiple(target, (float)s_tolerance);
    }
    public static bool IsMultiple(this decimal source, decimal target)
    {
        return source.IsMultiple(target, (decimal)s_tolerance);
    }
    public static bool IsMultiple(this double source, double target)
    {
        return source.IsMultiple(target, s_tolerance);
    }
    public static bool IsMultiple(this float source, float target, float tolerance)
    {
        if (source == 0) return false;
        if (target == 0) return false;
        return ((Math.Abs(source) > tolerance) && (Math.Abs((source % target)) < tolerance));
    }
    public static bool IsMultiple(this decimal source, decimal target, decimal tolerance)
    {
        if (source == 0) return false;
        if (target == 0) return false;
        return ((Math.Abs(source) > tolerance) && (Math.Abs((source % target)) < tolerance));
    }
    public static bool IsMultiple(this double source, double target, double tolerance)
    {
        if (source == 0) return false;
        if (target == 0) return false;
        return ((Math.Abs(source) > tolerance) && (Math.Abs((source % target)) < tolerance));
    }

    public static NumberType GetNumberType(long number)
    {
        if (number < 0L) number *= -1L;
        if (number == 0L) return NumberType.None;

        if (IsUnit(number)) return NumberType.Unit;
        else if (IsAdditivePrime(number)) return NumberType.AdditivePrime;
        else if (IsNonAdditivePrime(number)) return NumberType.NonAdditivePrime;
        if (IsAdditiveComposite(number)) return NumberType.AdditiveComposite;
        else if (IsNonAdditiveComposite(number)) return NumberType.NonAdditiveComposite;
        else return NumberType.None;
    }
    public static string GetNumberTypeAsString(long number)
    {
        if (number < 0L) number *= -1L;
        if (number == 0L) return "";

        if (IsUnit(number)) return "U";
        else if (IsAdditivePrime(number)) return "AP";
        else if (IsNonAdditivePrime(number)) return "XP";
        if (IsAdditiveComposite(number)) return "AC";
        else if (IsNonAdditiveComposite(number)) return "XC";
        else return "";
    }
    public static bool IsNumberType(long number, NumberType number_type)
    {
        if (number < 0L) number *= -1L;
        if (number == 0L) return false;

        switch (number_type)
        {
            case NumberType.Natural:
                {
                    return true;
                }
            case NumberType.Prime:
                {
                    return (IsPrime(number));
                }
            case NumberType.AdditivePrime:
                {
                    return (IsAdditivePrime(number));
                }
            case NumberType.NonAdditivePrime:
                {
                    return (IsNonAdditivePrime(number));
                }
            case NumberType.Composite:
                {
                    return (IsComposite(number));
                }
            case NumberType.AdditiveComposite:
                {
                    return (IsAdditiveComposite(number));
                }
            case NumberType.NonAdditiveComposite:
                {
                    return (IsNonAdditiveComposite(number));
                }
            case NumberType.Odd:
                {
                    return (IsOdd(number));
                }
            case NumberType.Even:
                {
                    return (IsEven(number));
                }
            case NumberType.None:
            default:
                {
                    return false;
                }
        }
    }
    /// <summary>
    /// Compare two numbers
    /// </summary>
    /// <param name="number1">first number</param>
    /// <param name="number2">second number</param>
    /// <param name="comparison_operator">operator for comparing the two numbers</param>
    /// <param name="remainder">remainder for the % operator. -1 means any remainder</param>
    /// <returns>returns comparison result</returns>
    public static bool Compare(long number1, long number2, ComparisonOperator comparison_operator, int remainder)
    {
        switch (comparison_operator)
        {
            case ComparisonOperator.Equal:
                {
                    return (number1 == number2);
                }
            case ComparisonOperator.NotEqual:
                {
                    return (number1 != number2);
                }
            case ComparisonOperator.LessThan:
                {
                    return (number1 < number2);
                }
            case ComparisonOperator.LessOrEqual:
                {
                    return (number1 <= number2);
                }
            case ComparisonOperator.GreaterThan:
                {
                    return (number1 > number2);
                }
            case ComparisonOperator.GreaterOrEqual:
                {
                    return (number1 >= number2);
                }
            case ComparisonOperator.DivisibleBy:
                {
                    if (number2 == 0) return false;
                    if (remainder == -1) // means any remainder
                    {
                        return ((number1 % number2) != 0);
                    }
                    else
                    {
                        // ignore 0
                        return ((number1 != 0) && (Math.Abs((number1 % number2)) == remainder));
                    }
                }
            case ComparisonOperator.IndivisibleBy:
                {
                    // ignore 0
                    if (number2 == 0) return false;
                    return ((number1 != 0) && (Math.Abs((number1 % number2)) != 0));
                }
            case ComparisonOperator.EqualSum:
                {
                    return (number1 == number2); //??? pass sum in number2
                }
            case ComparisonOperator.Reserved:
            default:
                {
                    return false;
                }
        }
    }
    public static int Reverse(int number)
    {
        int result = 0;
        while (number > 0)
        {
            result = (result * 10) + (number % 10);
            number /= 10;
        }
        return result;
    }
    public static long Reverse(long number)
    {
        long result = 0L;
        while (number > 0L)
        {
            result = (result * 10L) + (number % 10L);
            number /= 10L;
        }
        return result;
    }
    public static long Concatenate(long number1, long number2, Direction direction)
    {
        long result;

        string combination = "";
        string AAA = number1.ToString();
        string BBB = number2.ToString();
        if (direction == Direction.LeftToRight)
        {
            combination = AAA + BBB;
        }
        else
        {
            combination = BBB + AAA;
        }

        if (long.TryParse(combination, out result))
        {
            return result;
        }
        return -1L;
    }
    public static long Interlace(long number1, long number2, bool a_then_b, Direction direction)
    {
        long result;

        if (direction == Direction.RightToLeft)
        {
            number1 = Reverse(number1);
            number2 = Reverse(number2);
        }

        if ((number1 != -1L) && (number2 != -1L))
        {
            string combination = "";
            string AAA = number1.ToString();
            string BBB = number2.ToString();
            if (!a_then_b)
            {
                string temp = AAA;
                AAA = BBB;
                BBB = temp;
            }

            int a = AAA.Length;
            int b = BBB.Length;
            int min = Math.Min(a, b);

            for (int d = 0; d < min; d++)
            {
                combination += AAA[d].ToString() + BBB[d].ToString();
            }
            if (a > min)
            {
                combination += AAA.Substring(min);
            }
            else
            {
                combination += BBB.Substring(min);
            }

            if (long.TryParse(combination, out result))
            {
                return result;
            }
            return -1L;
        }
        return -1L;
    }
    public static long CrossOver(long number1, long number2, bool a_then_b, Direction direction)
    {
        long result;

        string combination = "";
        string AAA = number1.ToString();
        string BBB = number2.ToString();
        if (!a_then_b)
        {
            string temp = AAA;
            AAA = BBB;
            BBB = temp;
        }

        int a = AAA.Length;
        int b = BBB.Length;
        if ((a > 1) && (b > 1))
        {
            int mid_a = a / 2;
            string AAAHalf1 = AAA.Substring(0, mid_a - 1);
            string AAAHalf2 = AAA.Substring(mid_a - 1);

            int mid_b = b / 2;
            string BBBHalf1 = AAA.Substring(0, mid_b - 1);
            string BBBHalf2 = AAA.Substring(mid_b - 1);

            if (direction == Direction.LeftToRight)
            {
                combination = AAAHalf1 + BBBHalf2 + AAAHalf2 + BBBHalf1;
            }
            else
            {
                combination = BBBHalf1 + AAAHalf2 + BBBHalf2 + AAAHalf1;
            }

            if (long.TryParse(combination, out result))
            {
                return result;
            }
            return -1L;
        }
        return -1L;
    }
    public static bool AreReverse(long number1, long number2)
    {
        return (number1 == Reverse(number2));
    }
    public static bool AreConsecutive(List<int> numbers)
    {
        if (numbers != null)
        {
            for (int i = 0; i < numbers.Count - 1; i++)
            {
                if (numbers[i + 1] != numbers[i] + 1)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
    public static long SumOfNumbers(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfNumbers(number);
        }
        return 0L;
    }
    public static long SumOfNumbers(long number)
    {
        if (number < 0L) number *= -1L;
        return ((number * (number + 1)) / 2);
    }
    public static string GetNumbersString(long number)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append(i.ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static long SumOfSquareNumbers(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfSquareNumbers(number);
        }
        return 0L;
    }
    public static long SumOfSquareNumbers(long number)
    {
        if (number < 0L) number *= -1L;

        long result = 0L;
        for (int i = 1; i <= number; i++)
        {
            result += i * i;
        }
        return result;
    }
    public static string SumOfSquareNumbersString(long number)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((i * i).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static long SumOfCubicNumbers(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfCubicNumbers(number);
        }
        return 0L;
    }
    public static long SumOfCubicNumbers(long number)
    {
        if (number < 0L) number *= -1L;

        long result = 0L;
        for (int i = 1; i <= number; i++)
        {
            result += i * i * i;
        }
        return result;
    }
    public static string SumOfCubicNumbersString(long number)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((i * i * i).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }

    public static bool IsUnit(long number)
    {
        if (number < 0L) number *= -1L;
        return (number == 1L);
    }
    public static bool IsUnit(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsUnit(number);
    }
    public static bool IsOdd(long number)
    {
        if (number < 0L) number *= -1L;
        return ((number % 2) == 1L);
    }
    public static bool IsOdd(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsOdd(number);
    }
    public static bool IsEven(long number)
    {
        if (number < 0L) number *= -1L;
        return ((number % 2) == 0L);
    }
    public static bool IsEven(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsEven(number);
    }
    // http://digitalbush.com/2010/02/26/sieve-of-eratosthenes-in-csharp/

    //IList<int> FindPrimes(int max)
    //{
    //    var result = new List<int>((int)(max / (Math.Log(max) - 1.08366)));
    //    var maxSquareRoot = Math.Sqrt(max);
    //    var eliminated = new System.Collections.BitArray(max + 1);
    //    result.Add(2);
    //    for (int i = 3; i <= max; i += 2)
    //    {
    //        if (!eliminated[i])
    //        {
    //            if (i < maxSquareRoot)
    //            {
    //                for (int j = i * i; j <= max; j += 2 * i)
    //                    eliminated[j] = true;
    //            }
    //            result.Add(i);
    //        }
    //    }
    //    return result;
    //}

    // Algorithm Optimizations
    // I cut my work in half by treating the special case of '2'.
    // We know that 2 is prime and all even numbers thereafter are not.
    // So, we'll add two immediately and then start looping at 3 only checking odd numbers from there forward.

    // After we've found a prime, we only need to eliminate numbers from it's square and forward.
    // Let's say we want to find all prime numbers up to 100 and we've just identified 7 as a prime.
    // Per the algorithm, I'll need to eliminate 2*7, 3*7 ,4*7, 5*7, 6*7, 7*7 ,8*7 ,9*7, 10*7 ,11*7, 12*7 ,13*7 and 14*7.
    // None of the even multiples matter (even times an odd is always even) and none of the multiples
    // up to the square of the prime matter since we've already done those multiples in previous loops.
    // So really we only have to eliminate 7*7, 9*7, 11*7 and 13*7.
    // That's a 9 fewer iterations and those savings become more fruitful the deeper you go!

    // The last optimization is the square root calculation and check.
    // We know from above that we only need to start eliminating beginning at the square of the current prime.
    // Therefore it also makes sense that we can stop even trying once we get past the to square root of the max.
    // This saves a bunch more iterations.

    // Language Optimizations
    // Originally I had started by returning an IEnumerable<int>.
    // I wasn't using the list you see above and instead I was using yield return i.
    // I really like that syntax, but once I got to the VB.net version (Coming Soon!),
    // I didn't have a direct translation for the yield keyword.
    // I took the lazy route in the VB version and just stuffed it all into a list and returned that.
    // To my surprise it was faster! I went back and changed the C# version above and it performed better.
    // I'm not sure why, but I'm going with it.

    // What do you think that you get when do a sizeof(bool) in C#?
    // I was surprised to find out that my trusty booleans actually take up a whole byte instead of a single bit.
    // I speculate that there is a performance benefit that all of your types fit into a byte level offset in memory.
    // I was thrilled to find out that we have a BitArray class that is useful for situations above
    // when you need to store a lot of booleans and you need them to only take up a bit in memory.
    // I'm not sure it helped anything, but I feel better knowing I'm using the least amount of memory possible.

    // Conclusion
    // Despite the fact that I know C# really well, I'm very thrilled that I was able to learn a few things about the language.
    // Also, I'm really happy with the performance of this reference implementation.
    // On my machine (2.66 GHz Core2 Duo and 2 GB of RAM) I can find all of the primes under 1,000,000 in 19ms.
    // I think I've squeezed all I can out of this version.
    // Please let me know if you see something I missed or did wrong and I'll make adjustments.

    // EDIT: I just added one more optimization that's worth noting.
    // Instead of constructing my list with an empty constructor, I can save a several milliseconds 
    // off the larger sets by specifying a start m_size of the internal m_candidates structure behind the list.
    // If I set this m_size at or slightly above the end count of prime numbers,
    // then I avoid a lot of costly m_candidates copying as the m_candidates bounds keep getting hit.
    // It turns out that there is quite a bit of math involved in accurately predicting the number of primes underneath a given number.
    // I chose to cheat and just use Legendre's constant with the Prime Number Theorem which is close enough for my purposes.
    // I can now calculate all primes under 1,000,000 in 10ms on my machine. Neat!
    //private static List<int> GeneratePrimesUsingSieveOfEratosthenes(int limit)
    //{
    //    // guard against parameter out of range
    //    if (limit < 2)
    //    {
    //        return new List<int>();
    //    }

    //    // Legendre's constant to approximate the number of primes below N
    //    int max_primes = (int)Math.Ceiling((limit / (Math.Log(limit) - 1.08366)));
    //    if (max_primes < 1)
    //    {
    //        max_primes = 1;
    //    }
    //    List<int> primes = new List<int>(max_primes);

    //    // bit m_candidates to cross out multiples of primes successively
    //    BitArray candidates = new BitArray(limit + 1, true);

    //    // add number 2 as prime
    //    primes.Add(2);
    //    // and cross out all its multiples
    //    for (int j = 2 * 2; j <= limit; j += 2)
    //    {
    //        candidates[j] = false;
    //    }

    //    // get the ceiling of sqrt of N
    //    int limit_sqrt = (int)Math.Ceiling(Math.Sqrt(limit));

    //    // start from 3 and skip even numbers
    //    // don't go beyond limit or overflow into negative
    //    for (int i = 3; (i > 0 && i <= limit); i += 2)
    //    {
    //        if (candidates[i])
    //        {
    //            // add not-crossed out candidate
    //            primes.Add(i);

    //            // upto the sqrt of N
    //            if (i <= limit_sqrt)
    //            {
    //                // and cross out non-even multiples from i*i and skip even i multiples
    //                // don't go beyond limit, or overflow into negative
    //                for (int j = i * i; (j > 0 && j <= limit); j += 2 * i)
    //                {
    //                    candidates[j] = false;
    //                }
    //            }
    //        }
    //    }

    //    return primes;
    //}
    //private static List<int> GeneratePrimesUsingDivisionTrial(int limit)
    //{
    //    // guard against parameter out of range
    //    if (limit < 2)
    //    {
    //        return new List<int>();
    //    }

    //    // Legendre's constant to approximate the number of primes below N
    //    int max_primes = (int)Math.Ceiling((limit / (Math.Log(limit) - 1.08366)));
    //    if (max_primes < 1)
    //    {
    //        max_primes = 1;
    //    }
    //    List<int> primes = new List<int>(max_primes);

    //    primes.Add(2);

    //    for (int i = 3; i <= limit; i += 2)
    //    {
    //        bool is_prime = true;
    //        for (int j = 3; j <= (int)Math.Sqrt(i); j += 2)
    //        {
    //            if (i % j == 0)
    //            {
    //                is_prime = false;
    //                break;
    //            }
    //        }

    //        if (is_prime)
    //        {
    //            primes.Add(i);
    //        }
    //    }

    //    return primes;
    //}
    public static bool IsPrime(long number)
    {
        if (number < 0L) number *= -1L;

        if (number == 0L)        // 0 is neither prime nor composite
            return false;

        if (number == 1L)        // 1 is the unit, indivisible
            return false;        // NOT prime

        if (number == 2L)        // 2 is the first prime
            return true;

        if (number % 2L == 0L)   // exclude even numbers to speed up search
            return false;

        long sqrt = (long)Math.Round(Math.Sqrt(number));
        for (long i = 3L; i <= sqrt; i += 2L)
        {
            if ((number % i) == 0L)
            {
                return false;
            }
        }
        return true;
    }
    public static bool IsPrime(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsPrime(number);
    }
    public static bool IsAdditivePrime(long number)
    {
        if (IsPrime(number))
        {
            return IsPrime(DigitSum(number));
        }
        return false;
    }
    public static bool IsAdditivePrime(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsAdditivePrime(number);
    }
    public static bool IsNonAdditivePrime(long number)
    {
        if (IsPrime(number))
        {
            return !IsPrime(DigitSum(number));
        }
        return false;
    }
    public static bool IsNonAdditivePrime(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsNonAdditivePrime(number);
    }
    public static bool IsComposite(long number)
    {
        if (number < 0L) number *= -1L;

        if (number == 0L)        // 0 is NOT composite
            return false;

        if (number == 1L)        // 1 is the unit, indivisible
            return false;        // NOT composite

        if (number == 2L)        // 2 is the first prime
            return false;

        if (number % 2L == 0L)   // even numbers are composite
            return true;

        long sqrt = (long)Math.Round(Math.Sqrt(number));
        for (long i = 3L; i <= sqrt; i += 2L)
        {
            if ((number % i) == 0L)
            {
                return true;
            }
        }
        return false;
    }
    public static bool IsComposite(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsComposite(number);
    }
    public static bool IsAdditiveComposite(long number)
    {
        if (IsComposite(number))
        {
            return IsComposite(DigitSum(number));
        }
        return false;
    }
    public static bool IsAdditiveComposite(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsAdditiveComposite(number);
    }
    public static bool IsNonAdditiveComposite(long number)
    {
        if (IsComposite(number))
        {
            return !IsComposite(DigitSum(number));
        }
        return false;
    }
    public static bool IsNonAdditiveComposite(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return IsNonAdditiveComposite(number);
    }
    /// <summary>
    /// Check if three numbers are additive primes and their L2R and R2L concatenations are additive primes too.
    /// <para>Example:</para>
    /// <para>Quran chapter The Key has:</para>
    /// <para>(7, 29, 139) are primes with primes digit sums (7=7, 2+9=11, 1+3+9=13)</para>
    /// <para>and 729139, 139297 primes with prime digit sum (1+3+9+2+9+7=31)</para>
    /// </summary>
    /// <param name="n1"></param>
    /// <param name="n2"></param>
    /// <param name="n3"></param>
    /// <returns></returns>
    public static bool ArePrimeTriplets(string text1, string text2, string text3, int radix)
    {
        long number1 = Radix.Decode(text1, radix);
        long number2 = Radix.Decode(text2, radix);
        long number3 = Radix.Decode(text3, radix);
        return ArePrimeTriplets(number1, number2, number3);
    }
    public static bool ArePrimeTriplets(long number1, long number2, long number3)
    {
        if (IsAdditivePrime(number1) && IsAdditivePrime(number2) && IsAdditivePrime(number3))
        {
            long l2r;
            long r2l;
            if (long.TryParse(number1.ToString() + number2.ToString() + number3.ToString(), out l2r))
            {
                if (long.TryParse(number3.ToString() + number2.ToString() + number1.ToString(), out r2l))
                {
                    return (IsAdditivePrime(l2r) && IsAdditivePrime(r2l));
                }
            }
        }
        return false;
    }
    public static List<int> SieveOfEratosthenes(int limit)
    {
        // guard against parameter out of range
        if ((limit < 2) || (limit > (int)(int.MaxValue * 0.9999999)))
        {
            return new List<int>();
        }

        // Legendre's constant to approximate the number of primes below N
        int max_primes = (int)Math.Ceiling((limit / (Math.Log(limit) - 1.08366)));
        if (max_primes < 1)
        {
            max_primes = 1;
        }
        List<int> primes = new List<int>(max_primes);

        // bit m_candidates to cross out multiples of primes successively
        // from N^2, jumping 2N at a time (to skip even multiples)
        BitArray candidates = new BitArray(limit + 1, true);

        // add number 2 as prime
        primes.Add(2);
        //// no need to cross out evens as we are skipping them anyway
        //// and cross out all its multiples
        //for (int j = 2 * 2; j <= limit; j += 2)
        //{
        //    candidates[j] = false;
        //}

        // get the ceiling of sqrt of N
        int sqrt_of_limit = (int)Math.Ceiling(Math.Sqrt(limit));

        // start from 3 and skip even numbers
        // don't go beyond limit or overflow into negative
        for (int i = 3; (i > 0 && i <= limit); i += 2)
        {
            // if not-crossed out candidate yet
            if (candidates[i])
            {
                // add candidate
                primes.Add(i);

                // upto the sqrt of N
                if (i <= sqrt_of_limit)
                {
                    // and cross out non-even multiples from i*i and skip even i multiples
                    // don't go beyond limit, or overflow into negative
                    for (int j = i * i; (j > 0 && j <= limit); j += 2 * i)
                    {
                        candidates[j] = false;
                    }
                }
            }
        }
        return primes;
    }

    public static int GetDigitValue(char c)
    {
        int result = -1;
        if (Char.IsDigit(c)) // 0..9
        {
            result = (int)char.GetNumericValue(c);
        }
        else // A..Z
        {
            result = c.CompareTo('A') + 10;
        }
        return result;
    }
    public static List<int> GetDigits(long number)
    {
        if (number < 0L) number *= -1L;

        List<int> result = new List<int>();
        string str = number.ToString();
        for (int i = 0; i < str.Length; i++)
        {
            result.Add((int)Char.GetNumericValue(str[i]));
        }
        return result;
    }
    public static List<char> GetDigits(string text)
    {
        List<char> result = new List<char>();
        if (text.Length > 0)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (Char.IsDigit(c))
                {
                    result.Add(text[i]);
                }
            }
        }
        return result;
    }
    public static int DigitCount(long number)
    {
        if (number < 0L) number *= -1L;
        return DigitCount(number.ToString());
    }
    public static int DigitCount(string text)
    {
        return DigitCount(text, Numbers.DEFAULT_RADIX);
        //int result = 0;
        //if (text.Length > 0)
        //{
        //    for (int i = 0; i < text.Length; i++)
        //    {
        //        char c = text[i];
        //        if (Char.IsDigit(c))
        //        {
        //            result++;
        //        }
        //    }
        //}
        //return result;
    }
    public static int DigitCount(long number, int radix)
    {
        if (number < 0L) number *= -1L;
        return DigitCount(number.ToString(), radix);
    }
    public static int DigitCount(string text, int radix)
    {
        int result = 0;
        if (text.Length > 0)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (((c >= ('0')) && (c < ('9' - 9 + radix))) || ((c >= ('A')) && (c < ('A' - 10 + radix))))
                {
                    result++;
                }
            }
        }
        return result;
    }
    public static int DigitSum(long number)
    {
        if (number < 0L) number *= -1L;
        return DigitSum(number.ToString());
    }
    public static int DigitSum(string text)
    {
        int result = 0;
        if (text.Length > 0)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (Char.IsDigit(c))
                {
                    result += GetDigitValue(c);
                }
            }
        }
        return result;
    }
    public static int DigitSum(long number, int radix)
    {
        if (number < 0L) number *= -1L;
        return DigitSum(number.ToString(), radix);
    }
    public static int DigitSum(string text, int radix)
    {
        int result = 0;
        if (text.Length > 0)
        {
            for (int i = 0; i < text.Length; i++)
            {
                result += (int)Radix.Decode(text[i].ToString(), radix);
            }
        }
        return result;
    }

    public static long SumOfNumberDigitSums(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfNumberDigitSums(number, DEFAULT_RADIX);
    }
    public static long SumOfNumberDigitSums(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfNumberDigitSums(number, DEFAULT_RADIX);
        }
        return 0L;
    }
    public static long SumOfNumberDigitSums(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        //????? method works correctly for radix = 10 only
        // http://ideone.com/ik8iE6

        long result = 0L;
        long pos = 1L;
        long previous = 0L;
        long count = 0L;

        while (number > 0)
        {
            long r = number % radix;
            number /= radix;

            result += (r * (r - 1L) / 2L) * pos + r * (((radix - 1L) * (radix / 2L)) * count * pos / radix) + r * (previous + 1L);
            previous += pos * r;
            count++;

            pos *= radix;
        }
        return result;
    }
    public static long SumOfNumberDigitSums(string text, int radix)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfNumberDigitSums(number, radix);
        }
        return 0L;
    }
    public static string SumOfDigitSumsString(long number, int radix)
    {
        if (number < 0L) number *= -1L;
        if (number > 1000000000000L) return "";

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((DigitSum(i, radix)).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static string GetNumberDigitSumsString(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfDigitSumsString(number, DEFAULT_RADIX);
    }
    public static long SumOfSquareDigitSums(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfSquareDigitSums(number, DEFAULT_RADIX);
    }
    public static long SumOfSquareDigitSums(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfSquareDigitSums(number, DEFAULT_RADIX);
        }
        return 0L;
    }
    public static string SumOfSquareDigitSumsString(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfSquareDigitSumsString(number, DEFAULT_RADIX);
    }
    public static long SumOfSquareDigitSums(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        long result = 0L;
        for (int i = 1; i <= number; i++)
        {
            result += (DigitSum(i, radix) * DigitSum(i, radix));
        }
        return result;
    }
    public static long SumOfSquareDigitSums(string text, int radix)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfSquareDigitSums(number, radix);
        }
        return 0L;
    }
    public static string SumOfSquareDigitSumsString(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((DigitSum(i, radix) * DigitSum(i, radix)).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static long SumOfCubicDigitSums(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfCubicDigitSums(number, DEFAULT_RADIX);
    }
    public static long SumOfCubicDigitSums(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfCubicDigitSums(number, DEFAULT_RADIX);
        }
        return 0L;
    }
    public static string SumOfCubicDigitSumsString(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfCubicDigitSumsString(number, DEFAULT_RADIX);
    }
    public static long SumOfCubicDigitSums(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        long result = 0L;
        for (int i = 1; i <= number; i++)
        {
            result += (DigitSum(i, radix) * DigitSum(i, radix) * DigitSum(i, radix));
        }
        return result;
    }
    public static long SumOfCubicDigitSums(string text, int radix)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfCubicDigitSums(number, radix);
        }
        return 0L;
    }
    public static string SumOfCubicDigitSumsString(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((DigitSum(i, radix) * DigitSum(i, radix) * DigitSum(i, radix)).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }

    public static int DigitalRoot(long number)
    {
        if (number < 0L) number *= -1L;
        return DigitalRoot(number, DEFAULT_RADIX);
    }
    public static int DigitalRoot(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return DigitalRoot(number);
        }
        return 0;
    }
    public static int DigitalRoot(long number, int radix)
    {
        if (number < 0L) number *= -1L;
        return (int)(1L + (number - 1L) % (radix - 1));
    }
    public static int DigitalRoot(string text, int radix)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return DigitalRoot(number, radix);
        }
        return 0;
    }
    public static long SumNumberDigitalRoots(long number)
    {
        if (number < 0L) number *= -1L;
        return SumNumberDigitalRoots(number, DEFAULT_RADIX);
    }
    public static long SumNumberDigitalRoots(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumNumberDigitalRoots(number);
        }
        return 0L;
    }
    public static long SumNumberDigitalRoots(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        long d = number / (radix - 1L);
        long r = number % (radix - 1L);
        long sum_1_to_radix_minus_1 = (radix - 1L) * (radix) / 2;
        return (d * sum_1_to_radix_minus_1 + r * (r + 1L) / 2L);
    }
    public static long SumNumberDigitalRoots(string text, int radix)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumNumberDigitalRoots(number, radix);
        }
        return 0L;
    }
    public static string SumOfDigitalRootsString(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((DigitalRoot(i, radix)).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static string GetNumberDigitalRootsString(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfDigitalRootsString(number, DEFAULT_RADIX);
    }
    public static long SumOfSquareDigitalRoots(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfSquareDigitalRoots(number, DEFAULT_RADIX);
    }
    public static long SumOfSquareDigitalRoots(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfSquareDigitalRoots(number);
        }
        return 0L;
    }
    public static string SumOfSquareDigitalRootsString(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfSquareDigitalRootsString(number, DEFAULT_RADIX);
    }
    public static long SumOfSquareDigitalRoots(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        long result = 0L;
        for (int i = 1; i <= number; i++)
        {
            result += (DigitalRoot(i, radix) * DigitalRoot(i, radix));
        }
        return result;
    }
    public static long SumOfSquareDigitalRoots(string text, int radix)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfSquareDigitalRoots(number, radix);
        }
        return 0L;
    }
    public static string SumOfSquareDigitalRootsString(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((DigitalRoot(i, radix) * DigitalRoot(i, radix)).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static long SumOfCubicDigitalRoots(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfCubicDigitalRoots(number, DEFAULT_RADIX);
    }
    public static long SumOfCubicDigitalRoots(string text)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfCubicDigitalRoots(number);
        }
        return 0L;
    }
    public static string SumOfCubicDigitalRootsString(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfCubicDigitalRootsString(number, DEFAULT_RADIX);
    }
    public static long SumOfCubicDigitalRoots(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        long result = 0L;
        for (int i = 1; i <= number; i++)
        {
            result += (DigitalRoot(i, radix) * DigitalRoot(i, radix) * DigitalRoot(i, radix));
        }
        return result;
    }
    public static long SumOfCubicDigitalRoots(string text, int radix)
    {
        long number;
        if (long.TryParse(text, out number))
        {
            return SumOfCubicDigitalRoots(number, radix);
        }
        return 0L;
    }
    public static string SumOfCubicDigitalRootsString(long number, int radix)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        for (int i = 1; i <= number; i++)
        {
            str.Append((DigitalRoot(i, radix) * DigitalRoot(i, radix) * DigitalRoot(i, radix)).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }

    private static List<long> s_primes = null;
    private static List<long> s_additive_primes = null;
    private static List<long> s_non_additive_primes = null;
    public static List<long> Primes
    {
        get
        {
            if (s_primes == null)
            {
                GeneratePrimes(s_max_size);
            }
            return s_primes;
        }
    }
    public static List<long> AdditivePrimes
    {
        get
        {
            if (s_additive_primes == null)
            {
                GenerateAdditivePrimes(s_max_size);
            }
            return s_additive_primes;
        }
    }
    public static List<long> NonAdditivePrimes
    {
        get
        {
            if (s_non_additive_primes == null)
            {
                GenerateNonAdditivePrimes(s_max_size);
            }
            return s_non_additive_primes;
        }
    }
    public static int PrimeIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (IsPrime(number))
        {
            if (s_primes == null)
            {
                GeneratePrimes(s_max_size);
            }
            return BinarySearch(s_primes, number);
        }
        return -1;
    }
    public static int AdditivePrimeIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (IsAdditivePrime(number))
        {
            if (s_additive_primes == null)
            {
                GenerateAdditivePrimes(s_max_size);
            }
            return BinarySearch(s_additive_primes, number);
        }
        return -1;
    }
    public static int NonAdditivePrimeIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (IsNonAdditivePrime(number))
        {
            if (s_non_additive_primes == null)
            {
                GenerateNonAdditivePrimes(s_max_size);
            }
            return BinarySearch(s_non_additive_primes, number);
        }
        return -1;
    }
    public static int PrimeIndexOf(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return PrimeIndexOf(number);
    }
    public static int AdditivePrimeIndexOf(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return AdditivePrimeIndexOf(number);
    }
    public static int NonAdditivePrimeIndexOf(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return NonAdditivePrimeIndexOf(number);
    }
    private static void GeneratePrimes(int max)
    {
        //if (s_primes != null)
        //{
        //    int primes_upto_max = (int)(max / (Math.Log(max) + 1));
        //    if (s_primes.Count >= primes_upto_max)
        //    {
        //        return; // we already have a large list, no need to RE-generate new one
        //    }
        //}

        if (s_primes == null)
        {
            BitArray composites = new BitArray(max + 1);

            s_primes = new List<long>();

            s_primes.Add(2L);

            // process odd numbers // 3, 5, 7, 9, 11, ..., max
            long sqrt = (long)Math.Round(Math.Sqrt(max)) + 1L;
            for (int i = 3; i <= max; i += 2)
            {
                if (!composites[i])
                {
                    s_primes.Add(i);

                    // mark off multiples of i starting from i*i and skipping even "i"s
                    if (i < sqrt)
                    {
                        for (int j = i * i; j <= max; j += 2 * i)
                        {
                            composites[j] = true;
                        }
                    }
                }
            }
        }
    }
    private static void GenerateAdditivePrimes(int max)
    {
        //// re-generate for new max if larger
        //GeneratePrimes(max);

        if (s_additive_primes == null)
        {
            if (s_primes == null)
            {
                GeneratePrimes(max);
            }

            if (s_primes != null)
            {
                s_additive_primes = new List<long>();
                int count = s_primes.Count;
                for (int i = 0; i < count; i++)
                {
                    if (IsPrime(DigitSum(s_primes[i])))
                    {
                        s_additive_primes.Add(s_primes[i]);
                    }
                }
            }
        }
    }
    private static void GenerateNonAdditivePrimes(int max)
    {
        //// re-generate for new max if larger
        //GeneratePrimes(max);

        if (s_non_additive_primes == null)
        {
            if (s_primes == null)
            {
                GeneratePrimes(max);
            }

            if (s_primes != null)
            {
                s_non_additive_primes = new List<long>();
                int count = s_primes.Count;
                for (int i = 0; i < count; i++)
                {
                    if (!IsPrime(DigitSum(s_primes[i])))
                    {
                        s_non_additive_primes.Add(s_primes[i]);
                    }
                }
            }
        }
    }
    private static string s_primes_filename = "primes.txt";
    private static string s_additive_primes_filename = "additive_primes.txt";
    private static string s_non_additive_primes_filename = "non_additive_primes.txt";
    private static void LoadPrimes()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_primes_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_primes = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_primes.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void LoadAdditivePrimes()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_additive_primes_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_additive_primes = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_additive_primes.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void LoadNonAdditivePrimes()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_non_additive_primes_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_non_additive_primes = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_non_additive_primes.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void SavePrimes()
    {
        if (s_primes != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_primes_filename;
                FileHelper.SaveValues(filename, s_primes);
            }
        }
    }
    private static void SaveAdditivePrimes()
    {
        if (s_additive_primes != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_additive_primes_filename;
                FileHelper.SaveValues(filename, s_additive_primes);
            }
        }
    }
    private static void SaveNonAdditivePrimes()
    {
        if (s_non_additive_primes != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_non_additive_primes_filename;
                FileHelper.SaveValues(filename, s_non_additive_primes);
            }
        }
    }

    private static List<long> s_composites = null;
    private static List<long> s_additive_composites = null;
    private static List<long> s_non_additive_composites = null;
    public static List<long> Composites
    {
        get
        {
            if (s_composites == null)
            {
                GenerateComposites(s_max_size / 2);
            }
            return s_composites;
        }
    }
    public static List<long> AdditiveComposites
    {
        get
        {
            if (s_additive_composites == null)
            {
                GenerateAdditiveComposites(s_max_size / 2);
            }
            return s_additive_composites;
        }
    }
    public static List<long> NonAdditiveComposites
    {
        get
        {
            if (s_non_additive_composites == null)
            {
                GenerateNonAdditiveComposites(s_max_size / 2);
            }
            return s_non_additive_composites;
        }
    }
    public static int CompositeIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (IsComposite(number))
        {
            int max = s_max_size / 2;
            if (s_composites == null)
            {
                GenerateComposites(max);
            }
            return BinarySearch(s_composites, number);
        }
        return -1;
    }
    public static int AdditiveCompositeIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (IsAdditiveComposite(number))
        {
            int max = s_max_size / 2;
            if (s_additive_composites == null)
            {
                GenerateAdditiveComposites(max);
            }
            return BinarySearch(s_additive_composites, number);
        }
        return -1;
    }
    public static int NonAdditiveCompositeIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (IsNonAdditiveComposite(number))
        {
            int max = s_max_size / 2;
            if (s_non_additive_composites == null)
            {
                GenerateNonAdditiveComposites(max);
            }
            return BinarySearch(s_non_additive_composites, number);
        }
        return -1;
    }
    public static int CompositeIndexOf(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return CompositeIndexOf(number);
    }
    public static int AdditiveCompositeIndexOf(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return AdditiveCompositeIndexOf(number);
    }
    public static int NonAdditiveCompositeIndexOf(string text, int radix)
    {
        long number = Radix.Decode(text, radix);
        return NonAdditiveCompositeIndexOf(number);
    }
    private static void GenerateComposites(int max)
    {
        //if (s_composites != null)
        //{
        //    int primes_upto_max = (int)(max / (Math.Log(max) + 1));
        //    if (s_composites.Count >= (max - primes_upto_max))
        //    {
        //        return; // we already have a large list, no need to RE-generate new one
        //    }
        //}

        if (s_composites == null)
        {
            BitArray composites = new BitArray(max + 1);

            s_composites = new List<long>(max);

            for (int i = 4; i <= max; i += 2)
            {
                composites[i] = true;
            }

            // process odd numbers // 3, 5, 7, 9, 11, ..., max
            long sqrt = (long)Math.Round(Math.Sqrt(max)) + 1L;
            for (int i = 3; i <= max; i += 2)
            {
                if (!composites[i])
                {
                    // mark off multiples of i
                    if (i <= sqrt)
                    {
                        for (int j = i * i; j <= max; j += 2 * i)
                        {
                            composites[j] = true;
                        }
                    }
                }
            }

            for (int i = 4; i <= max; i++)
            {
                if (composites[i])
                {
                    s_composites.Add(i);
                }
            }
        }
    }
    private static void GenerateAdditiveComposites(int max)
    {
        //// re-generate for new max if larger
        //GenerateComposites(max);

        if (s_additive_composites == null)
        {
            if (s_composites == null)
            {
                GenerateComposites(max);
            }

            if (s_composites != null)
            {
                s_additive_composites = new List<long>();
                int count = s_composites.Count;
                for (int i = 0; i < count; i++)
                {
                    if (IsComposite(DigitSum(s_composites[i])))
                    {
                        s_additive_composites.Add(s_composites[i]);
                    }
                }
            }
        }
    }
    private static void GenerateNonAdditiveComposites(int max)
    {
        //// re-generate for new max if larger
        //GenerateComposites(max);

        if (s_non_additive_composites == null)
        {
            if (s_composites == null)
            {
                GenerateComposites(max);
            }

            if (s_composites != null)
            {
                s_non_additive_composites = new List<long>();
                int count = s_composites.Count;
                for (int i = 0; i < count; i++)
                {
                    if (!IsComposite(DigitSum(s_composites[i])))
                    {
                        s_non_additive_composites.Add(s_composites[i]);
                    }
                }
            }
        }
    }
    private static string s_composites_filename = "composites.txt";
    private static string s_additive_composites_filename = "additive_composites.txt";
    private static string s_non_additive_composites_filename = "non_additive_composites.txt";
    private static void LoadComposites()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_composites_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_composites = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_composites.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void LoadAdditiveComposites()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_additive_composites_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_additive_composites = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_additive_composites.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void LoadNonAdditiveComposites()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_non_additive_composites_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_non_additive_composites = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_non_additive_composites.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void SaveComposites()
    {
        if (s_composites != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_composites_filename;
                FileHelper.SaveValues(filename, s_composites);
            }
        }
    }
    private static void SaveAdditiveComposites()
    {
        if (s_additive_composites != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_additive_composites_filename;
                FileHelper.SaveValues(filename, s_additive_composites);
            }
        }
    }
    private static void SaveNonAdditiveComposites()
    {
        if (s_non_additive_composites != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_non_additive_composites_filename;
                FileHelper.SaveValues(filename, s_non_additive_composites);
            }
        }
    }

    public static NumberKind GetNumberKind(long number)
    {
        if (number < 0L) number *= -1L;
        if (number == 0L) return NumberKind.Deficient;
        if (number == 1L) return NumberKind.Deficient;
        if (number > 1000000000000L) return NumberKind.Deficient;

        long sum_of_proper_divisors = SumOfProperDivisors(number);
        if (sum_of_proper_divisors < number)
        {
            return NumberKind.Deficient;
        }
        else if (sum_of_proper_divisors == number)
        {
            return NumberKind.Perfect;
        }
        else //if (sum_of_proper_divisors > number)
        {
            return NumberKind.Abundant;
        }
    }
    private static List<long> s_deficient_numbers;
    public static List<long> DeficientNumbers
    {
        get
        {
            if (s_deficient_numbers == null)
            {
                GenerateDeficientNumbers(s_max_size / 16);
            }
            return s_deficient_numbers;
        }
    }
    public static int DeficientNumberIndexOf(long number)
    {
        if (number < 0L) number *= -1L;
        if (s_deficient_numbers == null)
        {
            GenerateDeficientNumbers(s_max_size / 16);
        }
        return BinarySearch(s_deficient_numbers, number);
    }
    private static void GenerateDeficientNumbers(int max)
    {
        s_deficient_numbers = new List<long>(max);
        for (int i = 0; i < max; i++)
        {
            long sum_of_proper_divisors = SumOfProperDivisors(i);
            if (sum_of_proper_divisors < i)
            {
                s_deficient_numbers.Add(i);
            }
        }
    }
    /// <summary>
    /// <para>Deficient number is a number with the sum of its proper divisors is less than itself</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsDeficientNumber(long number)
    {
        if (number < 0L) number *= -1L;
        return DeficientNumbers.Contains(number);
    }
    private static string s_deficient_numbers_filename = "deficient_numbers.txt";
    private static void LoadDeficientNumbers()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_deficient_numbers_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_deficient_numbers = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_deficient_numbers.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void SaveDeficientNumbers()
    {
        if (s_deficient_numbers != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_deficient_numbers_filename;
                FileHelper.SaveValues(filename, s_deficient_numbers);
            }
        }
    }

    private static List<long> s_perfect_numbers;
    public static List<long> PerfectNumbers
    {
        get
        {
            if (s_perfect_numbers == null)
            {
                GeneratePerfectNumbers(s_max_size / 16);
            }
            return s_perfect_numbers;
        }
    }
    public static int PerfectNumberIndexOf(long number)
    {
        if (number < 0L) number *= -1L;
        if (s_perfect_numbers == null)
        {
            GeneratePerfectNumbers(s_max_size / 16);
        }
        return BinarySearch(s_perfect_numbers, number);
    }
    private static void GeneratePerfectNumbers(int max)
    {
        s_perfect_numbers = new List<long>(max) { 1 }; // 1 is 1st perfect number
        for (int i = 2; i < max; i++)
        {
            long sum_of_proper_divisors = SumOfProperDivisors(i);
            if (sum_of_proper_divisors < i)
            {
                s_perfect_numbers.Add(i);
            }
        }
    }
    /// <summary>
    /// <para>Perfect number is a number with the sum of its proper divisors equals itself</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsPerfectNumber(long number)
    {
        if (number < 0L) number *= -1L;
        return PerfectNumbers.Contains(number);
    }
    private static string s_perfect_numbers_filename = "perfect_numbers.txt";
    private static void LoadPerfectNumbers()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_perfect_numbers_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_perfect_numbers = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_perfect_numbers.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void SavePerfectNumbers()
    {
        if (s_perfect_numbers != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_perfect_numbers_filename;
                FileHelper.SaveValues(filename, s_perfect_numbers);
            }
        }
    }

    private static List<long> s_abundant_numbers;
    public static List<long> AbundantNumbers
    {
        get
        {
            if (s_abundant_numbers == null)
            {
                GenerateAbundantNumbers(s_max_size / 16);
            }
            return s_abundant_numbers;
        }
    }
    public static int AbundantNumberIndexOf(long number)
    {
        if (number < 0L) number *= -1L;
        if (s_abundant_numbers == null)
        {
            GenerateAbundantNumbers(s_max_size / 16);
        }
        return BinarySearch(s_abundant_numbers, number);
    }
    private static void GenerateAbundantNumbers(int max)
    {
        s_abundant_numbers = new List<long>(max);
        for (int i = 0; i < max; i++)
        {
            long sum_of_proper_divisors = SumOfProperDivisors(i);
            if (sum_of_proper_divisors > i)
            {
                s_abundant_numbers.Add(i);
            }
        }
    }
    /// <summary>
    /// <para>Abundant number is a number with the sum of its proper divisors is greater than itself</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsAbundantNumber(long number)
    {
        if (number < 0L) number *= -1L;
        return AbundantNumbers.Contains(number);
    }
    private static string s_abundant_numbers_filename = "abundant_numbers.txt";
    private static void LoadAbundantNumbers()
    {
        string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_abundant_numbers_filename;
        if (File.Exists(filename))
        {
            FileHelper.WaitForReady(filename);

            s_abundant_numbers = new List<long>();
            using (StreamReader reader = new StreamReader(filename))
            {
                string line = null;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        long number;
                        if (long.TryParse(line, out number))
                        {
                            s_abundant_numbers.Add(number);
                        }
                    }
                }
            }
        }
    }
    private static void SaveAbundantNumbers()
    {
        if (s_abundant_numbers != null)
        {
            if (Directory.Exists(Globals.NUMBERS_FOLDER))
            {
                string filename = Globals.NUMBERS_FOLDER + Path.DirectorySeparatorChar + s_abundant_numbers_filename;
                FileHelper.SaveValues(filename, s_abundant_numbers);
            }
        }
    }

    // concatenate number|index
    private static List<long> s_prime_indexes;
    public static List<long> PrimeIndexes
    {
        get
        {
            if (s_prime_indexes == null)
            {
                GeneratePrimeIndexes();
            }
            return s_prime_indexes;
        }
    }
    public static int PrimeIndexIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (s_prime_indexes == null)
        {
            GeneratePrimeIndexes();
        }
        return BinarySearch(s_prime_indexes, number);
    }
    private static void GeneratePrimeIndexes()
    {
        s_prime_indexes = new List<long>(MAX_SERIES_LENGTH);
        for (int i = 0; i < MAX_SERIES_LENGTH; i++)
        {
            string text = s_primes[i].ToString() + (i + 1).ToString();
            long number;
            if (long.TryParse(text, out number))
            {
                s_prime_indexes.Add(number);
            }
        }
    }
    /// <summary>
    /// <para>Prime|Index number is concatenation of prime value and prime index.</para>
    /// <para>21, 32, 53, 74, 115, 136, 177, 198, 239, 2910, ...</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsPrimeIndex(long number)
    {
        if (number < 0L) number *= -1L;
        return PrimeIndexes.Contains(number);
    }

    private static List<long> s_additive_prime_indexes;
    public static List<long> AdditivePrimeIndexes
    {
        get
        {
            if (s_additive_prime_indexes == null)
            {
                GenerateAdditivePrimeIndexes();
            }
            return s_additive_prime_indexes;
        }
    }
    public static int AdditivePrimeIndexIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (s_additive_prime_indexes == null)
        {
            GenerateAdditivePrimeIndexes();
        }
        return BinarySearch(s_additive_prime_indexes, number);
    }
    private static void GenerateAdditivePrimeIndexes()
    {
        s_additive_prime_indexes = new List<long>(MAX_SERIES_LENGTH);
        for (int i = 0; i < MAX_SERIES_LENGTH; i++)
        {
            string text = s_additive_primes[i].ToString() + (i + 1).ToString();
            long number;
            if (long.TryParse(text, out number))
            {
                s_additive_prime_indexes.Add(number);
            }
        }
    }
    /// <summary>
    /// <para>AdditivePrime|Index number is concatenation of additive_prime value and additive_prime index.</para>
    /// <para>21, 32, 53, 74, 115, 236, 297, 418, 439, 4710, ...</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsAdditivePrimeIndex(long number)
    {
        if (number < 0L) number *= -1L;
        return AdditivePrimeIndexes.Contains(number);
    }

    private static List<long> s_non_additive_prime_indexes;
    public static List<long> NonAdditivePrimeIndexes
    {
        get
        {
            if (s_non_additive_prime_indexes == null)
            {
                GenerateNonAdditivePrimeIndexes();
            }
            return s_non_additive_prime_indexes;
        }
    }
    public static int NonAdditivePrimeIndexIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (s_non_additive_prime_indexes == null)
        {
            GenerateNonAdditivePrimeIndexes();
        }
        return BinarySearch(s_non_additive_prime_indexes, number);
    }
    private static void GenerateNonAdditivePrimeIndexes()
    {
        s_non_additive_prime_indexes = new List<long>(MAX_SERIES_LENGTH);
        for (int i = 0; i < MAX_SERIES_LENGTH; i++)
        {
            string text = s_non_additive_primes[i].ToString() + (i + 1).ToString();
            long number;
            if (long.TryParse(text, out number))
            {
                s_non_additive_prime_indexes.Add(number);
            }
        }
    }
    /// <summary>
    /// <para>NonAdditivePrime|Index number is concatenation of non_additive_prime value and non_additive_prime index.</para>
    /// <para>131, 172, 193, 314, 375, 536, 597, 718, 739, 7910, ...</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsNonAdditivePrimeIndex(long number)
    {
        if (number < 0L) number *= -1L;
        return NonAdditivePrimeIndexes.Contains(number);
    }

    private static List<long> s_composite_indexes;
    public static List<long> CompositeIndexes
    {
        get
        {
            if (s_composite_indexes == null)
            {
                GenerateCompositeIndexes();
            }
            return s_composite_indexes;
        }
    }
    public static int CompositeIndexIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (s_composite_indexes == null)
        {
            GenerateCompositeIndexes();
        }
        return BinarySearch(s_composite_indexes, number);
    }
    private static void GenerateCompositeIndexes()
    {
        s_composite_indexes = new List<long>(MAX_SERIES_LENGTH);
        for (int i = 0; i < MAX_SERIES_LENGTH; i++)
        {
            string text = s_composites[i].ToString() + (i + 1).ToString();
            long number;
            if (long.TryParse(text, out number))
            {
                s_composite_indexes.Add(number);
            }
        }
    }
    /// <summary>
    /// <para>Composite|Index number is concatenation of composite value and composite index.</para>
    /// <para>41, 62, 83, 94, 105, 126, 147, 158, 169, 1810, ...</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsCompositeIndex(long number)
    {
        if (number < 0L) number *= -1L;
        return CompositeIndexes.Contains(number);
    }

    private static List<long> s_additive_composite_indexes;
    public static List<long> AdditiveCompositeIndexes
    {
        get
        {
            if (s_additive_composite_indexes == null)
            {
                GenerateAdditiveCompositeIndexes();
            }
            return s_additive_composite_indexes;
        }
    }
    public static int AdditiveCompositeIndexIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (s_additive_composite_indexes == null)
        {
            GenerateAdditiveCompositeIndexes();
        }
        return BinarySearch(s_additive_composite_indexes, number);
    }
    private static void GenerateAdditiveCompositeIndexes()
    {
        s_additive_composite_indexes = new List<long>(MAX_SERIES_LENGTH);
        for (int i = 0; i < MAX_SERIES_LENGTH; i++)
        {
            string text = s_additive_composites[i].ToString() + (i + 1).ToString();
            long number;
            if (long.TryParse(text, out number))
            {
                s_additive_composite_indexes.Add(number);
            }
        }
    }
    /// <summary>
    /// <para>AdditiveComposite|Index number is concatenation of additive_composite value and additive_composite index.</para>
    /// <para>41, 62, 83, 94, 155, 186, 227, 248, 269, 2710, ...</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsAdditiveCompositeIndex(long number)
    {
        if (number < 0L) number *= -1L;
        return AdditiveCompositeIndexes.Contains(number);
    }

    private static List<long> s_non_additive_composite_indexes;
    public static List<long> NonAdditiveCompositeIndexes
    {
        get
        {
            if (s_non_additive_composite_indexes == null)
            {
                GenerateNonAdditiveCompositeIndexes();
            }
            return s_non_additive_composite_indexes;
        }
    }
    public static int NonAdditiveCompositeIndexIndexOf(long number)
    {
        if (number < 0L) number *= -1L;

        if (s_non_additive_composite_indexes == null)
        {
            GenerateNonAdditiveCompositeIndexes();
        }
        return BinarySearch(s_non_additive_composite_indexes, number);
    }
    private static void GenerateNonAdditiveCompositeIndexes()
    {
        s_non_additive_composite_indexes = new List<long>(MAX_SERIES_LENGTH);
        for (int i = 0; i < MAX_SERIES_LENGTH; i++)
        {
            string text = s_non_additive_composites[i].ToString() + (i + 1).ToString();
            long number;
            if (long.TryParse(text, out number))
            {
                s_non_additive_composite_indexes.Add(number);
            }
        }
    }
    /// <summary>
    /// <para>NonAdditiveComposite|Index number is concatenation of non_additive_composite value and non_additive_composite index.</para>
    /// <para>101, 122, 143, 164, 205, 216, 257, 308, 329, 3410, ...</para>
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static bool IsNonAdditiveCompositeIndex(long number)
    {
        if (number < 0L) number *= -1L;
        return NonAdditiveCompositeIndexes.Contains(number);
    }


    /// <summary>
    /// Factorize a number into its prime factors.
    /// </summary>
    /// <param name="number">A number to factorize.</param>
    /// <returns>Return all prime factors (including repeated ones).</returns>
    public static List<long> Factorize(long number)
    {
        List<long> result = new List<long>();

        if (number < 0L)
        {
            result.Add(-1L);
            number *= -1L;
        }

        if ((number >= 0L) && (number <= 2L))
        {
            result.Add(number);
        }
        else // if (number > 2L)
        {
            // if number has a prime factor add it to factors,
            // number /= p,
            // reloop until  number == 1L
            while (number != 1L)
            {
                if ((number % 2L) == 0L) // if even number
                {
                    result.Add(2L);
                    number /= 2L;
                }
                else // trial divide by all primes upto sqrt(number)
                {
                    long max = (long)Math.Round(Math.Sqrt(number)) + 1L;	// extra 1 for double calculation errors

                    bool is_factor_found = false;
                    for (long i = 3L; i <= max; i += 2L)
                    {
                        if ((number % i) == 0L)
                        {
                            is_factor_found = true;
                            result.Add(i);
                            number /= i;
                            break; // for loop, reloop while
                        }
                    }

                    // if no prime factor found the number must be prime in the first place
                    if (!is_factor_found)
                    {
                        result.Add(number);
                        break; // while loop
                    }
                }
            }
        }

        s_factors = result;
        return result;
    }
    /// <summary>
    /// Factorize a number into its prime factors.
    /// </summary>
    /// <param name="number">A number to factorize.</param>
    /// <returns>Return all prime factors and their powers.</returns>
    public static Dictionary<long, int> FactorizeByPowers(long number)
    {
        Dictionary<long, int> result = new Dictionary<long, int>();

        int power = 0;
        List<long> factors = Factorize(number);
        result.Add(factors[0], power);
        foreach (long factor in factors)
        {
            if (!result.ContainsKey(factor)) // new factor
            {
                power = 1;
                result.Add(factor, power);
            }
            else // update existing factor
            {
                power++;
                result[factor] = power;
            }
        }

        s_factor_powers = result;
        return result;
    }
    /// <summary>
    /// Get a multiplication string of a number's prime factors.
    /// </summary>
    /// <param name="number">A number to factorize.</param>
    /// <returns></returns>
    public static string FactorizeToString(long number)
    {
        StringBuilder str = new StringBuilder();
        List<long> factors = Factorize(number);
        if (factors != null)
        {
            if (factors.Count > 0)
            {
                foreach (long factor in factors)
                {
                    str.Append(factor.ToString() + "×");
                }
                if (str.Length > 1)
                {
                    str.Remove(str.Length - 1, 1);
                }
            }
        }
        return str.ToString();
    }

    private static List<long> s_factors = null;
    private static Dictionary<long, int> s_factor_powers = null;
    public static List<long> GetDivisors(long number)
    {
        if (number < 0L) number *= -1L;

        //if (s_factor_powers == null) 
        s_factor_powers = FactorizeByPowers(number);
        int factors_count = GetDivisorCount(number);
        List<long> result = new List<long>(factors_count);

        result.Insert(0, 1L);
        if (number <= 1L) return result;

        int count = 1;
        foreach (long key in s_factor_powers.Keys)
        {
            int count_so_far = count;
            long prime = key;
            int exponent = s_factor_powers[key];

            long multiplier = 1L;
            for (int j = 0; j < exponent; ++j)
            {
                multiplier *= prime;
                for (int i = 0; i < count_so_far; ++i)
                {
                    result.Insert(count++, result[i] * multiplier);
                }
            }
        }
        return result;
    }
    public static int GetDivisorCount(long number)
    {
        if (number < 0L) number *= -1L;

        int result = 1;
        //if (s_factor_powers == null) 
        s_factor_powers = FactorizeByPowers(number);
        foreach (long key in s_factor_powers.Keys)
        {
            result *= (s_factor_powers[key] + 1);
        }
        return result;
    }
    public static long SumOfDivisors(long number)
    {
        if (number < 0L) number *= -1L;
        if (number == 0) return 0L;

        long result = 0L;
        List<long> divisors = GetDivisors(number);
        foreach (long divisor in divisors)
        {
            result += divisor;
        }
        return result;
        //if (number < 0L) number *= -1L;
        //if (number == 0) return 0L;

        //long result = 1L;
        //s_factor_powers = FactorizeByPowers(number);
        //foreach (long key in s_factor_powers.Keys)
        //{
        //    long sum = 0;
        //    for (int i = 0; i <= s_factor_powers[key]; i++)
        //    {
        //        sum += (long)Math.Pow(key, i);
        //    }
        //    result *= sum;
        //}
        //return result;
    }
    public static long SumOfDivisorDigitSums(long number)
    {
        if (number < 0L) number *= -1L;
        if (number == 0) return 0L;

        long result = 0L;
        List<long> divisors = GetDivisors(number);
        foreach (long divisor in divisors)
        {
            result += DigitSum(divisor);
        }
        return result;
    }
    public static long SumOfDivisorDigitalRoots(long number)
    {
        if (number < 0L) number *= -1L;
        if (number == 0) return 0L;

        long result = 0L;
        List<long> divisors = GetDivisors(number);
        foreach (long divisor in divisors)
        {
            result += DigitalRoot(divisor);
        }
        return result;
    }
    public static string GetDivisorsString(long number)
    {
        if (number < 0L) number *= -1L;
        if (number > 1000000000000L) return "";

        StringBuilder str = new StringBuilder();
        List<long> divisors = GetDivisors(number);
        foreach (long divisor in divisors)
        {
            str.Append(divisor.ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static string GetDivisorDigitSumsString(long number)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        List<long> divisors = GetDivisors(number);
        foreach (long divisor in divisors)
        {
            str.Append(DigitSum(divisor).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }
    public static string GetDivisorDigitalRootsString(long number)
    {
        if (number < 0L) number *= -1L;

        StringBuilder str = new StringBuilder();
        List<long> divisors = GetDivisors(number);
        foreach (long divisor in divisors)
        {
            str.Append(DigitalRoot(divisor).ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }

    /// <summary>
    /// Proper divisors are all divisors except the number itself.
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static List<long> GetProperDivisors(long number)
    {
        if (number < 0L) number *= -1L;

        List<long> result = GetDivisors(number);
        result.RemoveAt(result.Count - 1);
        return result;
    }
    public static int GetProperDivisorCount(long number)
    {
        if (number < 0L) number *= -1L;
        return GetDivisorCount(number) - 1;
    }
    public static long SumOfProperDivisors(long number)
    {
        if (number < 0L) number *= -1L;
        return SumOfDivisors(number) - number;
    }
    public static string GetProperDivisorsString(long number)
    {
        if (number < 0L) number *= -1L;
        if (number > 1000000000000L) return "";

        StringBuilder str = new StringBuilder();
        List<long> divisors = GetProperDivisors(number);
        foreach (long divisor in divisors)
        {
            str.Append(divisor.ToString() + "+");
        }
        if (str.Length > 0)
        {
            str.Remove(str.Length - 1, 1);
        }
        return str.ToString();
    }

    public static int BinarySearch(IList<long> list, long number)
    {
        if (list == null) return -1;
        if (list.Count < 1) return -1;

        int min = 0;
        int max = list.Count - 1;
        int old_mid = -1;
        int mid;
        while ((mid = (min + max) / 2) != old_mid)
        {
            if (number == list[min]) { return min; }

            if (number == list[max]) { return max; }

            if (number == list[mid]) { return mid; }
            else if (number < list[mid]) { max = mid; }
            else /*if (number > list[mid])*/ { min = mid; }

            old_mid = mid;
        }

        return -1;
    }
    public static void QuickSort(IList<long> list, int min, int max)
    {
        if (list == null) return;
        if (list.Count < 1) return;
        if (min > max) return;
        if ((min < 0) || (max >= list.Count)) return;

        int lo = min;
        int hi = max;
        long mid = list[(lo + hi) / 2];	// uses copy constructor

        do
        {
            while (list[lo] < mid)		// uses comparison operator
                lo++;
            while (mid < list[hi])
                hi--;

            if (lo <= hi)
            {
                long temp = list[hi];
                list[hi] = list[lo];
                list[hi] = temp;
                lo++;
                hi--;
            }
        }
        while (lo <= hi);

        if (hi > min)
            QuickSort(list, min, hi);
        if (lo < max)
            QuickSort(list, lo, max);
    }

    // Permutations التباديل: P(n, k) = n! ⁄ (n−k)!
    //
    //          n!       
    // nPk = ---------
    //        (n-k)!
    //
    //       1 2 3 4 5 6 7 8 9
    // 9P4 = -----------------
    //       1 2 3 4 5    
    //
    //                         
    // 9P4 =           6 7 8 9 = multiply last k numbers
    //
    //
    public static BigInteger nPk(int n, int k)
    {
        BigInteger result = 0;
        if ((n > 0) && (k > 0))
        {
            if (k <= n)
            {
                // multiply last k numbers
                BigInteger numerator = 1;
                int r = n - k + 1;
                for (int i = r; i <= n; i++)
                {
                    numerator *= i;
                }

                result = numerator;
            }
            else // k > n
            {
                result = 0;
            }
        }
        return result;
    }
    // Combinations التوافيق: C(n, k) = n! ⁄ (k! (n−k)!)
    //
    //          n!
    // nCk = ---------
    //       k! (n-k)!
    //
    //       1 2 3 4 5 6 7 8 9
    // 9C4 = ---------------------
    //       1 2 3 4  *  1 2 3 4 5     
    //
    //                 6 7 8 9       multiply last k numbers
    // 9C4 = --------------------- = ------------------------
    //       1 2 3 4                 multiply first k numbers
    //
    //
    public static BigInteger nCk(int n, int k)
    {
        BigInteger result = 0;
        if ((n > 0) && (k > 0))
        {
            if (k <= n)
            {
                // multiply last k numbers
                BigInteger numerator = 1;
                int r = n - k + 1;
                for (int i = r; i <= n; i++)
                {
                    numerator *= i;
                }

                // multiply first k numbers
                BigInteger denominator = 1;
                for (int i = 1; i <= k; i++)
                {
                    denominator *= i;
                }

                result = numerator / denominator;
            }
            else // k > n
            {
                result = 0;
            }
        }
        return result;
    }
}
