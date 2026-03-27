using Project.Core.Input;
using Project.Core.Domain;
using UnityEngine;

namespace Project.Features.Climbing.Presentation
{
    public sealed class HipsMovementController : MonoBehaviour
    {
        [SerializeField] private TuningConfig tuningConfig;
        [SerializeField] private Transform movementTarget;
        [SerializeField] private InputReader inputReader;

        private void Awake()
        {
            if (movementTarget == null)
            {
                movementTarget = transform.parent != null ? transform.parent : transform;
            }

            if (inputReader == null)
            {
                inputReader = GetComponentInParent<InputReader>();
            }
        }

        private void Update()
        {
            if (tuningConfig == null || movementTarget == null)
            {
                return;
            }

            var input = inputReader != null ? inputReader.ReadMove() : Vector2.zero;
            var movement = new Vector3(input.x, input.y, 0f);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            movementTarget.position += movement * (tuningConfig.hipsMoveSpeed * Time.deltaTime);
        }
    }
}
