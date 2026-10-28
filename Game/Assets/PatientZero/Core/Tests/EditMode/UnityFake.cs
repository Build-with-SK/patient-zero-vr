using UnityEngine;

namespace PatientZero.Core.Tests
{
    public interface IFake
    {
    }

    /// <summary>A Unity object service, used to test destroyed-object handling. Own file so Unity can bind the script.</summary>
    public sealed class UnityFake : ScriptableObject, IFake
    {
    }
}
