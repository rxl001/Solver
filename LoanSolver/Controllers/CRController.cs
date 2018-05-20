using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Numeric;

namespace LoanSolver.Controllers
{
    public class CRController : ApiController
    {
        // POST api/values
        public HttpResponseMessage Post([FromBody]dynamic data)
        {
            string value = data.ToString();
            var input = JsonValue.Parse(value);
            var amount = input.ContainsKey("amount") ? (double)input["amount"] : 0;
            var fee = input.ContainsKey("feePct") ? (double)input["feePct"] : 0;
            var feeAmt = input.ContainsKey("feeAmt") ? (double)input["feeAmt"] : 0;
            var serviceFee = input.ContainsKey("serviceFee") ? (double)input["serviceFee"] : 0;
            var indicativeRate = input.ContainsKey("rate") ? (double)input["rate"] : 0;
            var nPeriod = input.ContainsKey("nPeriod") ? (int) input["nPeriod"] : 60;
            var period = input.ContainsKey("period") ? (int)input["period"] : 12;
            var token = input.ContainsKey("Token") ? (string)input["Token"] : "";

            var jsonResult = "{";
            if (token == @"91713974-CDB8-43E8-AD17-D7FE53ED3B53")
            {
                // Do work here
                // Step 1: Calculate the mortgage repayment
                var feeAmount = (feeAmt > 0 ? feeAmt : amount * fee) + serviceFee * nPeriod * 1.0 / period;
                if (feeAmount < 150.0)
                    feeAmount = 150.0;

                var totalAmount = amount + feeAmount;

                var pow = Math.Pow(1.0 + indicativeRate / period, -nPeriod);
                var repayment = (totalAmount * indicativeRate / period) / (1 - pow);

                var totalRepayment = repayment * nPeriod;
                var interestCharged = totalRepayment - totalAmount;

                var irr = Rate(nPeriod, period, repayment, amount, 0, indicativeRate);

                jsonResult += "\"IRR\": \"" + irr.ToString() + "\"";
            }
            jsonResult += "}";

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Access-Control-Allow-Headers", "content-type");
            response.Headers.Add("Access-Control-Allow-Methods", "GET,HEAD,PUT,PATCH,POST,DELETE");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Expose-Headers", "Link");
            response.Content = new StringContent(jsonResult, System.Text.Encoding.UTF8, "application/json");
            return response;
        }

        private double Rate(int nPeriod, int period, double pmt, double pv, double fv, double indicativeRate)
        {
            var array = new List<double>();
            array.Add(-pv);
            for (var i=1;i<=nPeriod;i++)
                array.Add(pmt);

            double irrResult = Financial.Irr(array, indicativeRate) * period;
            return irrResult;
        }
    }
}
