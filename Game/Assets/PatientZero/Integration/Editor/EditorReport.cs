// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// A plain-text report shared by the composer, validator and build script.
    /// Every line is also logged with a "[PZ]" prefix so tools/unity/unity_batch.py can read results from the Unity log.
    /// </summary>
    public sealed class EditorReport
    {
        private readonly string _name;
        private readonly StringBuilder _text = new StringBuilder();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public EditorReport(string name)
        {
            _name = name;
            Line("# PatientZero " + name + " — Unity " + Application.unityVersion + " — " +
                 DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        }

        public bool Ok => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors;

        public void Line(string text)
        {
            _text.AppendLine(text);
            Debug.Log("[PZ] " + text);
        }

        public void Pass(string what) => Line("PASS  " + what);

        public void Error(string what)
        {
            _errors.Add(what);
            _text.AppendLine("FAIL  " + what);
            Debug.LogError("[PZ] FAIL  " + what);
        }

        public void Warn(string what)
        {
            _warnings.Add(what);
            _text.AppendLine("WARN  " + what);
            Debug.LogWarning("[PZ] WARN  " + what);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            foreach (string e in errors) Error(e);
        }

        /// <summary>Writes Game/Logs/PatientZero/&lt;name&gt;.txt (ignored by git) and returns the path.</summary>
        public string Finish()
        {
            Line("RESULT " + (Ok ? "PASS" : "FAIL") + " — " + _errors.Count + " error(s), " + _warnings.Count + " warning(s)");
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "PatientZero"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, _name + ".txt");
            File.WriteAllText(path, _text.ToString(), new UTF8Encoding(false));
            Debug.Log("[PZ] report written: " + path);
            return path;
        }
    }
}
