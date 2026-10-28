using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR;

namespace PatientZero.XR
{
    // Explicit capture only. Exported on-device data is required before claiming a Quest frame rate.
    public sealed class QuestPerformanceRecorder : MonoBehaviour
    {
        public NoiseManager Noise;
        [Range(72, 60000)] public int MaxFrames = 30000;
        private struct Sample { public double FrameMs, CpuMs; public long Allocated; public int Zombies; }
        private Sample[] samples;
        private ProfilerRecorder cpu, gc;
        private int count;
        private bool capturing;
        public string LastCapturePath { get; private set; }
        private void Awake() => samples = new Sample[Mathf.Clamp(MaxFrames, 72, 60000)];
        public void BeginCapture()
        {
            if (capturing) return;
            cpu = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            count = 0; capturing = true;
        }
        private void Update()
        {
            if (!capturing) return;
            if (count == samples.Length) { EndCapture(); return; }
            samples[count++] = new Sample
            {
                FrameMs = Time.unscaledDeltaTime * 1000,
                CpuMs = cpu.Valid ? cpu.LastValue / 1000000.0 : -1,
                Allocated = gc.Valid ? gc.LastValue : -1,
                Zombies = Noise ? Noise.ActiveZombies : 0
            };
        }
        public void EndCapture()
        {
            if (!capturing) return;
            capturing = false; cpu.Dispose(); gc.Dispose();
            var text = new StringBuilder(count * 40);
            text.AppendLine("# device=" + SystemInfo.deviceModel + "; platform=" + Application.platform +
                "; unity=" + Application.unityVersion + "; xrActive=" + XRSettings.isDeviceActive);
            text.AppendLine("frame,wall_frame_ms,main_thread_ms,gc_bytes,active_zombies");
            for (int i = 0; i < count; i++)
            {
                var s = samples[i];
                text.Append(i).Append(',').Append(s.FrameMs.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                    .Append(s.CpuMs.ToString("F4", CultureInfo.InvariantCulture)).Append(',')
                    .Append(s.Allocated).Append(',').Append(s.Zombies).AppendLine();
            }
            LastCapturePath = Path.Combine(Application.persistentDataPath, "pz-profile-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".csv");
            File.WriteAllText(LastCapturePath, text.ToString());
            Debug.Log("Patient Zero performance capture: " + LastCapturePath + ". Missing counters are -1; GPU results require the Quest profiler.", this);
        }
        private void OnDisable() => EndCapture();
    }
}
