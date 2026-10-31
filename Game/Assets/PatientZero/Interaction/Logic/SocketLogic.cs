namespace PatientZero.Interaction.Logic
{
    /// <summary>Distance and orientation acceptance shared by hand/controller socket adapters.</summary>
    public static class SocketLogic
    {
        /// <summary>E-19: on a flat screen a held prop cannot be turned or pushed into a socket, so near enough plugs it.</summary>
        public const float FlatScreenSnapMeters=0.25f;
        public static bool CanSnapFlat(float distanceMeters) => distanceMeters>=0 && distanceMeters<=FlatScreenSnapMeters;
        public static bool CanSnap(float distanceMeters,float angleDegrees,bool assist) => distanceMeters>=0 && angleDegrees>=0 && distanceMeters<=(assist?0.12f:0.06f) && angleDegrees<=(assist?60f:35f);
    }
}
