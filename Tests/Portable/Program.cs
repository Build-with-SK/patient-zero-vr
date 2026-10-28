using System;
using System.Diagnostics;
using PatientZero.Core;
using PatientZero.Tests;

int failures = 0;
foreach (var test in CoreScenarios.All)
{
    try { test.Run(); Console.WriteLine("PASS " + test.Name); }
    catch (Exception e) { failures++; Console.WriteLine("FAIL " + test.Name + ": " + e.Message); }
}
Console.WriteLine($"{CoreScenarios.All.Length - failures} passed; {failures} failed; 0 skipped");
// Host throughput evidence only. This does not model NavMesh, physics, rendering, or Quest hardware.
var brains = new ZombieBrain[12];
for (int i = 0; i < brains.Length; i++) brains[i] = new ZombieBrain(new ZombieSettings());
var clock = Stopwatch.StartNew();
for (int tick = 0; tick < 100000; tick++)
    for (int i = 0; i < brains.Length; i++) brains[i].Tick(tick * .1, new WorldPoint());
clock.Stop();
Console.WriteLine($"Host-only idle core benchmark: 1,200,000 ticks in {clock.Elapsed.TotalMilliseconds:F2} ms. Not a Quest frame-rate result.");
return failures == 0 ? 0 : 1;
