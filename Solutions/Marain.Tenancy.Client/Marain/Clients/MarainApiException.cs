// <copyright file="UserNotificationsApiException.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Clients
{
    using System;
    using System.Net;

    /// <summary>
    /// An exception thrown by a UserNotifications API client.
    /// </summary>
    [Serializable]
    public class MarainApiException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarainApiException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MarainApiException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarainApiException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The underlying exception.</param>
        public MarainApiException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Gets or sets the status code of the response.
        /// </summary>
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public string? ResponseMessage { get; set; }
    }
}