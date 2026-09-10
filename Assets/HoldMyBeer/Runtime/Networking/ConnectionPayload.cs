using System;
using UnityEngine;

namespace HoldMyBeer.Networking
{
    /// <summary>
    /// Sent by a client inside the connection approval request. Keep it small:
    /// NGO caps the payload (1024 bytes by default) and drops oversized connections.
    /// </summary>
    [Serializable]
    public struct ConnectionPayload
    {
        public string displayName;
        public string buildVersion;

        public static byte[] Serialize(string displayName, string buildVersion)
        {
            var payload = new ConnectionPayload { displayName = displayName, buildVersion = buildVersion };
            return System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        }

        public static bool TryDeserialize(byte[] raw, out ConnectionPayload payload)
        {
            payload = default;
            if (raw == null || raw.Length == 0)
            {
                return false;
            }

            try
            {
                payload = JsonUtility.FromJson<ConnectionPayload>(System.Text.Encoding.UTF8.GetString(raw));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rejecting malformed connection payload: {exception.Message}");
                return false;
            }
        }
    }
}
