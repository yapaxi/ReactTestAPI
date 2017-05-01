using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading;
using System.Runtime.Serialization;

namespace WebApi.Controllers
{
    [Route("api/returns")]
    public class ValuesController : Controller
    {
        [JsonObject]
        public class Return
        {
            [JsonProperty("id")]
            public Guid Id { get; set; }

            [JsonProperty("marketplaceName")]
            public string MarketplaceName { get; set; }

            [JsonProperty("isRejected")]
            public bool IsRejected { get; set; }

            [JsonProperty("isCompleted")]
            public bool IsCompleted { get; set; }

            [JsonProperty("isVoided")]
            public bool IsVoided { get; set; }

            [JsonProperty("shippingLabelsSent")]
            public bool ShippingLabelsSent { get; set; }

            [JsonProperty("partialRefundAccepted")]
            public bool PartialRefundAccepted { get; set; }

            [JsonProperty("partialRefundRejected")]
            public bool PartialRefundRejected { get; set; }
        }

        private static IEnumerable<Return> GenerateReturns()
        {
            var marketplaces = new[] { "EBay", "Amazon", "Newegg", "Jet", "Walmart" };

            foreach (var m in marketplaces)
            {
                var hashCode = Math.Abs(m.GetHashCode());
                var count = hashCode % 40;

                for (var i = 0; i < count; i++)
                {
                    yield return new Return()
                    {
                        Id = Guid.NewGuid(),
                        IsRejected = (hashCode + i) % 5 == 0,
                        MarketplaceName = m,
                        IsCompleted = (hashCode + i) % 2 == 0
                    };
                }
            }

        }

        private static readonly Dictionary<ReturnAction, string> Actions = new Dictionary<ReturnAction, string>()
        {
            { ReturnAction.OfferPartialRefund, "Offer Partial Refund" },
            { ReturnAction.SendShippingLabels, "Send Shipping Labels" },
            { ReturnAction.ApproveFullRefund, "Approve Full Refund" },
            { ReturnAction.Void, "Void" },
        };
        
        private static readonly List<Return> Returns = GenerateReturns().ToList();

        // GET api/values
        [HttpGet("pending/{marketplace?}")]
        public IEnumerable<object> GetPending(string marketplace = null)
        {
            Thread.Sleep(2000);
            return Returns
                .Where(e => !e.IsCompleted && (string.IsNullOrWhiteSpace(marketplace) || e.MarketplaceName == marketplace));
        }

        // GET api/values
        [HttpGet("completed/{marketplace?}")]
        public IEnumerable<object> GetCompleted(string marketplace = null)
        {
            Thread.Sleep(2000);
            return Returns
                .Where(e => e.IsCompleted && (string.IsNullOrWhiteSpace(marketplace) || e.MarketplaceName == marketplace));
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public object Get(Guid id)
        {
            var @return = Returns.Where(e => e.Id == id).FirstOrDefault();

            
            ReturnActionView[] Filter(IReadOnlyDictionary<ReturnAction, string> actions) {
                
                if (@return == null || @return.IsCompleted || @return.IsVoided)
                {
                    return new ReturnActionView[0];
                }

                if (@return.ShippingLabelsSent)
                {
                    return actions
                        .Where(e => e.Key == ReturnAction.ApproveFullRefund || e.Key == ReturnAction.Void)
                        .Select(e => new ReturnActionView() { Code = e.Key, Name = e.Value })
                        .ToArray();
                }

                if (@return.PartialRefundRejected)
                {
                    return actions
                        .Where(e => e.Key == ReturnAction.SendShippingLabels)
                        .Select(e => new ReturnActionView() { Code = e.Key, Name = e.Value })
                        .ToArray();
                }
                else if (@return.PartialRefundAccepted)
                {
                    return new ReturnActionView[0];
                }

                return actions.Select(e => new ReturnActionView() { Code = e.Key, Name = e.Value })
                        .ToArray();
            }

            Thread.Sleep(1000);
            return new {
                @return = @return,
                availableActions = Filter(Actions)
            };
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post(Guid id, [FromBody]ActionObj value)
        {
            var ret = Returns.FirstOrDefault(e => e.Id == id);
            switch (value.Action)
            {
                case ReturnAction.Void:
                    ret.IsVoided = true;
                    ret.IsCompleted = true;
                    break;
                case ReturnAction.ApproveFullRefund:
                    ret.IsCompleted = true;
                    break;
                case ReturnAction.OfferPartialRefund:
                    if (Math.Abs(ret.Id.GetHashCode()) % 2 == 0)
                    {
                        ret.PartialRefundAccepted = true;
                    }
                    else
                    {
                        ret.PartialRefundRejected = true;
                    }
                    break;
                case ReturnAction.SendShippingLabels:
                    ret.ShippingLabelsSent = true;
                    break;
                default:
                    throw new Exception();
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
    
    public enum ReturnAction
    {
        OfferPartialRefund = 1,
        SendShippingLabels = 2,
        ApproveFullRefund = 3,
        Void = 4
    }

    public class ReturnActionView
    {
        [JsonProperty("code")]
        public ReturnAction Code { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class ActionObj
    {
        [JsonProperty("action")]
        public ReturnAction Action { get; set; }
    }
}

