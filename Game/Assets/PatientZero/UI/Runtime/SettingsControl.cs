// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using System.Reflection;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.UI;
namespace PatientZero.UI
{
    /// <summary>Cold-path menu binding to an existing ExperienceSettings field; never a second settings model.</summary>
    public sealed class SettingsControl : MonoBehaviour
    {
        public string fieldName;public Toggle toggle;public Slider slider;private FieldInfo field;
        private void Awake(){field=typeof(ExperienceSettings).GetField(fieldName,BindingFlags.Public|BindingFlags.Instance);}
        private void OnEnable(){GameEvents.SettingsChanged.Subscribe(Changed);if(Services<ISettingsProvider>.TryGet(out var s))Refresh(s.Current);}
        private void OnDisable(){GameEvents.SettingsChanged.Unsubscribe(Changed);}
        private void Changed(SettingsChanged s){Refresh(s.Settings);}
        private void Refresh(ExperienceSettings settings){if(field==null)return;object value=field.GetValue(settings);if(toggle){bool selected=value is bool b?b:fieldName==nameof(ExperienceSettings.DominantHand)?(HandSide)value==HandSide.Left:fieldName==nameof(ExperienceSettings.Scares)?(ScareIntensity)value==ScareIntensity.Reduced:fieldName==nameof(ExperienceSettings.Turn)?(TurnMode)value==TurnMode.Smooth:(LocomotionMode)value==LocomotionMode.TeleportAndSmooth;toggle.SetIsOnWithoutNotify(selected);}if(slider)slider.SetValueWithoutNotify(System.Convert.ToSingle(value));}
    }
}
