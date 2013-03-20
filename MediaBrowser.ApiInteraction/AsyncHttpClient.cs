﻿using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Class AsyncHttpClient
    /// </summary>
    public class AsyncHttpClient : IAsyncHttpClient
    {
        /// <summary>
        /// Gets or sets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        private HttpClient HttpClient { get; set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="handler">The handler.</param>
        public AsyncHttpClient(ILogger logger, HttpMessageHandler handler)
        {
            Logger = logger;
            HttpClient = new HttpClient(handler);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public AsyncHttpClient(ILogger logger)
        {
            Logger = logger;
            HttpClient = new HttpClient();
        }

        /// <summary>
        /// Gets the stream async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<Stream> GetAsync(string url, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.Info("Sending Http Get to {0}", url);
            
            try
            {
                var msg = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

                EnsureSuccessStatusCode(msg);

                return await msg.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                Logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(url, cancellationToken, ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error requesting {0}", ex, url);
                
                throw;
            }
        }

        /// <summary>
        /// Posts the async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="postContent">Content of the post.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task<Stream> PostAsync(string url, string contentType, string postContent, CancellationToken cancellationToken)
        {
            Logger.Info("Sending Http Post to {0}", url);
            
            var content = new StringContent(postContent, Encoding.UTF8, contentType);

            try
            {
                var msg = await HttpClient.PostAsync(url, content).ConfigureAwait(false);

                EnsureSuccessStatusCode(msg);

                return await msg.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                Logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(url, cancellationToken, ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error posting {0}", ex, url);

                throw;
            }
        }

        /// <summary>
        /// Deletes the async.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        public async Task DeleteAsync(string url, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Logger.Debug("Sending Http Delete to {0}", url);

            try
            {
                using (var msg = await HttpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    EnsureSuccessStatusCode(msg);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.ErrorException("Error getting response from " + url, ex);

                throw new HttpException(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw GetCancellationException(url, cancellationToken, ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error requesting {0}", ex, url);

                throw;
            }
        }

        /// <summary>
        /// Throws the cancellation exception.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>Exception.</returns>
        private Exception GetCancellationException(string url, CancellationToken cancellationToken, OperationCanceledException exception)
        {
            // If the HttpClient's timeout is reached, it will cancel the Task internally
            if (!cancellationToken.IsCancellationRequested)
            {
                var msg = string.Format("Connection to {0} timed out", url);

                Logger.Error(msg);

                // Throw an HttpException so that the caller doesn't think it was cancelled by user code
                return new HttpException(msg, exception) { IsTimedOut = true };
            }

            return exception;
        }

        /// <summary>
        /// Ensures the success status code.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <exception cref="MediaBrowser.Model.Net.HttpException"></exception>
        private void EnsureSuccessStatusCode(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpException(response.ReasonPhrase) { StatusCode = response.StatusCode };
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                HttpClient.Dispose();
            }
        }

        /// <summary>
        /// Sets the authorization header that should be supplied on every request
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        /// <param name="paraneter">The paraneter.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetAuthorizationHeader(string scheme, string paraneter)
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, paraneter);
        }

        /// <summary>
        /// Removes the authorization header.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void RemoveAuthorizationHeader()
        {
            HttpClient.DefaultRequestHeaders.Remove("Authorization");
        }
    }
}
