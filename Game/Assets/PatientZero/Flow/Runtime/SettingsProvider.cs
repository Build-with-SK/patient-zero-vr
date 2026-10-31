// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Flow.Logic;
using UnityEngine;
namespace PatientZero.Flow
{
    /// <summary>Bootstrap-owned persistence and settings broadcast; menu calls Apply/ConfirmCalibration.</summary>
    public sealed class SettingsProvider : MonoBehaviour,ISettingsProvider
    {
        private const string Key="PatientZero.Settings.v1",ConfirmedKey=CalibrationKey;
        /// <summary>PlayerPrefs key that skips the calibration screen once confirmed. Public for tooling and tests (integration E-13).</summary>
        public const string CalibrationKey="PatientZero.Calibrated.v1";
        /// <summary>The next run starts at the calibration screen (integration E-13).</summary>
        public static void ResetCalibration(){PlayerPrefs.DeleteKey(CalibrationKey);PlayerPrefs.Save();}
        public ExperienceSettings Current {get;private set;}=ExperienceSettings.Defaults;
        public bool CalibrationConfirmed => PlayerPrefs.GetInt(ConfirmedKey,0)==1;
        private void OnEnable(){Current=SettingsCodec.Decode(PlayerPrefs.GetString(Key,""));if(!Services<ISettingsProvider>.Register(this)){enabled=false;return;}GameEvents.SettingsChanged.Raise(new SettingsChanged(Current));}
        private void OnDisable(){Services<ISettingsProvider>.Unregister(this);}
        public void Apply(ExperienceSettings settings){Current=settings.Sanitized();PlayerPrefs.SetString(Key,SettingsCodec.Encode(Current));PlayerPrefs.Save();GameEvents.SettingsChanged.Raise(new SettingsChanged(Current));}
        public void ConfirmCalibration(){PlayerPrefs.SetInt(ConfirmedKey,1);PlayerPrefs.Save();}
    }
}
