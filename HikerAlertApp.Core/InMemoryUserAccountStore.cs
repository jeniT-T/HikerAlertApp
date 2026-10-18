using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace HikerAlertApp.Core
{
    public sealed class InMemoryUserAccountStore : IUserAccountStore
    {
        private readonly ConcurrentDictionary<string, UserAccount> _accounts = new(StringComparer.Ordinal);
        private readonly IClock _clock;

        public InMemoryUserAccountStore(IClock? clock=null) =>_clock = clock ?? SystemClock.Instance;

        public int Count => _accounts.Count;

        public Task<bool> ExistsAsync(string normalisedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accounts.ContainsKey(normalisedEmail));
        }

        public Task<UserAccount?> GetAccountAsync(string normalisedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accounts.GetValueOrDefault(normalisedEmail));
        }

        public Task CreateAsync(string normalisedEmail, string passwordHash, CancellationToken cancellationToken = default)
        {
            _accounts[normalisedEmail] = new UserAccount(normalisedEmail, passwordHash, UserRole.Hiker, _clock.UtcNow);
            return Task.CompletedTask;
        }

        public Task<UserAccount?> FindAsync(string normalisedEmail, CancellationToken cancellationToken = default)
       => Task.FromResult(
           _accounts.TryGetValue(normalisedEmail, out var account) ? account : null);

        public Task<bool> DeleteAsync(string normalisedEmail, CancellationToken cancellationToken = default)
            => Task.FromResult(_accounts.TryRemove(normalisedEmail, out _));
        public void Seed(UserAccount account) => _accounts[account.normalisedEmail] = account;
    }
}
