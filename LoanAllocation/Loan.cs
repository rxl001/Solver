using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoanAllocation
{
    public class Loan
    {
        public string LoanId { get; set; }
        public double LoanAmount { get; set; }
        public double InterestRate { get; set; }
        public double Funded { get; set; }
        public int CreditScore { get; set; }
        public int CreditEnquires { get; set; }
        public double Allocation { get; set; }
        public double DefaultFrequency { get; set; }

        public double Available
        {
            get
            {
                return LoanAmount - Funded;
            }
        }

        public bool Ignored
        {
            get { return Available < 50 || _ignored; }
        }

        public bool Locked
        {
            get { return _locked; }
        }

        public void Lock()
        {
            _locked = true;
        }

        public void Ignore()
        {
            _ignored = true;
        }

        private bool _locked;

        private bool _ignored;

        public Loan()
        {
            LoanId = null;
            LoanAmount = 0;
            InterestRate = 0;
            Funded = 0;
            CreditEnquires = 0;
            CreditScore = 0;
            Allocation = 0;
            DefaultFrequency = 0;
            _locked = false;
            _ignored = false;
        }

        public Loan(string id, double amount, double funded, double interestRate, int creditScore, int creditEnquires) : this()
        {
            LoanId = id;
            LoanAmount = amount;
            Funded = funded;
            InterestRate = interestRate;
            CreditScore = creditScore;
            CreditEnquires = creditEnquires;
            if (creditScore > 900)
                DefaultFrequency = 0.01;
            else if (creditScore > 800)
                DefaultFrequency = 0.015;
            else if (creditScore > 700)
                DefaultFrequency = 0.03;
            else if (creditScore > 600)
                DefaultFrequency = 0.06;
            else if (creditScore > 500)
                DefaultFrequency = 0.08;
            else
                DefaultFrequency = 0.10;
        }

        public void Reset()
        {
            Allocation = 0;
            _locked = false;
            _ignored = false;
        }
    }
}
