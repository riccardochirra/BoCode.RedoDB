using System.Collections.Generic;
using BoCode.RedoDB.Builder;

namespace BoCode.RedoDB.Interception
{
    /// <summary>
    /// this class can be prepared by teh builder and given to the RedoEngine in order to restrict the methods
    /// of the class being intercepted.
    /// </summary>
    public class InterceptionsManager : IInterceptions, IBuilderComponent
    {
        List<string> _namesOfMehtodsOrSetterToBeIntercepted = new();
        List<string> _startingSubstringsCausingExclusion = new();
        List<string> _namesOfGetterToBeIntercepted = new();

        public void AddInterception(string methodName)
        {
            if (_namesOfMehtodsOrSetterToBeIntercepted.Contains(methodName)) return;
            _namesOfMehtodsOrSetterToBeIntercepted.Add(methodName);
        }

        public void AddInterceptions(string[] methodNames)
        {
            _namesOfMehtodsOrSetterToBeIntercepted.AddRange(methodNames);
        }

        public void AssertBuildReady()
        {
            AssertAmbiguitiesBetweenInclusionsAndExclusions();
        }

        private void AssertAmbiguitiesBetweenInclusionsAndExclusions()
        {
            if (ThereAreAmbiguitiesBetweenInclusionsAndExclusions())
                throw new MissingBuilderConfigurationException("Please review your configuration of interceptions: inclusions and exclusions of method cause abmbiguity and the interception manager can't resolve it.");
        }

        private bool ThereAreAmbiguitiesBetweenInclusionsAndExclusions()
        {
            //TODO later we will improve it checking for ambiguities. For the moment we just accept all configurations.
            return false;
        }

        public bool CanIntercept(string methodName)
        {
            return IsExcluded(methodName) ? false : IsIncluded(methodName);
        }

        public void ExcludeMembersStartingWith(string startingSubstring)
        {
            _startingSubstringsCausingExclusion.Add(startingSubstring);
        }

        private bool IsExcluded(string methodName)
        {
            foreach (string s in _startingSubstringsCausingExclusion)
            {
                if (methodName.StartsWith(s)) return true;
            }
            return false;
        }

        private bool IsIncluded(string methodName)
        {
            if (_namesOfMehtodsOrSetterToBeIntercepted.Count == 0) return true;
            return _namesOfMehtodsOrSetterToBeIntercepted.Contains(methodName);
        }

        public void NoPersistence()
        {
            //do nothing, this method does not require persistence, so this configuration has no effect.
        }

        public bool CanInterceptGetter(string name)
        {
            if (!IsExcluded(name))
            {
                //Getter must be explicitely included
                return _namesOfGetterToBeIntercepted.Contains(name);
            }
            else
            {
                return false;
            }
        }

        public bool CanInterceptSetter(string name)
        {
            return CanIntercept(name);
        }

        public void AddGetterInterception(string name)
        {
            if (_namesOfGetterToBeIntercepted.Contains(name)) return;
            _namesOfGetterToBeIntercepted.Add(name);
        }
    }
}
