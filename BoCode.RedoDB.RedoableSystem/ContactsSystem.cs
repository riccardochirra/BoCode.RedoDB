using BoCode.RedoDB.RedoableData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace BoCode.RedoDB.RedoableSystem
{

    /// <summary>
    /// This is the system we want to be redoable
    /// </summary>
    [Serializable]
    public class ContactsSystem : IContactsSystem, IDependsOnRedoableGuid, IDependsOnRedoableClock
    {
        [JsonProperty]
        private List<Contact> _contacts = new List<Contact>();
        private IRedoableGuid _redoableGuid;
        private IRedoableClock _clock;

        public void AddContact(Contact contact) => _contacts.Add(contact);

        public Contact CreateContact()
        {
            Contact c = new Contact(_redoableGuid.New(), _clock.Now);
            AddContact(c); //internal call should not be tracked.
            return c;
        }

        public int Count() => _contacts.Count;

        public IEnumerable<Contact> GetAll() => _contacts;

        public void SetRedoableGuid(IRedoableGuid redoableGuid)
        {
            _redoableGuid = redoableGuid;
        }

        public void SetRedoableClock(IRedoableClock clock)
        {
            _clock = clock;
        }

        public string SomeInfo { get; set; }

    }
}
