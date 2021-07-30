using System.Collections.Generic;

namespace BoCode.RedoDB.RedoableSystem
{
    public interface IAccountingSystem
    {
        [Redoable]
        void AddAccount(Account account);
        IEnumerable<Account> GetAll();

        int Count();
    }
}
