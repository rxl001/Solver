using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanAllocation
{
    public static class LoanExtensions
    {
        public static double WavgInterestRate(this List<Loan> loans)
        {
            var amountTimesInterest = loans.Sum(p => p.Available * p.InterestRate);
            var amount = loans.Sum(p => p.Available);
            return Math.Abs(amount) < 0.001 ? 0 : amountTimesInterest / amount;
        }

        public static double WavgAllocationInterestRate(this List<Loan> loans)
        {
            var amountTimesAllocation = loans.Sum(p => p.Allocation * p.InterestRate);
            var amount = loans.Sum(p => p.Allocation);
            return Math.Abs(amount) < 0.001 ? 0 : amountTimesAllocation / amount;
        }

        public static double WavgDefaultFrequency(this List<Loan> loans)
        {
            var amountTimesDefault = loans.Sum(p => p.Available * p.DefaultFrequency);
            var amount = loans.Sum(p => p.Available);
            return Math.Abs(amount) < 0.001 ? 0 : amountTimesDefault / amount;
        }

        public static double WavgAllocationDefaultFrequency(this List<Loan> loans)
        {
            var amountTimesDefault = loans.Sum(p => p.Allocation * p.DefaultFrequency);
            var amount = loans.Sum(p => p.Allocation);
            return Math.Abs(amount) < 0.001 ? 0 : amountTimesDefault / amount;
        }
    }
}
