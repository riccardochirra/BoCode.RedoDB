using System;
using System.Threading.Tasks;
using BoCode.RedoDB.Builder;
using BoCode.RedoDB.RedoableSystem;

namespace BoCode.RedoDB.ConsoleApp
{
    //This program demonstrates how a redoable system is set up. 
    //The sistem is recovered from comman logs only (no snapshot).
    //Each time the program starts it shows the current start of the system
    //and it adds 10 more contacts.
    class Program
    {
        async static Task Main(string[] args)
        {
            Console.WriteLine("Welcome to RedoDB demo!");

            RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
            builder.WithJsonAdapters(@"c:\Demo");
            IContactsSystem contacts = await builder.BuildAsync();
            using var engine = RedoDBEngine<ContactsSystem>.GetEngine(contacts);

            int count = contacts.Count();

            foreach (Contact c in contacts.GetAll())
            {
                Console.Write($"{c.Name}, ");
            }

            Console.WriteLine($"\n{count} contacts in the system.");
            Console.WriteLine("Adding other 10 contacts to the system...");
            for (int i = 0; i < 10; i++)
            {
                Contact contact = new Contact() { Name = $"Name{count + i}" };
                contacts.AddContact(contact);
            }
            Console.WriteLine("10 contacts added. Press any key to close.");
        }
    }
}
