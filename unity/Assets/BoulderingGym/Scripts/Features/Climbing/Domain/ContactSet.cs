using UnityEngine;

namespace Project.Features.Climbing.Domain
{
    /// <summary>Wall surface the pose solver keeps the body in front of.</summary>
    public interface IClimbingSurface
    {
        /// <summary>Perpendicular distance from the surface, positive on the climber side.</summary>
        float SignedDistance(Vector3 point);

        /// <summary>Unit normal pointing toward the climber at a height.</summary>
        Vector3 NormalAt(float y);
    }

    public enum ContactKind
    {
        Free,
        Hold,
        Floor,
    }

    public readonly struct LimbContact
    {
        private LimbContact(ContactKind kind, Vector3 position, IClimbingSurface surface, int holdId)
        {
            Kind = kind;
            Position = position;
            Surface = surface;
            HoldId = holdId;
        }

        public ContactKind Kind { get; }

        public Vector3 Position { get; }

        /// <summary>Wall carrying the hold; null on the floor.</summary>
        public IClimbingSurface Surface { get; }

        public int HoldId { get; }

        public bool IsSupporting => Kind != ContactKind.Free;

        public static LimbContact Free => new(ContactKind.Free, Vector3.zero, null, -1);

        public static LimbContact OnHold(int holdId, Vector3 position, IClimbingSurface surface)
        {
            return new LimbContact(ContactKind.Hold, position, surface, holdId);
        }

        public static LimbContact OnFloor(Vector3 position)
        {
            return new LimbContact(ContactKind.Floor, position, null, -1);
        }
    }

    /// <summary>Immutable snapshot of what each limb rests on.</summary>
    public sealed class ContactSet
    {
        private readonly LimbContact[] _contacts;

        private ContactSet(LimbContact[] contacts)
        {
            _contacts = contacts;
        }

        public static ContactSet Empty => new(new[] { LimbContact.Free, LimbContact.Free, LimbContact.Free, LimbContact.Free });

        public LimbContact this[LimbId limb] => _contacts[(int)limb];

        public int HandsOnHolds => CountOnHolds(LimbId.LeftHand) + CountOnHolds(LimbId.RightHand);

        public int FeetSupporting => (this[LimbId.LeftFoot].IsSupporting ? 1 : 0) + (this[LimbId.RightFoot].IsSupporting ? 1 : 0);

        public int FeetOnHolds => CountOnHolds(LimbId.LeftFoot) + CountOnHolds(LimbId.RightFoot);

        public bool TouchesFloor
        {
            get
            {
                foreach (var contact in _contacts)
                {
                    if (contact.Kind == ContactKind.Floor)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasAnySupport => HandsOnHolds + FeetSupporting > 0;

        public static bool IsHand(LimbId limb) => limb == LimbId.LeftHand || limb == LimbId.RightHand;

        public static bool IsLeft(LimbId limb) => limb == LimbId.LeftHand || limb == LimbId.LeftFoot;

        public ContactSet With(LimbId limb, LimbContact contact)
        {
            var copy = (LimbContact[])_contacts.Clone();
            copy[(int)limb] = contact;
            return new ContactSet(copy);
        }

        public bool IsOnHold(LimbId limb, int holdId)
        {
            var contact = this[limb];
            return contact.Kind == ContactKind.Hold && contact.HoldId == holdId;
        }

        public static LimbId[] AllLimbs => new[] { LimbId.LeftHand, LimbId.RightHand, LimbId.LeftFoot, LimbId.RightFoot };

        private int CountOnHolds(LimbId limb) => this[limb].Kind == ContactKind.Hold ? 1 : 0;
    }
}
