using UnityEngine;

namespace FirstHand.Entities
{
    public interface IInteractable
    {
        string InteractId { get; }
        void OnTapped(Vector3 worldPoint);
    }

    public interface IDraggable : IInteractable
    {
        Vector3 DragPlanePoint { get; }
        void OnDragStart();
        void OnDrag(Vector3 worldPoint);
        void OnDragEnd();
    }

    public abstract class SimEntity : MonoBehaviour, IInteractable
    {
        [SerializeField] protected string entityId;
        public string EntityId => entityId;
        public virtual string InteractId => entityId;

        public abstract void ResetEntity();
        public virtual void OnTapped(Vector3 worldPoint) { }
    }
}
