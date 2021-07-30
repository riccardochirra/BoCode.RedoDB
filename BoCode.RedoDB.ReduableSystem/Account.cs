using System;

namespace BoCode.RedoDB.RedoableSystem
{
    [Serializable]
    public class Account
    {
        public string Name { get; set; }
        public decimal Balance { get; set; }
    }
}
