﻿using commercetools.Common;
using Newtonsoft.Json;

namespace commercetools.CartDiscounts.UpdateActions
{
    public class ChangeCartPredicate: UpdateAction
    {
        [JsonProperty(PropertyName = "cartPredicate")]
        public string CartPredicate { get; }

        public ChangeCartPredicate(string cartPredicate)
        {
            this.Action = "changeCartPredicate";
            this.CartPredicate = cartPredicate;
        }
    }
}