﻿using System;

using commercetools.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace commercetools.Orders
{
    /// <summary>
    /// OrderFromCartDraft
    /// </summary>
    /// <see href="http://dev.commercetools.com/http-api-projects-orders.html#orderfromcartdraft"/>
    public class OrderFromCartDraft
    {
        #region Properties

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "orderNumber")]
        public string OrderNumber { get; set; }

        [JsonProperty(PropertyName = "paymentState")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PaymentState? PaymentState { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">The unique id of the cart from which an order is created.</param>
        /// <param name="version">Cart version</param>
        public OrderFromCartDraft(string id, int version)
        {
            this.Id = id;
            this.Version = version;
        }

        #endregion
    }
}