using System.Collections.Generic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Environment.Logic
{
    public enum DressingStyle { Metal, Fabric, Ceramic, Wayfinding, Trace, Damage }
    public readonly struct DressingBox
    {
        public readonly string Name;
        public readonly Vector3 Position, Size;
        public readonly bool Collider;
        public readonly DressingStyle Style;
        public DressingBox(string name, Vector3 position, Vector3 size, bool collider = true, DressingStyle style = DressingStyle.Metal)
        { Name = name; Position = position; Size = size; Collider = collider; Style = style; }
    }
    /// <summary>Operating-theatre dressing in the existing supply footprint; never changes shared layout or puzzle locations.</summary>
    public static class HospitalDressing
    {
        public static DressingBox[] Create()
        {
            RoomRect theatre = default, patient = default, corridor = default, exit = default;
            foreach (var room in WardLayout.Rooms)
            { if (room.Id == "SupplyRoom") theatre = room; if (room.Id == "IsolationB7") patient = room; if (room.Id == "Corridor") corridor = room; if (room.Id == "Vestibule") exit = room; }
            var bed = new Vector3(patient.MinX + .65f, .65f, patient.MinZ + .8f);
            // Keep the doorway-to-spawn route clear; static furniture occupies the west perimeter only.
            var table = new Vector3(theatre.MinX + .55f, .85f, (theatre.MinZ + theatre.MaxZ) / 2);
            var cabinet = new Vector3(theatre.MaxX - .45f, 1, theatre.MinZ + .3f);
            var boxes = new List<DressingBox>
            {
                new DressingBox("PatientBed", bed, new Vector3(.8f, .12f, 1.4f)),
                new DressingBox("PatientMattress", bed + Vector3.up * .1f, new Vector3(.75f, .1f, 1.35f), false, DressingStyle.Fabric),
                new DressingBox("OperatingTable", table, new Vector3(.8f, .15f, 1.7f)),
                new DressingBox("OperatingTableBase", table - Vector3.up * .45f, new Vector3(.35f, .75f, .8f)),
                new DressingBox("SurgicalLampArm_Unlit", table + Vector3.up * 1.5f, new Vector3(.05f, .5f, .05f), false),
                new DressingBox("SurgicalLamp_Unlit", table + Vector3.up * 1.2f, new Vector3(.65f, .08f, .45f), false),
                new DressingBox("SterileCabinet", cabinet, new Vector3(.65f, 2, .45f)),
                new DressingBox("InstrumentShelf", cabinet + Vector3.forward * .4f - Vector3.up * .1f, new Vector3(.65f, .07f, .3f)),
                new DressingBox("InstrumentTray", cabinet + Vector3.forward * .4f, new Vector3(.45f, .04f, .2f), false)
            };
            boxes.Add(new DressingBox("BedFrame", bed - Vector3.up * .25f, new Vector3(.65f, .45f, 1.15f)));
            boxes.Add(new DressingBox("BedHeadboard", bed - Vector3.forward * .65f + Vector3.up * .3f, new Vector3(.85f, .55f, .04f), false, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("IVStand", bed - Vector3.right * .25f + Vector3.forward * .65f + Vector3.up * .45f, new Vector3(.035f, 1.65f, .035f), false));
            boxes.Add(new DressingBox("IVBag", bed - Vector3.right * .15f + Vector3.forward * .65f + Vector3.up * 1.1f, new Vector3(.15f, .25f, .04f), false, DressingStyle.Ceramic));
            var rail = new Vector3(patient.MinX + 1.18f, 2.5f, patient.MinZ + 1.05f);
            boxes.Add(new DressingBox("PrivacyRail", rail, new Vector3(.04f, .04f, 1.6f), false));
            for (int i = 0; i < 4; i++)
                boxes.Add(new DressingBox("PrivacyCurtain_" + i, rail - Vector3.up * .9f + Vector3.forward * (i * .3f - .55f) + Vector3.right * (i % 2 == 0 ? .025f : 0),
                    new Vector3(.035f, 1.7f, .31f), true, DressingStyle.Fabric));
            boxes.Add(new DressingBox("TheatrePrivacyScreen", new Vector3((theatre.MinX + theatre.MaxX) * .5f, 1.4f, theatre.MinZ + .08f),
                new Vector3(1.5f, 1.7f, .035f), true, DressingStyle.Fabric));
            boxes.Add(new DressingBox("TheatreMattress", table + Vector3.up * .12f, new Vector3(.72f, .08f, 1.55f), false, DressingStyle.Fabric));
            boxes.Add(new DressingBox("FoldedSterileDrape", table + Vector3.up * .19f + Vector3.forward * .35f, new Vector3(.65f, .05f, .45f), false, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("OxygenCylinder", new Vector3(theatre.MaxX - .2f, .6f, theatre.MinZ + 1.4f), new Vector3(.2f, 1.1f, .2f), true, DressingStyle.Wayfinding));
            // Wall-mounted colour bands and restrained surface traces are decoration, never navigation colliders.
            foreach (var span in new RoomGeometry().Walls)
                if (System.Math.Abs(span.A.z - corridor.MaxZ) < .001f && System.Math.Abs(span.B.z - corridor.MaxZ) < .001f && span.Length > .2f)
                    boxes.Add(new DressingBox("CorridorWayfindingBand", (span.A + span.B) * .5f + Vector3.up * 1.15f - Vector3.forward * .06f,
                        new Vector3(span.Length - .1f, .08f, .012f), false, DressingStyle.Wayfinding));
            boxes.Add(new DressingBox("ContainmentStripe", new Vector3(exit.MaxX - .06f, 1.5f, exit.MaxZ - .35f), new Vector3(.012f, 1.1f, .25f), false, DressingStyle.Wayfinding));
            boxes.Add(new DressingBox("StoryTrace_Bedside", bed + Vector3.forward * .9f - Vector3.up * (bed.y - .008f), new Vector3(.25f, .006f, .4f), false, DressingStyle.Trace));
            var chain = (WardLayout.A4_Release.Position - WardLayout.Z4_Gurney.Position).normalized;
            for (int i = 1; i <= 4; i++)
                boxes.Add(new DressingBox("StoryTrace_Chain_" + i, WardLayout.Z4_Gurney.Position + chain * (WardLayout.FinalPatientReach * i / 4) + Vector3.up * .008f,
                    new Vector3(.1f, .006f, .1f), false, DressingStyle.Trace));
            boxes.Add(new DressingBox("ScuffedWall", new Vector3(corridor.MinX + .7f, .35f, corridor.MaxZ - .06f), new Vector3(.4f, .12f, .01f), false, DressingStyle.Damage));
            boxes.Add(new DressingBox("CeilingPanelRecess", WardLayout.A3_Mid1.Position + Vector3.up * 2.76f, new Vector3(.75f, .012f, 1.1f), false, DressingStyle.Damage));
            boxes.Add(new DressingBox("BrokenCeilingPanel", WardLayout.A3_Mid1.Position + Vector3.up * 2.58f, new Vector3(.6f, .035f, .85f), false, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("AbandonedBag", new Vector3(patient.MinX + .45f, .13f, patient.MinZ + 2.1f), new Vector3(.38f, .25f, .3f), true, DressingStyle.Fabric));
            boxes.Add(new DressingBox("UnfinishedChart", bed + Vector3.up * .2f + Vector3.forward * .4f, new Vector3(.25f, .015f, .32f), false, DressingStyle.Ceramic));
            foreach (var door in new RoomGeometry().Doors)
                for (int side = -1; side <= 1; side += 2)
                {
                    var along = door.AlongX ? Vector3.right : Vector3.forward;
                    var normal = door.AlongX ? Vector3.forward : Vector3.right;
                    boxes.Add(new DressingBox("DamagedDoorJamb", door.Centre + along * (side * (door.Width / 2 + .07f)) - normal * .065f + Vector3.up * 1.2f,
                        new Vector3(.035f, .65f, .035f), false, DressingStyle.Damage));
                }
            return boxes.ToArray();
        }
    }
}
