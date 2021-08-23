using System;
using System.Collections.Generic;
using System.Linq;

namespace BoCode.RedoDB.RedoableData
{
    public class RedoableData<T> : IRedoableData<T>
    {
        List<T> _tracked = new List<T>();
        private bool _isRedoing;
        private Func<T> _generateMethod;

        public RedoableData(Func<T> generate)
        {
            _generateMethod = generate;
        }

        public T New()
        {
            if (_isRedoing)
            {
                return Redo();
            }
            else
            {
                return TrackNewValue();
            }
        }

        private T TrackNewValue()
        {
            T newValue = _generateMethod();
            _tracked.Add(newValue);
            _isRedoing = false;
            return newValue;
        }

        public void Redoing(List<T> repeatValues)
        {
            if (_tracked.Count > 0) throw new RedoDBRedoableException("An instance of a derivation RedoableData wanted to load values to repeat, but the internal list of tracked values to repeat was not empty.");
            _tracked = repeatValues;
            _isRedoing = true;
        }

        public IEnumerable<T> Tracked => _tracked;

        public T Redo()
        {

            if (_isRedoing)
            {
                T first = _tracked.FirstOrDefault();
                if (first is null)
                {
                    //no more tracked value, stop redoing and return a new value
                    _isRedoing = false;
                    return TrackNewValue();
                }
                else
                {
                    _tracked.Remove(first);
                    if (!_tracked.Any()) _isRedoing = false;
                    return first;

                }
            }
            return TrackNewValue();
        }

        public bool IsRedoing => _isRedoing;

        public void ClearTracking()
        {
            _tracked.Clear();
            _isRedoing = false;
        }
    }
}
