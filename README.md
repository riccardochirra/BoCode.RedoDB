# BoCode.RedoDB

## Automagical persistence for your object's state

__BoCode.RedoDB__, short __RedoDB__, is a library adding the ability to persist your object state with almost no extra coding.

In this documentation, the term *System* will be used for the object being persisted by RedoDB. Alternatively, we will call it also *redoable object*. 

Instead of constructing the system directly using the constructor of the system's entry class, you let construct the object instance using the RedoDBEngineBuilder. Example:

```c#
RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new();
builder.WithJsonAdapters("c:\data");
IContactsSystem contacts = builder.Build();
```

Your system must implement an interface defining all members changing the system's state. 

The system, to be persistable, must be serializable. It is all you need to make your system "redoable" - as we like to say.

## How does it work? The Commandlog

We use the event sourcing pattern.

The __RedoDBEngineBuilder__ constructs a proxy object using system's interface. Every call to methods or properties of the system can then be intercepted by the __RedoDBEngine__ instance associated to your system instance.

When a method is intercepted, the __RedoDBEngine__ writes an entry for the command executed in the command log. A command represents the method call and saves the parameters, among other execution context information. 

>NOTE: All arguments of a method invocation must be serializable too.

This would be all we need to reconstruct the state of your redoable object using the __RedoDBEngineBuilder__. The builder would redo the commands against the constructing instance, and voilà, you have your state back online.

## Optimization: take snapshots
Saving a lot of commands would generate a very long command log. To maintain recovering time stable __RedoDBEngine__ has a method called __TakeSnapshot__. This method cuts the command log and saves an image of the object graph kept by your system's instance. 

The next time your system will be recovered **RedoDBEngineBuilder** will use the latest snapshot and redo commands tracked after the last snapshot.

## Commandlogs and Snapshots are Json files

If you do not inject a command adapter or a snapshot adapter while building the RedoDBEngine, the default adapters are used. They are implemented to save command logs and snapshots as Json-files.

>NOTE the command log is a file containing a Json per command. Itself is not a Json-file, because the root element is left out.

## You can extend RedoDB
RedoDB can easily be extended, and you can implement your own adapters by implementing the __ICommandAdapter__ and __ISnapshotAdapters__ interface. 

>ATTENTION: Do not mix implementations of command adapters and snapshot adapters, as they come in couples because they need to "talk" to each other to coordinate, for example, file names. So, if you decide to implement your own commandlog adapter, implement a snapshot adapter too.

## Compensation

_"Compensating activities have to generate a state of the accessed data object which is identical to the state at the point in time the original activity started, i.e., object in the database(s) must have the same value."_ (from Advanced Transaction Models and Architectures, Springer Science+Business Media, LLC)

If a command starts execution and changes the state of your system, but before it ends, an exception is thrown, then the state of your system would be invalid.

RedoDB can compensate automatically. The system's state is rolled back to the one before the faulty command's execution.

If you don't own the code, and you are not sure if the system you are using compensate internally in case of exceptions, then you can activate RedoDB compensation. This way, you can be 100% sure of avoiding invalid states.

The activation of compensation looks as follows:

```c#
RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new()
    .WithJsonAdapters("c:\data")
    .WithCompensation;
IContactsSystem contacts = builder.Build();
```
>NOTE: during compensation  writers are queued, so response time could slow down. For this reason is preferable that you ensure a valid state in the internal design of your system. This way you can avoid to activate compensation and you can control performance.

## Configure **Interception** using the builder

You can instruct the builder on what to intercept or mark methods as 'redoable' using the method attribute. This way, you can reduce how many commands are tracked and improve recovering time.

> NOTE: you must intercept all methods changing the state of your system. You should configure away only members you are sure are not changing the state of the system. Methods beginning with 'Get' are likely to be read-only methods.

Example
```c#
RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new()
    .WithJsonAdapters("c:\data")
    .ExcludeMethodsStartingWith("Get");
IContactsSystem contacts = builder.Build();

```

Another method to control interception is the 'AddInterception' method. 

Example
```c#
RedoDBEngineBuilder<ContactsSystem, IContactsSystem> builder = new()
    .WithJsonAdapters("c:\data")
    .AddInterception("AddContact");
IContactsSystem contacts = builder.Build();
```

Using this method implies that you want to control Interception by adding all the methods you want to intercept manually. Our system would not intercept 'GetAll' or 'Count' anymore.

## Configure Interception using the __Redoable__ attribute

Example:

```c#
    public interface IAccountingSystem
    {
        [Redoable]
        void AddAccount(Account account);
```

Other methods without the attribute are now excluded from Interception. 

## More than one redoable system in your project?

Let imagine that your project your are dealing with several redoable systems. You can't use the same DataPath (the directory for command logs and snapshots) for all systems. The first solution would be to configure different DataPaths for each system. An alternative is the RedoSubdirectoryAttribute. This class level Attribute has a parameter called 'Subdirectory'. It lets you specify the name of a subdirectory of DataPath (root path of your persistence files). 

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

The advantage of this approach is that a single configuration setting (for DataPath) is enough. If you move the DataPath to another directory, all systems will automatically follow.

## RedoableGuid
If your system needs to use __Guid.NewGuid()__ you would not be able to recover the state, as while recovering the command execution would be repeated but generate another value for the Guid. The solution to this problem is to relay upon the __RedoableGuid__ class. This generic class can be used with the __Guid__ type. Your system must implement the interface __IDependsOnRedoableGuid__ and in your code, to obtain a new Guid, you should use only the instance of __RedoableGuid__ provided through the method __SetRedoableGuid__. The original value will be remembered and returned avery time the same command will be redone.

Example
```C#
    [Serializable]
    public class ContactsSystem : IContactsSystem, IDependsOnRedoableGuid
    {
        IRedoableGuid _redoableGuid;

        SetRedoableGuid(IRedoableGuid redoableGuid)
        {
            _redoableGuid = redoableGuid
        }

        ...

        public Contact CreateContact(redoableGuid.New())
        {
            ...
```

## RedoableClock
If your system needs to call DateTime.Now, for the same reasons as for Guid, you must implement __IDependsOnRedoableClock__ and use the __ReodableClock__ instance to get a redoable value for DateTime.Now. Use only the instance provided to your system by __RedoDBEngine__ through __SetRedoableClock__.

## Please note
Be aware of this scalability issue: __RedoDBEngine__ locks to only one writer, while you can have many simultaneous readers. If different clients try to write simultaneously, the calls are queued. 

I have already used the strategy implemented by RedoDB in productive software with success. The biggest snapshots handled in my projects were 6GB big. __RedoDB is not yet tested in such scenarios.__

RedoDB has been designed to provide a rapid way to get a fully functional data repository for the development environment so that Front-End developers can start using a fake of the Back-End system very soon in project development. 











