using System;
using System.Collections.Generic;

namespace SCP096TheShyGuy
{
    // FIFO survives within a play session. After loading a saved world the
    // witness trait is re-scanned, but the historic assignment order is NOT
    // recoverable using the APIs verified so far (documented limitation).
    internal sealed class WitnessQueue
    {
        private readonly List<object> _actors = new List<object>();
        internal int Count => _actors.Count;
        internal object First => _actors.Count > 0 ? _actors[0] : null;

        internal bool Contains(object actor)
        {
            foreach (object a in _actors)
                if (ReferenceEquals(actor, a)) return true;
            return false;
        }

        internal bool Enqueue(object actor, int limit)
        {
            if (actor == null || Contains(actor) || _actors.Count >= limit) return false;
            _actors.Add(actor);
            return true;
        }

        internal bool Remove(object actor)
        {
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!ReferenceEquals(actor, _actors[i])) continue;
                _actors.RemoveAt(i);
                return true;
            }
            return false;
        }

        internal int RemoveInvalid(Func<object, bool> valid)
        {
            int removed = 0;
            for (int i = _actors.Count - 1; i >= 0; i--)
                if (!valid(_actors[i])) { _actors.RemoveAt(i); removed++; }
            return removed;
        }

        internal object[] Snapshot() => _actors.ToArray();
        internal void Clear() => _actors.Clear();
    }
}
