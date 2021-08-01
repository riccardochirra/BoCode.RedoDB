using BoCode.RedoDB.Builder;

namespace BoCode.RedoDB
{
    public interface IInterceptions : IBuilderComponent
    {
        void AddInterception(string methodName);
        void AddInterceptions(string[] methodNames);
        bool CanIntercept(string methodName);
        void ExcludeMembersStartingWith(string startingSubstring);
        bool CanInterceptGetter(string name);
        bool CanInterceptSetter(string name);
        void AddGetterInterception(string name);
    }
}
