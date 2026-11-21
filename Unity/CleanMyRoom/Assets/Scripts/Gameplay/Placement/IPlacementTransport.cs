using System;
using CleanMyRoom.Net;

namespace CleanMyRoom.Gameplay.Placement
{
    /// <summary>
    /// Thin seam around the existing MATLAB connection. It carries no gameplay
    /// knowledge and introduces no new protocol message.
    /// </summary>
    public interface IPlacementTransport
    {
        bool IsAvailable { get; }

        string LastError { get; }

        event Action<WireMessage> MessageReceived;

        event Action<ConnectionState, string> ConnectionChanged;

        string SendPlaceRequest(
            string activityId,
            string objectId,
            string targetId,
            float normalizedX,
            float normalizedY);
    }
}
