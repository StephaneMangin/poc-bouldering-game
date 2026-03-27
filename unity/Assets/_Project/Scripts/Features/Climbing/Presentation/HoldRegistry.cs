using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class HoldRegistry : MonoBehaviour
    {
        private HoldComponent[] _holds;

        public HoldComponent[] GetAllHolds()
        {
            if (_holds == null || _holds.Length == 0)
            {
                _holds = FindObjectsByType<HoldComponent>(FindObjectsSortMode.None);
            }

            return _holds;
        }

        public void Refresh()
        {
            _holds = FindObjectsByType<HoldComponent>(FindObjectsSortMode.None);
        }
    }
}
