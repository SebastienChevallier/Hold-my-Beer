using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldMyBeer.Core
{
    /// <inheritdoc cref="ISceneLoader"/>
    public sealed class SceneLoader : ISceneLoader
    {
        private readonly ICoroutineRunner _runner;

        public SceneLoader(ICoroutineRunner runner)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public void Load(string sceneName, Action onLoaded = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new ArgumentException("Scene name is empty.", nameof(sceneName));
            }

            _runner.Run(LoadRoutine(sceneName, onLoaded));
        }

        private static IEnumerator LoadRoutine(string sceneName, Action onLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"Scene '{sceneName}' is missing from the build settings.");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            onLoaded?.Invoke();
        }
    }
}
