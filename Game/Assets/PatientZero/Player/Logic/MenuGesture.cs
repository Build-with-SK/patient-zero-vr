namespace PatientZero.Player.Logic
{
    /// <summary>Debounced palm-up pinch from Unity XR Hands poses; menu adapter calls once per sample.</summary>
    public sealed class MenuGesture
    {
        private bool latched;
        public bool Sample(bool palmUp,bool pinch,bool tracked){bool pressed=tracked && palmUp && pinch;bool fire=pressed && !latched;latched=pressed;return fire;}
    }
}
