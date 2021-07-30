using System.Collections.Generic;

namespace BoCode.RedoDB.RedoableSystem
{
    public interface IContactsSystem
    {
        void AddContact(Contact contact);
        IEnumerable<Contact> GetAll();

        int Count();
        Contact CreateContact();
    }
}