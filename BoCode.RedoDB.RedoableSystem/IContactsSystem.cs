using System.Collections.Generic;

namespace BoCode.RedoDB.RedoableSystem
{
    public interface IContactsSystem
    {
        string SomeInfo { get; set; }

        void AddContact(Contact contact);
        IEnumerable<Contact> GetAll();

        int Count();
        Contact CreateContact();
    }
}