using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SDC_Test.NM.Implementation
{
    internal sealed class NMHandlerCollection :
        IEnumerable<(Delegate Original, Action<NetworkReader> Wrapper)>
    {
        private readonly Dictionary<Delegate, Action<NetworkReader>> mHandlers;

        internal int Count => mHandlers.Count;

        internal NMHandlerCollection()
        {
            mHandlers = new Dictionary<Delegate, Action<NetworkReader>>();
        }

        internal bool TryAdd(Delegate original, Action<NetworkReader> wrapper)
        {
            if (original == null)
                return false;

            if (wrapper == null)
                return false;

            if (mHandlers.ContainsKey(original))
                return false;

            mHandlers.Add(original, wrapper);
            return true;
        }

        internal bool Remove(Delegate original)
        {
            if (original == null)
                return false;

            return mHandlers.Remove(original);
        }

        internal void Clear() => mHandlers.Clear();

        public IEnumerator<(Delegate Original, Action<NetworkReader> Wrapper)> GetEnumerator()
        {
            foreach (var kvp in mHandlers)
                yield return (kvp.Key, kvp.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}