using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LoanAllocation;

namespace LoanSolver.Controllers
{
    public class SolverController : ApiController
    {
        // POST api/values
        public HttpResponseMessage Post([FromBody]dynamic data)
        {
            string value = data.ToString();
            var input = JsonValue.Parse(value);
            var loans = input.ContainsKey("Loans") ? input["Loans"] as JsonArray : new JsonArray();

            var loansInMarket = new List<Loan>();
            var allocation = new List<Loan>();

            foreach (var l in loans)
            {
                var loan = new Loan
                {
                    LoanId = (string)l["LoanId"],
                    LoanAmount = (double)l["LoanAmount"],
                    InterestRate = (double)l["InterestRate"],
                    Funded = (double)l["Funded"],
                    CreditScore = (int)l["CreditScore"],
                    CreditEnquires = (int)l["CreditEnquires"]
                };
                loansInMarket.Add(loan);
            }

            var market = new Market(loansInMarket);
            var token = input.ContainsKey("Token") ? (string)input["Token"] : "";

            var jsonResult = "{";
            
            if (input.ContainsKey("Investment") && token == @"91713974-CDB8-43E8-AD17-D7FE53ED3B53")
            {
                var inv = input["Investment"];
                var amount = inv.ContainsKey("Amount") ? (double)inv["Amount"] : 0;
                var strategy = inv.ContainsKey("Strategy") ? (string)inv["Strategy"] : "BALANCED";
                var max = inv.ContainsKey("MaxAmountPerLoan") ? (double)inv["MaxAmountPerLoan"] : -1;
                var strategyId = strategy == "CONSERVATIVE" ? Market.InvestmentStrategy.CONSERVATIVE :
                    strategy == "AGGRESSIVE" ? Market.InvestmentStrategy.AGGRESSIVE :
                    Market.InvestmentStrategy.BALANCED;

                if (amount > 0)
                {
                    allocation = market.Invest(amount, strategyId, max);
                }

                if (allocation != null && allocation.Count > 0)
                {
                    jsonResult += "\"MarketRate\":" + market.WavgInterestRate.ToString();
                    jsonResult += ",\"TargetRate\":" + market.WavgTargetInterestRate(strategyId).ToString();
                    jsonResult += ",\"AllocationCount\":" + allocation.Count().ToString();
                    jsonResult += ",\"AllocationAmount\":" + allocation.Sum(p=>p.Allocation).ToString();
                    jsonResult += ",\"AllocationRate\":" + allocation.WavgAllocationInterestRate().ToString();
                    jsonResult += ",\"Allocation\": [";

                    var allocList = new List<string>();
                    foreach (var a in allocation)
                    {
                        var frag = "{\"LoanId\":\"" + a.LoanId + "\",\"Funded\":\"" + a.Funded.ToString("N2") + "\",\"Allocation\":\"" + a.Allocation.ToString("N2") + "\"}";
                        allocList.Add(frag);
                    }
                    jsonResult += string.Join(",", allocList);
                    jsonResult += "]";
                }
            }

            jsonResult += "}";
                        
            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(jsonResult, System.Text.Encoding.UTF8, "application/json");
            return response;
        }
    }
}