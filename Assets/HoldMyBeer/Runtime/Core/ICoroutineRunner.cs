using System.Collections;
using UnityEngine;

namespace HoldMyBeer.Core
{
    /// <summary>
    /// Lets plain C# services drive coroutines without being MonoBehaviours themselves.
    /// </summary>
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator routine);
        void Stop(Coroutine routine);
    }
}
