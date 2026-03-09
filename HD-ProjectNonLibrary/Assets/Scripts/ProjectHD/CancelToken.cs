using System.Threading;

namespace ProjectHD
{
    public class CancelToken
    {
        public CancellationTokenSource TokenSource => _cancellationaTokenSource;
        public CancellationToken Token => _cancellationaTokenSource.Token;
        private CancellationTokenSource _cancellationaTokenSource;

        public void SetToken()
        {
            if (_cancellationaTokenSource != null)
            {
                _cancellationaTokenSource.Cancel();
                _cancellationaTokenSource.Dispose();
            }
            _cancellationaTokenSource = new CancellationTokenSource();
        }

        public void UnSetToken()
        {
            if (_cancellationaTokenSource != null)
            {
                _cancellationaTokenSource.Cancel();
                _cancellationaTokenSource.Dispose();
                _cancellationaTokenSource = null;
            }
        }
    }
}