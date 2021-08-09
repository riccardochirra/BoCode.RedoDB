using System;
using System.Collections.Generic;

namespace BoCode.RedoDB.RedoableSystem
{
    [Serializable]
    public class AccountingSystem : IAccountingSystem
    {
        private List<Account> _accounts = new List<Account>();

        public void AddAccount(Account account) => _accounts.Add(account);

        public int Count() => _accounts.Count;

        public IEnumerable<Account> GetAll() => _accounts;
    }
}
