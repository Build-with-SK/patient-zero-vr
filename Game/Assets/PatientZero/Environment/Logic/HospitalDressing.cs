using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Environment.Logic
{
    public readonly struct DressingBox
    {
        public readonly string Name;
        public readonly Vector3 Position, Size;
        public readonly bool Collider;
        public DressingBox(string name, Vector3 position, Vector3 size, bool collider = true)
        { Name = name; Position = position; Size = size; Collider = collider; }
    }
    /// <summary>Operating-theatre dressing in the existing supply footprint; never changes shared layout or puzzle locations.</summary>
    public static class HospitalDressing
    {
        public static DressingBox[] Create()
        {
            RoomRect theatre = default;
            foreach (var room in WardLayout.Rooms) if (room.Id == "SupplyRoom") theatre = room;
            // Keep the doorway-to-spawn route clear; static furniture occupies the west perimeter only.
            var table = new Vector3(theatre.MinX + .55f, .85f, (theatre.MinZ + theatre.MaxZ) / 2);
            var cabinet = new Vector3(theatre.MaxX - .45f, 1, theatre.MinZ + .3f);
            return new[]
            {
                new DressingBox("OperatingTable", table, new Vector3(.8f, .15f, 1.7f)),
                new DressingBox("OperatingTableBase", table - Vector3.up * .45f, new Vector3(.35f, .75f, .8f)),
                new DressingBox("SurgicalLampArm_Unlit", table + Vector3.up * 1.5f, new Vector3(.05f, .5f, .05f), false),
                new DressingBox("SurgicalLamp_Unlit", table + Vector3.up * 1.2f, new Vector3(.65f, .08f, .45f), false),
                new DressingBox("SterileCabinet", cabinet, new Vector3(.65f, 2, .45f)),
                new DressingBox("InstrumentShelf", cabinet + Vector3.forward * .4f - Vector3.up * .1f, new Vector3(.65f, .07f, .3f)),
                new DressingBox("InstrumentTray", cabinet + Vector3.forward * .4f, new Vector3(.45f, .04f, .2f), false)
            };
        }
    }
}
