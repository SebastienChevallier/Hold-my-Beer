namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Result of a host/join attempt. Networking fails constantly and for boring
    /// reasons, so failure is a value, not an exception: the UI has to show it.
    /// </summary>
    public readonly struct SessionResult
    {
        private SessionResult(bool success, string joinCode, string error)
        {
            Success = success;
            JoinCode = joinCode;
            Error = error;
        }

        public bool Success { get; }

        /// <summary>Relay join code to share with friends. Empty in Direct IP mode.</summary>
        public string JoinCode { get; }

        public string Error { get; }

        public static SessionResult Ok(string joinCode = "") => new(true, joinCode ?? string.Empty, string.Empty);

        public static SessionResult Fail(string error) => new(false, string.Empty, error);
    }
}
