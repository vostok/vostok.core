﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Model;
using Vostok.Logging;

namespace Vostok.Clusterclient.Transport.Http
{
    // TODO(iloktionov): 1. Tune CurlHandler in case it backs our handler (see SetCurlOption function with CURLOPT_CONNECTTIMEOUT_MS)
    // TODO(iloktionov): 2. Classify errors from WinHttpHandler (they are Win32Exceptions, see Interop.WinHttp in corefx)
    // TODO(iloktionov): 3. Classify errors from CurlHandler (they are CurlExceptions, see Interop.CURLcode in corefx)
    // TODO(iloktionov): 4. Functional tests.
    public partial class VostokHttpTransport : IDisposable
    {
        private readonly ILog log;
        private readonly HttpClientHandler handler;
        private readonly HttpClient httpClient;

        public VostokHttpTransport(ILog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            handler = CreateClientHandler();

            TuneClientHandler();

            httpClient = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        public TimeSpan? ConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        public async Task<Response> SendAsync(Request request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                var requestMessage = SystemNetHttpRequestConverter.Convert(request);

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    cts.CancelAfter(timeout);

                    var responseMessage = await httpClient.SendAsync(requestMessage, cts.Token).ConfigureAwait(false);

                    var response = await SystemNetHttpResponseConverter.ConvertAsync(responseMessage).ConfigureAwait(false);

                    return response;
                }
            }
            catch (OperationCanceledException)
            {
                return HandleCancellationError(request, timeout, cancellationToken);
            }
            catch (Exception error)
            {
                return HandleGenericError(request, timeout, error);
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        private static HttpClientHandler CreateClientHandler()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                CheckCertificateRevocationList = false,
                MaxConnectionsPerServer = 10000,
                Proxy = null,
                PreAuthenticate = false,
                UseDefaultCredentials = false,
                UseCookies = false,
                UseProxy = false,
                ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true
            };

            return handler;
        }

        private void TuneClientHandler()
        {
            if (ConnectionTimeout.HasValue)
                WinHttpHandlerTuner.Tune(handler, ConnectionTimeout.Value, log);
        }

        private Response HandleCancellationError(Request request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return new Response(ResponseCode.Canceled);

            LogRequestTimeout(request, timeout);

            return new Response(ResponseCode.RequestTimeout);
        }

        private Response HandleGenericError(Request request, TimeSpan timeout, Exception error)
        {
            LogUnknownException(request, error);

            return new Response(ResponseCode.UnknownFailure);
        }

        #region Logging

        private void LogRequestTimeout(Request request, TimeSpan timeout)
        {
            log.Error($"Request timed out. Target = {request.Url.Authority}. Timeout = {timeout.TotalSeconds:0.000} sec.");
        }

        private void LogUnknownException(Request request, Exception error)
        {
            log.Error($"Unknown error in sending request to {request.Url.Authority}. ", error);
        }

        #endregion
    }
}