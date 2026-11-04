using UnityEngine;

namespace AtLightsEnd
{
    public sealed class RoomVisitCast : MonoBehaviour
    {
        [SerializeField] private GameObject firstVisitCast;
        [SerializeField] private GameObject laterVisitCast;
        public void ApplyVisit(int visit)
        {
            if (firstVisitCast != null) firstVisitCast.SetActive(visit <= 1);
            if (laterVisitCast != null) laterVisitCast.SetActive(visit >= 2);
        }
    }
}
