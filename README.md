# BoCode.RedoDB

## Automagic persistence for your object's state

BoCode.RedoDB, short RedoDB, is a library adding the ability to persist your object state with almost no extra coding.

In this documentation, the term *System* will be used for the object being persisted by RedoDB. Alternatively, we will call it also *redoable object*. 

Instead of constructing the system directly using the constructor of the system's entry class, you let construct the object instance using the RedoDBEngineBuilder. Example:

```c#
RedoDBEngineBuilder<Contacts, IContacts> builder = new();
builder.WithDataPath("c:\data");
IContacts contacts = builder.Build();
```

Es you see in the code above, your system must implement an interface defining all the members needed to manipulate the system's state. 

*Contacts* implements methods like *AddContact*, *GetAll* and *Count*. 

The system, in order to be persistable must be serializable. Basically this is all you need to make your system "redoable" - as we like to say.

## How does it work? The Commandlog

The RedoDBEngineBuilder constructs a proxy object with the system's interface. Every call to methods or properties of the system can then be intercepted by the RedoDBEngine instance associated to your system instance.

When a method is intercepted, the RedoDBEngine writes a log-entry for the command executed in the command log. A command represents the method call and saves the parameters. 

>NOTE: All arguments of a method invocation must be serializable too.

Basically, this would be all we need to reconstruct the state of your redoable object the next time you build the system using the RedoDBEngineBuilder. The builder would redo the commands against the constructing instance, and voilà, you have your state back online.

Basically, we use event sourcing to save state changes. 

## Optimization: TakeSnapshots
Saving a lot of commands would generate a very long command log. To maintain recovering time stable **RedoDBEngine** has a method called __TakeSnapshot__. This method cuts the command log and saves an image of the object graph kept by your system's instance. 

The next time your system will be recovered **RedoDBEngineBuilder** will use the latest snapshot and redo commands tracked after the call of the last TakeSnapshot.

## Commandlog and Snapshot are Json files

If you do not inject a command adapter or a snapshot adapter while building the RedoDBEngine, the default adapters are used. They are implemented to save command logs and snapshots as Json-files.

>NOTE the command log is a file containing a json per command. Itself is not a Json-file, because the root element is left out.

RedoDB can easily be extended, and you can implement your adapters.

## Compensation
If a command starts execution and changes the state of your system, but before it ends an exception is thrown, then the state of your system would be invalid.

RedoDB can compensate automatically. The system's state is rolled back to the one before the faulty command's execution.

You should think about compensation in your system's code. If you don't own the code, you can activate compensation. This way, you can be 100% sure of avoiding invalid states.

The activation of compensation looks as follows:

```c#
RedoDBEngineBuilder<Contacts, IContacts> builder = new()
    .WithDataPath("c:\data")
    .WithCompensation;
IContacts contacts = builder.Build();
```

## Configure **Interception** using the builder

You can instruct the builder on what to intercept or mark methods as 'redoable' using the method attribute. This way you can reduce how many commands are tracked and improve recovering time.

> NOTE: you must intercept all methods changing the state of your system. You should configure away only members you are sure they don't change the state of the system. Methods beginning with 'Get' are likely to be read-only methods.

Example
```c#
RedoDBEngineBuilder<Contacts, IContacts> builder = new()
    .WithDataPath("c:\data")
    .ExcludeMethodsStartingWith("Get");
IContacts contacts = builder.Build();

```

Another method to control interception is the 'AddInterception' method. 

Example
```c#
RedoDBEngineBuilder<Contacts, IContacts> builder = new()
    .WithDataPath("c:\data")
    .AddInterception("AddContact");
IContacts contacts = builder.Build();
```

Using this method implies that you want to control interception by adding all the methods you want to intercept manually. Our system would not intercept 'GetAll' or 'Count' anymore.

## Configure interception using the __Redoable__ attribute

Example:

```c#
    public interface IAccountingSystem
    {
        [Redoable]
        void AddAccount(Account account);
```

Other methods without the attribute are now excluded from interception. 

## More than one redoable system in your project?

Let imagine that your project your are dealing with several redoable systems. You can't use the same DataPath (the directory for command logs and snapshots) for all systems. The first solution would be to configure different DataPaths for each system. An alternative is the RedoSubdirectoryAttribute. This class level Attribute has a parameter called 'Subdirectory' and lets you specify the name of a subdirectory of DataPath. 

Example:
```c#
    [RedoSubdirectory("Subdirectory")]
    [Serializable]
    public class TestSubdirectorySystem : ITestSubdirectorySystem 
    {
        public void DoSomething()
        {
            System.Diagnostics.Debug.WriteLine("Doing something...");
        }
    }
```

The advantage of this approach is that a single configuration setting (for DataPath) is enough. If you move to another directory the DataPath, all systems will automatically follow.

## Final notes
Be aware of this scalability issue: The RedoDBEngine locks to only one writer, while you can have many simultaneous readers. If different clients try to write simultaneously, the calls are queued. 





