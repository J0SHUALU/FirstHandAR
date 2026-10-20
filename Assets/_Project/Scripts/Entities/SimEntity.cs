using UnityEngine;

namespace FirstHand.Entities
{
    /// <summary>Anything the trainee can tap. InputManager calls OnTapped without knowing what it hit.</summary>
    public interface IInteractable
    {
        string InteractId { get; }
        void OnTapped(Vector3 worldPoint);
    }

    /// <summary>Things that can be dragged with a finger (recovery position limb handles).</summary>
    public interface IDraggable : IInteractable
    {
        void OnDragStart();
        void OnDrag(Vector3 worldPoint);
        void OnDragEnd();
    }

    /// <summary>
    /// Base for every scene object the simulation spawns: an id and a way to return to its starting state,
    /// so retries reuse the same objects instead of destroying and recreating them.
    /// </summary>
    public abstract class SimEntity : MonoBehaviour, IInteractable
    {
        [SerializeField] protected string entityId;
        public string EntityId => entityId;
        public virtual string InteractId => entityId;

        public abstract void ResetEntity();
        public virtual void OnTapped(Vector3 worldPoint) { }
    }
}
