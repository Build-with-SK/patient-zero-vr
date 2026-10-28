namespace PatientZero.Interaction.Logic
{
    /// <summary>Distance and orientation acceptance shared by hand/controller socket adapters.</summary>
    public static class SocketLogic
    {
        public static bool CanSnap(float distanceMeters,float angleDegrees,bool assist) => distanceMeters>=0 && angleDegrees>=0 && distanceMeters<=(assist?0.12f:0.06f) && angleDegrees<=(assist?60f:35f);
    }
}
