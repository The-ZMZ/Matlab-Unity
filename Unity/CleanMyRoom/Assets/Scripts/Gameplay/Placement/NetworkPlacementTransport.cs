using System;
using CleanMyRoom.Net;

namespace CleanMyRoom.Gameplay.Placement
{
    /// <summary>
    /// Adapts the existing <see cref="NetworkManager"/> to the placement seam.
    /// No new socket, request type, or message structure is introduced.
    /// </summary>
    public sealed class NetworkPlacementTransport : IPlacementTransport
    {
        private readonly NetworkManager network;

        public NetworkPlacementTransport(NetworkManager manager = null)
        {
            network = manager ?? NetworkManager.Instance;
        }

        public bool IsAvailable
        {
            get { return network != null && network.ActivitiesEnabled; }
        }

        public string LastError
        {
            get
            {
                return network != null && !string.IsNullOrEmpty(network.LastError)
                    ? network.LastError
                    : "MATLAB connection is unavailable.";
            }
        }

        public event Action<WireMessage> MessageReceived
        {
            add
            {
                if (network != null)
                {
                    network.MessageReceived += value;
                }
            }
            remove
            {
                if (network != null)
                {
                    network.MessageReceived -= value;
                }
            }
        }

        public event Action<ConnectionState, string> ConnectionChanged
        {
            add
            {
                if (network != null)
                {
                    network.ConnectionChanged += value;
                }
            }
            remove
            {
                if (network != null)
                {
                    network.ConnectionChanged -= value;
                }
            }
        }

        public string SendPlaceRequest(
            string activityId,
            string objectId,
            string targetId,
            float normalizedX,
            float normalizedY)
        {
            return network == null
                ? null
                : network.SendPlaceRequest(activityId, objectId, targetId, normalizedX, normalizedY);
        }
    }
}
