using System.Collections.Concurrent;

namespace WebApi.V2_Secure.Services
{
    public class RateLimitingService : IRateLimitingService
    {
        private class RateBucket
        {
            public List<DateTime> Timestamps { get; } = new();
            public object Lock { get; } = new();
        }

        private class LoginFailureState
        {
            public int Attempts { get; set; }
            public DateTime WindowStart { get; set; }
            public DateTime? BlockedUntil { get; set; }
            public object Lock { get; } = new();
        }

        private readonly ConcurrentDictionary<string, RateBucket> _rateLimitBuckets = new();
        private readonly ConcurrentDictionary<string, LoginFailureState> _loginFailures = new();

        public bool IsAllowed(string key, int limit, TimeSpan window, out int retryAfterSeconds)
        {
            retryAfterSeconds = 0;
            var now = DateTime.UtcNow;
            var bucket = _rateLimitBuckets.GetOrAdd(key, _ => new RateBucket());

            lock (bucket.Lock)
            {
                var cutoff = now - window;
                bucket.Timestamps.RemoveAll(t => t < cutoff);

                if (bucket.Timestamps.Count >= limit)
                {
                    var oldestInWindow = bucket.Timestamps[0];
                    var expiresAt = oldestInWindow + window;
                    var diff = (int)Math.Ceiling((expiresAt - now).TotalSeconds);
                    retryAfterSeconds = Math.Max(diff, 1);
                    return false;
                }

                bucket.Timestamps.Add(now);
                return true;
            }
        }

        public bool IsLoginBlocked(string ip, string username, out int retryAfterSeconds)
        {
            retryAfterSeconds = 0;
            var key = $"{ip}:{username.ToLowerInvariant()}";
            if (_loginFailures.TryGetValue(key, out var state))
            {
                lock (state.Lock)
                {
                    var now = DateTime.UtcNow;
                    if (state.BlockedUntil.HasValue)
                    {
                        if (now < state.BlockedUntil.Value)
                        {
                            retryAfterSeconds = (int)Math.Ceiling((state.BlockedUntil.Value - now).TotalSeconds);
                            return true;
                        }
                        else
                        {
                            // Hết thời gian phạt, reset lại trạng thái
                            state.BlockedUntil = null;
                            state.Attempts = 0;
                            state.WindowStart = now;
                        }
                    }
                }
            }
            return false;
        }

        public void RecordFailedLogin(string ip, string username)
        {
            var key = $"{ip}:{username.ToLowerInvariant()}";
            var state = _loginFailures.GetOrAdd(key, _ => new LoginFailureState
            {
                Attempts = 0,
                WindowStart = DateTime.UtcNow
            });

            lock (state.Lock)
            {
                var now = DateTime.UtcNow;
                if ((now - state.WindowStart).TotalSeconds > 60)
                {
                    state.Attempts = 1;
                    state.WindowStart = now;
                    state.BlockedUntil = null;
                }
                else
                {
                    state.Attempts++;
                    // Nếu nhập sai liên tục >= 5 lần trong vòng 60 giây -> Khóa trong 60 giây
                    if (state.Attempts >= 5)
                    {
                        state.BlockedUntil = now.AddSeconds(60);
                    }
                }
            }
        }

        public void ResetFailedLogin(string ip, string username)
        {
            var key = $"{ip}:{username.ToLowerInvariant()}";
            _loginFailures.TryRemove(key, out _);
        }
    }
}
