using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanAllocation
{
    public class Market
    {
        public enum InvestmentStrategy
        {
            AGGRESSIVE,
            BALANCED,
            CONSERVATIVE,
        }

        private static readonly Random random = new Random();
        public static int RandomNumber(int min, int max)
        {
            if (min > max)
            {
                var m = max;
                max = min;
                min = m;
            }
            return random.Next(min, max);
        }

        public List<Loan> LoansInMarket { get; set; }

        public double MarketSize { get; set; }

        public double WavgDefaultFrequency { get; set; }

        public double WavgInterestRate { get; set; }

        public double WavgAllocationInterestRate
        {
            get
            {
                return LoansInMarket.WavgAllocationInterestRate();
            }
        }

        public double WavgAllocationDefaultFrequency
        {
            get
            {
                return LoansInMarket.WavgAllocationDefaultFrequency();
            }
        }

        public void OptimiseAllocation(double targetInterestRate)
        {
            while (Math.Abs(targetInterestRate - WavgAllocationInterestRate) > 0.005)
            {
                if (targetInterestRate > WavgAllocationInterestRate)
                {
                    var adjustment = (double)Math.Round((targetInterestRate - WavgAllocationInterestRate) * 500, 0);

                    var loanWithLeastInterest =
                        LoansInMarket.Where(
                            p =>
                                !p.Ignored && !p.Locked && p.Allocation - adjustment > 50 &&
                                p.InterestRate < targetInterestRate).OrderBy(p => p.InterestRate).FirstOrDefault();
                    if (loanWithLeastInterest == null || loanWithLeastInterest.InterestRate > targetInterestRate)
                        return; // can't do anything
                    loanWithLeastInterest.Allocation -= adjustment;
                    loanWithLeastInterest.Lock();

                    var unlockedMarketSize =
                        LoansInMarket.Where(p => !p.Ignored && !p.Locked && p.LoanId != loanWithLeastInterest.LoanId)
                            .Sum(p => p.Available);
                    foreach (var m in LoansInMarket.Where(p => !p.Ignored && !p.Locked))
                    {
                        if (m.LoanId == loanWithLeastInterest.LoanId) continue;
                        m.Allocation += adjustment * m.Available / unlockedMarketSize;
                    }
                }
                else
                {
                    var adjustment = (double)Math.Round((WavgAllocationInterestRate - targetInterestRate) * 500, 0);

                    var loanWithLeastInterest =
                        LoansInMarket.Where(
                            p =>
                                !p.Ignored && !p.Locked && p.Allocation - adjustment > 50 &&
                                p.InterestRate < targetInterestRate).OrderByDescending(p => p.InterestRate).FirstOrDefault();
                    if (loanWithLeastInterest == null || loanWithLeastInterest.InterestRate < targetInterestRate)
                        return; // can't do anything
                    loanWithLeastInterest.Allocation -= adjustment;
                    loanWithLeastInterest.Lock();

                    var unlockedMarketSize =
                        LoansInMarket.Where(p => !p.Ignored && !p.Locked && p.LoanId != loanWithLeastInterest.LoanId)
                            .Sum(p => p.Available);
                    foreach (var m in LoansInMarket.Where(p => !p.Ignored && !p.Locked))
                    {
                        if (m.LoanId == loanWithLeastInterest.LoanId) continue;
                        m.Allocation += adjustment * m.Available / unlockedMarketSize;
                    }
                }
            }
        }

        public Market()
        {
            LoansInMarket = new List<Loan>();
            WavgInterestRate = 0;
            WavgDefaultFrequency = 0;
        }

        public Market(List<Loan> loans) : this()
        {
            LoansInMarket = loans.Where(p => !p.Ignored).ToList();
            var amountTimesInterest = loans.Sum(p => p.Available * p.InterestRate);
            MarketSize = loans.Sum(p => p.Available);
            WavgInterestRate = Math.Abs(MarketSize) < 0.001 ? 0 : amountTimesInterest / MarketSize;
            var dfTimesAmount = loans.Sum(p => p.Available * p.DefaultFrequency);
            WavgDefaultFrequency = Math.Abs(MarketSize) < 0.001 ? 0 : dfTimesAmount / MarketSize;
        }

        public void Reset()
        {
            foreach (var l in LoansInMarket)
                l.Reset();
        }

        private double Allocate(List<Loan> market, double amount, double maxAmountPerLoan)
        {
            var classAsize = market.Sum(p => p.Available);
            var classArates = market.Sum(p => p.InterestRate * p.Available) / classAsize;

            // initial allocation
            foreach (var l in market)
            {
                l.Allocation = (l.Available / classAsize) * amount;

                if (Math.Abs(l.Allocation - 50) < 0.001 || Math.Abs(l.Allocation - maxAmountPerLoan) < 0.001)
                    l.Lock();

                if (l.Allocation > l.Available)
                {
                    l.Allocation = l.Available;
                    l.Lock();
                }
            }

            foreach (var l in market.Where(p => !p.Locked))
            {
                if (l.Allocation < 50 || l.Allocation > maxAmountPerLoan)
                {
                    var target = l.Allocation < 50 ? 50 : maxAmountPerLoan;
                    var borrowed = l.Allocation - target;
                    l.Allocation = target;
                    l.Lock();

                    var availableMarket = market.Where(p => !p.Locked && p.LoanId != l.LoanId && p.Allocation < maxAmountPerLoan).ToList();
                    var unlockedMarketSize = market.Sum(p => p.Available);
                    foreach (var m in availableMarket)
                    {
                        if (m.Ignored) continue;
                        if (m.LoanId == l.LoanId || m.Locked) continue;
                        m.Allocation += borrowed * m.Available / unlockedMarketSize;

                        if (m.Allocation > maxAmountPerLoan)
                        {
                            m.Allocation = maxAmountPerLoan;
                            m.Lock();
                        }

                        if (m.Allocation > m.Available)
                        {
                            m.Allocation = m.Available;
                            m.Lock();
                        }
                    }
                }
            }

            var allocation = market.Sum(p => p.Allocation);
            var roundingError = amount - allocation;

            var loan =
                    market.Where(p => !p.Ignored && !p.Locked).OrderBy(p => p.InterestRate).FirstOrDefault();

            if (loan == null)
                return roundingError;
            else
            {
                loan.Allocation += roundingError;
                return 0;
            }
        }

        private double Allocate(List<Loan> market, double amount, int scoreLow, int scoreHigh, double targetInterestRate, double maxAmountPerLoan)
        {
            double remainder;
            var classA = market.Where(p => p.CreditScore >= scoreLow && p.CreditScore < scoreHigh);
            var classAsize = classA.Sum(p => p.Available);
            var classArates = classA.Sum(p => p.InterestRate * p.Available) / classA.Sum(p => p.Available);
            var other = market.Where(p => p.CreditScore <= scoreLow);
            var otherRates = other.Sum(p => p.InterestRate * p.Available) / other.Sum(p => p.Available);

            var classAllocation = amount * (otherRates - targetInterestRate) / (otherRates - classArates);
            remainder = amount - classAllocation;

            Allocate(classA.ToList(), classAllocation, maxAmountPerLoan);

            var allocation = classA.Sum(p => p.Allocation);
            var allocRate = classA.Sum(p => p.Allocation * p.InterestRate) / allocation;
            remainder = amount - allocation;

            return remainder;
        }

        public double WavgTargetInterestRate(InvestmentStrategy strategy)
        {
            var targetInterestRate = strategy == InvestmentStrategy.BALANCED ? WavgInterestRate :
                strategy == InvestmentStrategy.CONSERVATIVE ? WavgInterestRate - 3.5 : WavgInterestRate + 3.5;
            return targetInterestRate;
        }

        private double WeightFactor(double targetInterest, double thisInterest)
        {
            var result = 1.0 / Math.Exp(Math.Abs(targetInterest - thisInterest)) * 5.0;
            return result;
        }

        public List<Loan> Invest(double amount, InvestmentStrategy strategy, double maxAmountPerLoan = -1)
        {
            Reset();

            var targetInterestRate = WavgTargetInterestRate(strategy);

            var result = new List<Loan>();
            if (LoansInMarket == null || LoansInMarket.Count == 0)
                return result;

            if (maxAmountPerLoan < 0)
                maxAmountPerLoan = Math.Max(amount / 100, 100);

            var min = Math.Max((int)(amount / maxAmountPerLoan), 2);
            var max = (int)Math.Min(amount / 50.0, 250);
            if (min > max)
            {
                maxAmountPerLoan = 100;
                min = (int)(amount / 100.0);
            }

            var expectedLoans = RandomNumber(min, max);
            if (expectedLoans > LoansInMarket.Count)
                expectedLoans = LoansInMarket.Count;
            if (LoansInMarket.Count < 25)
                expectedLoans = LoansInMarket.Count;

            if (strategy == InvestmentStrategy.CONSERVATIVE)
            {
                var maxMarketInterestRate = LoansInMarket.Max(p => p.InterestRate);
                var minMarketInterestRate = LoansInMarket.Min(p => p.InterestRate);

                var maxInterestRate = WavgInterestRate + (maxMarketInterestRate - WavgInterestRate) / 2.0;
                var selectionBucket = 0.0;
                selectionBucket = LoansInMarket.Where(p => p.InterestRate < maxInterestRate).Sum(p => p.Available > maxAmountPerLoan ? maxAmountPerLoan : p.Available);
                while (selectionBucket < amount && maxInterestRate < minMarketInterestRate)
                {
                    maxInterestRate += 0.5;
                    selectionBucket = LoansInMarket.Where(p => p.InterestRate < maxInterestRate).Sum(p => p.Available > maxAmountPerLoan ? maxAmountPerLoan : p.Available);
                }

                //var minInterestRate = LoansInMarket.Min(p => p.InterestRate);
                //var adjustment = LoansInMarket.Count < 30 ? 6.0 : LoansInMarket.Count < 60 ? 3.0 : 1.0;
                //var maxInterestRate = targetInterestRate * 2.0 - minInterestRate + adjustment;

                foreach (var l in LoansInMarket)
                {
                    if (l.InterestRate > maxInterestRate)
                        l.Ignore();
                }

                while (expectedLoans < LoansInMarket.Count(p => !p.Ignored))
                {
                    var idx = RandomNumber(0, LoansInMarket.Count);
                    LoansInMarket[idx].Ignore();
                }

                var loanSample = LoansInMarket.Where(p => !p.Ignored).ToList();
                var wavg = loanSample.WavgInterestRate();
            }
            else if (strategy == InvestmentStrategy.AGGRESSIVE)
            {
                var maxMarketInterestRate = LoansInMarket.Max(p => p.InterestRate);
                var minMarketInterestRate = LoansInMarket.Min(p => p.InterestRate);

                var minInterestRate = WavgInterestRate - (WavgInterestRate - minMarketInterestRate) / 2.0;
                var selectionBucket = 0.0;
                selectionBucket = LoansInMarket.Where(p => p.InterestRate > minInterestRate).Sum(p => p.Available > maxAmountPerLoan ? maxAmountPerLoan : p.Available);
                while (selectionBucket < amount && minInterestRate > minMarketInterestRate)
                {
                    minInterestRate -= 0.5;
                    selectionBucket = LoansInMarket.Where(p => p.InterestRate > minInterestRate).Sum(p => p.Available > maxAmountPerLoan ? maxAmountPerLoan : p.Available);
                }
                                
                foreach (var l in LoansInMarket)
                {
                    if (l.InterestRate < minInterestRate)
                        l.Ignore();
                }

                while (expectedLoans < LoansInMarket.Count(p => !p.Ignored))
                {
                    var idx = RandomNumber(0, LoansInMarket.Count);
                    LoansInMarket[idx].Ignore();
                }
            }
            else if (strategy == InvestmentStrategy.BALANCED)
            {
                while (expectedLoans < LoansInMarket.Count(p => !p.Ignored))
                {
                    var idx = RandomNumber(0, LoansInMarket.Count);
                    LoansInMarket[idx].Ignore();
                }
            }

            var market = LoansInMarket.Where(p => !p.Ignored).ToList();
            var marketSize = market.Sum(p => p.Available);

            // initial allocation
            foreach (var l in market)
            {
                var weightFactor =
                    strategy == InvestmentStrategy.BALANCED
                        ? (RandomNumber(5, 15) / 10.0)
                        : WeightFactor(targetInterestRate, l.InterestRate) * (RandomNumber(10, 20) / 10.0);
                l.Allocation = (l.Available / marketSize) * amount * weightFactor;
                if (l.Allocation > maxAmountPerLoan)
                {
                    l.Allocation = maxAmountPerLoan;
                    l.Lock();
                }
                if (l.Allocation < 50)
                {
                    l.Allocation = 50;
                    l.Lock();
                }
                //l.Allocation = (l.Available / marketSize) * amount;
                if (Math.Abs(l.Allocation - 50) < 0.001 || Math.Abs(l.Allocation - maxAmountPerLoan) < 0.001)
                    l.Lock();
                if (l.Allocation > l.Available)
                {
                    l.Allocation = l.Available;
                    l.Lock();
                }
            }

            var allocationX = market.Sum(p => p.Allocation);
            var roundingErrorX = amount - allocationX;
            var noSolution = false;

            while (Math.Abs(roundingErrorX) > 0.01 && !noSolution)
            {
                foreach (var l in market)
                {
                    l.Allocation += (l.Available / marketSize) * roundingErrorX;
                    if (l.Allocation > maxAmountPerLoan)
                        l.Allocation = maxAmountPerLoan;
                    if (l.Allocation < 50)
                        l.Allocation = 50;
                    if (Math.Abs(l.Allocation - 50) < 0.001 || Math.Abs(l.Allocation - maxAmountPerLoan) < 0.001)
                        l.Lock();
                    if (l.Allocation > l.Available)
                    {
                        l.Allocation = l.Available;
                        l.Lock();
                    }
                }
                if (market.Any(p => !p.Locked))
                {
                    allocationX = market.Sum(p => p.Allocation);
                    roundingErrorX = amount - allocationX;
                }
                else
                    noSolution = true;
            }

            foreach (var l in market.Where(p => !p.Locked))
            {
                if (l.Allocation < 50 || l.Allocation > Math.Min(maxAmountPerLoan, l.Available))
                {
                    var target = l.Allocation < 50 ? 50 : Math.Min(maxAmountPerLoan, l.Available);
                    var borrowed = l.Allocation - target;
                    l.Allocation = target;
                    l.Lock();

                    var availableMarket = market.Where(p => !p.Locked && p.LoanId != l.LoanId && p.Allocation < Math.Min(maxAmountPerLoan, l.Available)).ToList();
                    var unlockedMarketSize = availableMarket.Sum(p => p.Available);
                    foreach (var m in availableMarket)
                    {
                        if (m.Ignored) continue;
                        if (m.LoanId == l.LoanId || m.Locked) continue;
                        m.Allocation += borrowed * m.Available / unlockedMarketSize;
                        if (m.Allocation > Math.Min(maxAmountPerLoan, l.Available))
                        {
                            m.Allocation = Math.Min(maxAmountPerLoan, l.Available);
                            m.Lock();
                        }
                        if (m.Allocation > m.Available)
                        {
                            m.Allocation = m.Available;
                            m.Lock();
                        }
                    }
                }

                var allocation = market.Sum(p => p.Allocation);
                var roundingError = amount - allocation;

                if ((WavgAllocationInterestRate > targetInterestRate && roundingError > 0) ||
                    (WavgAllocationInterestRate < targetInterestRate && roundingError < 0))
                {
                    var loan =
                        market.Where(p => !p.Ignored && !p.Locked).OrderBy(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation += roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }

                    allocation = market.Sum(p => p.Allocation);
                    roundingError = amount - allocation;

                    loan =
                        market.Where(p => !p.Ignored && !p.Locked).OrderByDescending(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation -= roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }
                }
                else if ((WavgAllocationInterestRate < targetInterestRate && roundingError > 0) ||
                    (WavgAllocationInterestRate > targetInterestRate && roundingError < 0))
                {
                    var loan =
                        market.Where(p => !p.Ignored && !p.Locked).OrderByDescending(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation += roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }

                    allocation = market.Sum(p => p.Allocation);
                    roundingError = amount - allocation;

                    loan =
                        market.Where(p => !p.Ignored && !p.Locked).OrderBy(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation -= roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }
                }
            }

            OptimiseAllocation(targetInterestRate);

            foreach (var l in LoansInMarket.Where(p => !p.Ignored))
            {
                var allocated = new Loan
                {
                    LoanId = l.LoanId,
                    LoanAmount = l.LoanAmount,
                    Funded = l.Funded,
                    InterestRate = l.InterestRate,
                    DefaultFrequency = l.DefaultFrequency,
                    CreditScore = l.CreditScore,
                    CreditEnquires = l.CreditEnquires,
                    Allocation = Math.Round(l.Allocation * 20, 0) / 20 // round to nearest 5 cents
                };

                result.Add(allocated);
            }

            {
                var allocation = result.Sum(p => p.Allocation);
                var roundingError = amount - allocation;

                if ((result.WavgAllocationInterestRate() > targetInterestRate && roundingError > 0) ||
                    (result.WavgAllocationInterestRate() < targetInterestRate && roundingError < 0))
                {
                    var loan =
                        result.Where(p => !p.Ignored && !p.Locked).OrderByDescending(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation += roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }

                    allocation = result.Sum(p => p.Allocation);
                    roundingError = amount - allocation;

                    loan =
                        market.Where(p => !p.Ignored && !p.Locked).OrderBy(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation -= roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }
                }
                else if ((result.WavgAllocationInterestRate() < targetInterestRate && roundingError > 0) ||
                    (result.WavgAllocationInterestRate() > targetInterestRate && roundingError < 0))
                {
                    var loan =
                        result.Where(p => !p.Ignored && !p.Locked).OrderByDescending(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation += roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }

                    allocation = result.Sum(p => p.Allocation);
                    roundingError = amount - allocation;

                    loan =
                        market.Where(p => !p.Ignored && !p.Locked).OrderBy(p => p.InterestRate).FirstOrDefault();
                    if (loan != null)
                    {
                        loan.Allocation -= roundingError;
                        if (loan.Allocation > maxAmountPerLoan)
                            loan.Allocation = maxAmountPerLoan;
                        if (loan.Allocation < 50)
                            loan.Allocation = 50;
                        if (loan.Allocation > loan.Available)
                        {
                            loan.Allocation = loan.Available;
                            loan.Lock();
                        }
                    }
                }
            }

            allocationX = result.Sum(p => p.Allocation);
            roundingErrorX = amount - allocationX;

            while (Math.Abs(roundingErrorX) > 0.01 && !noSolution)
            {
                foreach (var l in result)
                {
                    l.Allocation += (l.Available / marketSize) * roundingErrorX;
                    if (l.Allocation > maxAmountPerLoan)
                        l.Allocation = maxAmountPerLoan;
                    if (l.Allocation < 50)
                        l.Allocation = 50;
                    if (Math.Abs(l.Allocation - 50) < 0.001 || Math.Abs(l.Allocation - maxAmountPerLoan) < 0.001)
                        l.Lock();
                    if (l.Allocation > l.Available)
                    {
                        l.Allocation = l.Available;
                        l.Lock();
                    }
                }
                if (market.Any(p => !p.Locked))
                {
                    allocationX = result.Sum(p => p.Allocation);
                    roundingErrorX = amount - allocationX;
                }
                else
                    noSolution = true;
            }

            return result;
        }
    }
}
