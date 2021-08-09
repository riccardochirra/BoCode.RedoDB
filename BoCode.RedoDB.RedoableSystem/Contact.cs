using Newtonsoft.Json;
using System;

namespace BoCode.RedoDB.RedoableSystem
{
    [Serializable]
    public class Contact
    {
        [JsonProperty("Id")]
        private Guid _id;
        [JsonProperty("TimeStamp")]
        private DateTime _timeStamp;

        public Contact()
        {
        }

        public Contact(Guid id, DateTime timeStamp)
        {
            _id = id;
            _timeStamp = timeStamp;
        }

        public string Name { get; set; }
        public int BirthYear { get; set; }

        [JsonIgnore]
        public Guid Id => _id;
        [JsonIgnore]
        public DateTime TimeStamp => _timeStamp;
    }
}
