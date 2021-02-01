using UnityEngine;

namespace MessageTypes
{
    public struct Pickup
    {
        public GameObject picker;
        public GameObject picked;
        public int slotIndex;
    }
}