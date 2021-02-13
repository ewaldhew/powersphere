using UnityEngine;

namespace MessageTypes
{
    public struct Pickup
    {
        public GameObject[] candidates;
        public GameObject picker;
        public int slotIndex;
    }

    public struct PickupComplete
    {
        public GameObject picker;
        public GameObject picked;
        public int slotIndex;
    }

    public struct Goal
    {
        public GameObject powerSphere;
        public GameObject goal;
    }
}