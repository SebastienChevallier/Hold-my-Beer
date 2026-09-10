using System.Collections;
using UnityEngine;

namespace HoldMyBeer.Core
{
    /// <inheritdoc cref="ICoroutineRunner"/>
    public sealed class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);

        public void Stop(Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
    }
}
