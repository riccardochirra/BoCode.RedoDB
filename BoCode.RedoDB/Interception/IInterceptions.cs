using BoCode.RedoDB.Builder;

namespace BoCode.RedoDB
{
    public interface IInterceptions : IBuilderComponent
    {
        void AddInterception(string methodName);
        void AddInterceptions(string[] methodNames);
        bool CanIntercept(string methodName);
        void ExcludeMethodsStartingWith(string startingSubstring);
        bool CanInterceptGetter(string name);
    }
}
