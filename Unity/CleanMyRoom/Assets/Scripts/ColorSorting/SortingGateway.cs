using CleanMyRoom.Net;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// Minimal seam between the activity and the existing TCP client.
    /// It contains no validation logic; it only forwards a request and reports
    /// whether MATLAB processing is currently available.
    /// </summary>
    public interface ISortingGateway
    {
        bool ActivitiesEnabled { get; }
        string SendPlacement(string objectId, string targetId, float x, float y);
    }

    /// <summary>Production gateway backed by the existing NetworkManager.</summary>
    public sealed class MatlabSortingGateway : ISortingGateway
    {
        public bool ActivitiesEnabled
        {
            get
            {
                NetworkManager network = NetworkManager.Instance;
                return network != null && network.ActivitiesEnabled;
            }
        }

        public string SendPlacement(string objectId, string targetId, float x, float y)
        {
            NetworkManager network = NetworkManager.Instance;
            if (network == null)
            {
                return null;
            }
            return network.SendPlaceRequest(
                ColorSortingCatalog.ActivityId, objectId, targetId, x, y);
        }
    }
}
