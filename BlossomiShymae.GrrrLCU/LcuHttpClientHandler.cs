namespace BlossomiShymae.GrrrLCU
{
    internal class LcuHttpClientHandler : HttpClientHandler
    {
        public ProcessInfo? ProcessInfo { get; internal set; } = null;

        public RiotAuthentication? RiotAuthentication => ProcessInfo == null ?
            null : new(ProcessInfo.RemotingAuthToken);

        public string? BaseAddress => ProcessInfo == null ?
            null : $"https://127.0.0.1:{ProcessInfo.AppPort}";

        private Lazy<bool> _isFirstRequest = new(() => true);

        private Lazy<bool> _isFailing = new(() => false);


        internal LcuHttpClientHandler() : base()
        {
            ServerCertificateCustomValidationCallback = DangerousAcceptAnyServerCertificateValidator;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (_isFirstRequest.Value)
                {
                    _isFirstRequest = new(() => true);
                    ProcessInfo = ProcessFinder.Get();
                }
                if (_isFailing.Value)
                {
                    _isFailing = new(() => false);
                    ProcessInfo = ProcessFinder.Get();
                }

                PrepareRequestMessage(request);
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                return response;
            }
            catch (HttpRequestException)
            {
                try
                {
                    ProcessInfo = ProcessFinder.Get();

                    PrepareRequestMessage(request);
                    var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    return response;
                }
                catch (InvalidOperationException)
                {
                    _isFailing = new(() => true);
                    throw;
                }
            }
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (_isFirstRequest.Value)
                {
                    _isFirstRequest = new(() => true);
                    ProcessInfo = ProcessFinder.Get();
                }
                if (_isFailing.Value)
                {
                    _isFailing = new(() => false);
                    ProcessInfo = ProcessFinder.Get();
                }

                PrepareRequestMessage(request);

                return base.Send(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                try
                {
                    ProcessInfo = ProcessFinder.Get();

                    PrepareRequestMessage(request);

                    return base.Send(request, cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    _isFailing = new(() => true);
                    throw;
                }
            }
        }

        private void PrepareRequestMessage(HttpRequestMessage request)
        {       
            if (BaseAddress != null)
            {
                request.RequestUri = new Uri($"{request.RequestUri?.ToString().Replace("https://127.0.0.1", BaseAddress)}");
                
                if (!ProcessFinder.IsPortOpen()) 
                    throw new InvalidOperationException("Failed to connect to LCUx process port.");
            }
            request.Headers.Authorization = RiotAuthentication?.ToAuthenticationHeaderValue();
        }
    }
}