using System;
using System.Collections.Generic;

namespace Model
{
    //private static CultureInfo arabic = new CultureInfo("ar-SA");
    //// Get the standard StringComparers.
    //private static StringComparer invCmp = StringComparer.InvariantCulture;
    //private static StringComparer invICCmp = StringComparer.InvariantCultureIgnoreCase;
    //private static StringComparer currCmp = StringComparer.CurrentCulture;
    //private static StringComparer currICCmp = StringComparer.CurrentCultureIgnoreCase;
    //private static StringComparer ordCmp = StringComparer.Ordinal;
    //private static StringComparer ordICCmp = StringComparer.OrdinalIgnoreCase;
    //// Create a StringComparer that uses the Turkish culture and ignores case.
    //private static StringComparer arabicICComp = StringComparer.Create(arabic, true);

    public enum StatisticCompareBy { Order, Letter, Frequency, PositionSum, DistanceSum, ReversePositionSum, ReverseDistanceSum }
    public enum StatisticCompareOrder { Ascending, Descending }

    public class LetterStatistic : IComparable<LetterStatistic>
    {
        public int Order;
        public char Letter;
        public int Frequency;
        public List<long> Positions = new List<long>();
        public long PositionSum;
        public List<long> Distances = new List<long>();
        public long DistanceSum;
        public List<long> ReversePositions = new List<long>();
        public long ReversePositionSum;
        public List<long> ReverseDistances = new List<long>();
        public long ReverseDistanceSum;

        public static StatisticCompareBy CompareBy;
        public static StatisticCompareOrder CompareOrder;

        public int CompareTo(LetterStatistic obj)
        {
            if (CompareOrder == StatisticCompareOrder.Ascending)
            {
                switch (CompareBy)
                {
                    case StatisticCompareBy.Order:
                        {
                            return this.Order.CompareTo(obj.Order);
                        }
                    case StatisticCompareBy.Letter:
                        {
                            return this.Letter.CompareTo(obj.Letter);
                        }
                    case StatisticCompareBy.Frequency:
                        {
                            if (this.Frequency.CompareTo(obj.Frequency) == 0)
                            {
                                return this.Order.CompareTo(obj.Order);
                            }
                            return this.Frequency.CompareTo(obj.Frequency);
                        }
                    case StatisticCompareBy.PositionSum:
                        {
                            if (this.PositionSum.CompareTo(obj.PositionSum) == 0)
                            {
                                return this.Order.CompareTo(obj.Order);
                            }
                            return this.PositionSum.CompareTo(obj.PositionSum);
                        }
                    case StatisticCompareBy.DistanceSum:
                        {
                            if (this.DistanceSum.CompareTo(obj.DistanceSum) == 0)
                            {
                                return this.Order.CompareTo(obj.Order);
                            }
                            return this.DistanceSum.CompareTo(obj.DistanceSum);
                        }
                    case StatisticCompareBy.ReversePositionSum:
                        {
                            if (this.ReversePositionSum.CompareTo(obj.ReversePositionSum) == 0)
                            {
                                return this.Order.CompareTo(obj.Order);
                            }
                            return this.ReversePositionSum.CompareTo(obj.ReversePositionSum);
                        }
                    case StatisticCompareBy.ReverseDistanceSum:
                        {
                            if (this.ReverseDistanceSum.CompareTo(obj.ReverseDistanceSum) == 0)
                            {
                                return this.Order.CompareTo(obj.Order);
                            }
                            return this.ReverseDistanceSum.CompareTo(obj.ReverseDistanceSum);
                        }
                    default:
                        return this.Order.CompareTo(obj.Order);
                }
            }
            else
            {
                switch (CompareBy)
                {
                    case StatisticCompareBy.Order:
                        {
                            return obj.Order.CompareTo(this.Order);
                        }
                    case StatisticCompareBy.Letter:
                        {
                            return obj.Letter.CompareTo(this.Letter);
                        }
                    case StatisticCompareBy.Frequency:
                        {
                            if (obj.Frequency.CompareTo(this.Frequency) == 0)
                            {
                                return obj.Order.CompareTo(this.Order);
                            }
                            return obj.Frequency.CompareTo(this.Frequency);
                        }
                    case StatisticCompareBy.PositionSum:
                        {
                            if (obj.PositionSum.CompareTo(this.PositionSum) == 0)
                            {
                                return obj.Order.CompareTo(this.Order);
                            }
                            return obj.PositionSum.CompareTo(this.PositionSum);
                        }
                    case StatisticCompareBy.DistanceSum:
                        {
                            if (obj.DistanceSum.CompareTo(this.DistanceSum) == 0)
                            {
                                return obj.Order.CompareTo(this.Order);
                            }
                            return obj.DistanceSum.CompareTo(this.DistanceSum);
                        }
                    case StatisticCompareBy.ReversePositionSum:
                        {
                            if (obj.ReversePositionSum.CompareTo(this.ReversePositionSum) == 0)
                            {
                                return obj.Order.CompareTo(this.Order);
                            }
                            return obj.ReversePositionSum.CompareTo(this.ReversePositionSum);
                        }
                    case StatisticCompareBy.ReverseDistanceSum:
                        {
                            if (obj.ReverseDistanceSum.CompareTo(this.ReverseDistanceSum) == 0)
                            {
                                return obj.Order.CompareTo(this.Order);
                            }
                            return obj.ReverseDistanceSum.CompareTo(this.ReverseDistanceSum);
                        }
                    default:
                        return obj.Order.CompareTo(this.Order);
                }
            }
        }
    }
}
