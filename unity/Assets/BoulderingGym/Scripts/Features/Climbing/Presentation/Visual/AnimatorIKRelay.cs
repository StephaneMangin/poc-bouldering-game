using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    /// <summary>
    /// Placed on the same GameObject as the Animator so that OnAnimatorIK fires,
    /// then relays the callback to the <see cref="ClimberVisualOrchestrator"/> on the Player root.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorIKRelay : MonoBehaviour
    {
        [SerializeField] private ClimberVisualOrchestrator orchestrator;

        private void OnAnimatorIK(int layerIndex)
        {
            if (orchestrator != null)
            {
                orchestrator.HandleAnimatorIK(layerIndex);
            }
        }
    }
}
