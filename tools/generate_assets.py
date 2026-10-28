#!/usr/bin/env python3
"""Rebuild our original template assets; Unity still must import/validate them.

No Meta assets or SDK source are copied. Stable UUIDs keep script references intact.
"""
import hashlib
import math
from pathlib import Path
import random
import struct
import uuid
import wave

ROOT = Path(__file__).resolve().parents[1] / "Packages/com.patientzero.xr"
NAMESPACE = uuid.UUID("c8f3ace1-7c48-44c4-a47c-f1cc2c44101f")
HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"


def guid(path):
    return uuid.uuid5(NAMESPACE, str(path)).hex


def write(path, data):
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text('\n'.join(line.rstrip() for line in data.splitlines()) + '\n')


def ref(path, file_id=11500000):
    return "{fileID: " + str(file_id) + ", guid: " + guid(path) + ", type: 3}"


def mono(file_id, script, fields=""):
    return f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {ref('Runtime/Unity/' + script + '.cs')}
  m_Name:
  m_EditorClassIdentifier:
{fields}"""


def prefab(name, components):
    ids = [4000] + [file_id for file_id, _ in components]
    data = HEADER + """--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
"""
    data += "".join(f"  - component: {{fileID: {i}}}\n" for i in ids)
    data += f"""  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &4000
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""
    data += "".join(contents for _, contents in components)
    write("Prefabs/" + name + ".prefab", data)


def generate_mixer():
    data = HEADER + """--- !u!241 &24100000
AudioMixerController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: PatientZero
  m_OutputGroup: {fileID: 0}
  m_MasterGroup: {fileID: 24300000}
  m_Snapshots:
  - {fileID: 24500000}
  m_StartSnapshot: {fileID: 24500000}
  m_SuspendThreshold: -80
  m_EnableSuspend: 1
  m_UpdateMode: 0
  m_ExposedParameters: []
  m_AudioMixerGroupViews:
  - guids:
"""
    groups = ["Master", "Zombie", "Ambience", "Encounters"]
    data += "".join(f"    - {guid('mixer/group/' + g)}\n" for g in groups)
    data += "    name: View\n  m_CurrentViewIndex: 0\n  m_TargetSnapshot: {fileID: 24500000}\n"
    for index, name in enumerate(groups):
        group_id, effect_id = 24300000 + index, 24400000 + index
        children = "[]\n" if index else "\n" + "".join(f"  - {{fileID: {24300000 + i}}}\n" for i in range(1, 4))
        data += f"""--- !u!243 &{group_id}
AudioMixerGroupController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_AudioMixer: {{fileID: 24100000}}
  m_GroupID: {guid('mixer/group/' + name)}
  m_Children: {children}  m_Volume: {guid('mixer/volume/' + name)}
  m_Pitch: {guid('mixer/pitch/' + name)}
  m_Send: 00000000000000000000000000000000
  m_Effects:
  - {{fileID: {effect_id}}}
  m_UserColorIndex: 0
  m_Mute: 0
  m_Solo: 0
  m_BypassEffects: 0
--- !u!244 &{effect_id}
AudioMixerEffectController:
  m_ObjectHideFlags: 3
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name:
  m_EffectID: {guid('mixer/effect/' + name)}
  m_EffectName: Attenuation
  m_MixLevel: {guid('mixer/mix/' + name)}
  m_Parameters: []
  m_SendTarget: {{fileID: 0}}
  m_EnableWetMix: 0
  m_Bypass: 0
"""
    data += f"""--- !u!245 &24500000
AudioMixerSnapshotController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Default
  m_AudioMixer: {{fileID: 24100000}}
  m_SnapshotID: {guid('mixer/snapshot')}
  m_FloatValues: {{}}
  m_TransitionOverrides: {{}}
"""
    write("Audio/PatientZero.mixer", data)


def generate_audio():
    # Quiet, original mono diagnostic placeholders. Replace for production after headphone review.
    rng = random.Random(207)
    cues = ["FootLeft", "FootRight", "Breathing", "Scratching", "Alarm", "Growl"]
    durations = [.25, .25, 2, .8, 3, 1.2]
    rate = 22050
    for cue, duration in zip(cues, durations):
        samples = []
        for i in range(int(duration * rate)):
            t = i / rate
            fade = min(1, t / .03, (duration - t) / .06)
            noise = rng.uniform(-1, 1)
            if cue.startswith("Foot"):
                value = (.65 * math.sin(2 * math.pi * (82 if cue == 'FootLeft' else 95) * t) + .35 * noise) * math.exp(-20 * t)
            elif cue == "Breathing":
                value = noise * .3 * math.sin(math.pi * t / duration) ** 2
            elif cue == "Scratching":
                value = noise * .4 * (math.sin(t * 35) ** 8)
            elif cue == "Alarm":
                value = .3 * math.sin(2 * math.pi * (650 * t + 35 * math.sin(t * 3)))
            else:
                value = .3 * math.sin(2 * math.pi * 65 * t) * (1 + .2 * math.sin(t * 22))
            samples.append(struct.pack('<h', int(max(-1, min(1, value * fade)) * 6000)))
        target = ROOT / "Audio/Diagnostic" / (cue + ".wav")
        target.parent.mkdir(parents=True, exist_ok=True)
        with wave.open(str(target), 'wb') as wav:
            wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(rate); wav.writeframes(b''.join(samples))
    data = HEADER + f"""--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {ref('Runtime/Unity/AudioCueBank.cs')}
  m_Name: DiagnosticCueBank
  m_EditorClassIdentifier:
  Entries:
"""
    for i, cue in enumerate(cues):
        group_id = 24300002 if cue == "Alarm" else 24300003 if cue in ["Breathing", "Scratching"] else 24300001
        data += f"""  - Cue: {i}
    Clip: {ref('Audio/Diagnostic/' + cue + '.wav', 8300000)}
    Group: {ref('Audio/PatientZero.mixer', group_id)}
    Volume: 0.65
    MinDistance: 1
    MaxDistance: {60 if cue == 'Alarm' else 25}
    Cooldown: {0.1 if cue.startswith('Foot') else 1}
    ComfortSensitive: 1
"""
    write("Audio/DiagnosticCueBank.asset", data)


def generate_prefabs():
    prefab("PZ_NoiseManager", [(1140, mono(1140, "NoiseManager", "  MaxActiveZombies: 12\n  MaxNoiseSources: 128\n  SourceCooldown: 0.15\n"))])
    nav = """--- !u!195 &1950
NavMeshAgent:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1000}
  m_Enabled: 1
  m_AgentTypeID: 0
  m_Radius: 0.3
  m_Speed: 1.2
  m_Acceleration: 8
  avoidancePriority: 50
  m_AngularSpeed: 120
  m_StoppingDistance: 0.5
  m_AutoTraverseOffMeshLink: 1
  m_AutoBraking: 1
  m_AutoRepath: 1
  m_Height: 1.8
  m_BaseOffset: 0
  m_WalkableMask: 4294967295
  m_ObstacleAvoidanceType: 2
"""
    prefab("PZ_ZombieAgent", [
        (1950, nav),
        (1140, mono(1140, "ZombieNavigation", "  WalkSpeed: 1.2\n  ChaseSpeed: 2.4\n  RepathSeconds: 0.35\n")),
        (1141, mono(1141, "ZombieStateMachine", """  Settings:
    HearingThreshold: 0.12
    ChaseThreshold: 0.6
    ArrivalDistance: 0.6
    AttackDistance: 1.3
    InvestigateSeconds: 7
    SearchSeconds: 4
    HearingMemorySeconds: 2
    AttackWindupSeconds: 0.7
    RecoverSeconds: 1.8
  TickHz: 10
  PlayerMask:
    serializedVersion: 2
    m_Bits: 0
  AttackBlockers:
    serializedVersion: 2
    m_Bits: 0
  Damage: 10
  OnStateChanged:
    m_PersistentCalls:
      m_Calls: []
""")),
        (1142, mono(1142, "ZombieHearingSensor", "  Manager: {fileID: 0}\n")),
        (1143, mono(1143, "ZombieAnimationController", "  Animator: {fileID: 0}\n  Audio: {fileID: 0}\n  LeftFoot: {fileID: 0}\n  RightFoot: {fileID: 0}\n  Mouth: {fileID: 0}\n"))])
    prefab("PZ_SpatialAudio", [(1140, mono(1140, "SpatialAudioSystem", f"""  Bank: {ref('Audio/DiagnosticCueBank.asset', 11400000)}
  Listener: {{fileID: 0}}
  Mixer: {ref('Audio/PatientZero.mixer', 24100000)}
  Occluders:
    serializedVersion: 2
    m_Bits: 0
  MasterVolume: 0.8
  ComfortVolume: 0.65
  VoiceCount: 12
  OcclusionInterval: 0.15
  OccludedGain: 0.4
"""))])
    encounters = [
        ("BehindGlass", [3, 5]), ("ThrownDistraction", [5, 0]), ("OperatingRoom", [0, 5]),
        ("DarkCorridor", [3, 2]), ("InfectedReveal", [2, 5])]
    for kind, (name, cues) in enumerate(encounters):
        fields = f"""  Kind: {kind}
  Listener: {{fileID: 0}}
  Audio: {{fileID: 0}}
  MinimumDistance: 2.5
  AllowIntenseEncounters: 0
  OneShot: 1
  Beats:
"""
        for i, cue in enumerate(cues):
            fields += f"""  - Delay: {1 if i == 0 else 2}
    Cue: {cue}
    AudioAnchor: {{fileID: 4000}}
    PlayAudio: 1
    OnBeat:
      m_PersistentCalls:
        m_Calls: []
"""
        fields += "  OnComplete:\n    m_PersistentCalls:\n      m_Calls: []\n"
        components = [(1140, mono(1140, "EncounterSequence", fields))]
        if name == "ThrownDistraction":
            components.append((1141, mono(1141, "NoiseEncounterTrigger", "  Manager: {fileID: 0}\n  Category: 0\n  HearingThreshold: 0.15\n")))
        prefab("PZ_Encounter_" + name, components)


def metadata():
    for path in sorted(ROOT.rglob('*')):
        if path.name.endswith('.meta') or '~' in str(path.relative_to(ROOT)):
            continue
        relative = path.relative_to(ROOT).as_posix()
        target = Path(str(path) + '.meta')
        if target.exists():
            continue
        base = f"fileFormatVersion: 2\nguid: {guid(relative)}\n"
        if path.is_dir():
            base += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        elif path.suffix == '.cs':
            base += "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        elif path.suffix == '.asmdef':
            base += "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        elif path.suffix == '.prefab':
            base += "PrefabImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        elif path.suffix == '.wav':
            base += "AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 22050\n    compressionFormat: 1\n    quality: 0.7\n    conversionMode: 0\n    preloadAudioData: 1\n  platformSettingOverrides: {}\n  forceToMono: 1\n  normalize: 0\n  ambisonic: 0\n  loadInBackground: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        elif path.suffix in ['.asset', '.mixer']:
            main_id = 24100000 if path.suffix == '.mixer' else 11400000
            base += f"NativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: {main_id}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        else:
            base += "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
        target.write_text('\n'.join(line.rstrip() for line in base.splitlines()) + '\n')


if __name__ == '__main__':
    generate_mixer()
    generate_audio()
    generate_prefabs()
    metadata()
    print("Generated 8 template prefabs, diagnostic audio bank, mixer and stable package metadata.")
