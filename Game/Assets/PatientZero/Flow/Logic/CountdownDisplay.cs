using System;
namespace PatientZero.Flow.Logic
{
    /// <summary>UI projection of the B-owned experience clock; reaching zero never fails the run.</summary>
    public static class CountdownDisplay
    {
        public static int Remaining(float experienceSeconds) => float.IsNaN(experienceSeconds)?120:(int)Math.Ceiling(Math.Max(0,120-Math.Max(0,experienceSeconds)));
    }
}
