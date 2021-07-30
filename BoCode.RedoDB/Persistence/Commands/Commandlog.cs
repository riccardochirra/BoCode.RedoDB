using System.Collections.Generic;

namespace BoCode.RedoDB.Persistence.Commands
{
    public class Commandlog
    {
        private string _name;
        private string _content;

        public Commandlog(string name, string content)
        {
            _name = name;
            _content = content;
            Commands = new List<Command>();
        }

        public string Name { get => _name; }
        public string Content { get => _content; }
        public IEnumerable<Command> Commands { get; internal set; }
    }

}
