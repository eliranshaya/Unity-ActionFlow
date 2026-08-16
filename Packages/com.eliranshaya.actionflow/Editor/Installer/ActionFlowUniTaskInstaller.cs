using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Core.ActionFlowInstaller
{
    /// <summary>
    /// Makes ActionFlow self-installing: the first time the package is imported into a project,
    /// it registers the OpenUPM scoped registry and adds UniTask to the project's manifest dependencies.
    ///
    /// This lives in its own assembly with no references, so it still compiles (and therefore still runs)
    /// while the ActionFlow runtime assembly is failing to compile because UniTask is not there yet.
    /// </summary>
    internal static class ActionFlowUniTaskInstaller
    {
        private const string PackageName = "com.cysharp.unitask";
        private const string PackageVersion = "2.5.10";
        private const string ScopeName = "com.cysharp";
        private const string RegistryName = "package.openupm.com";
        private const string RegistryUrl = "https://package.openupm.com";

        private const string SessionKey = "Core.ActionFlow.UniTaskInstallAttempted";

        private static AddRequest _request;

        [InitializeOnLoadMethod]
        private static void Bootstrap()
        {
            // Only attempt once per editor session, and never while the editor is busy compiling/importing.
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () => Install(false);
        }

        [MenuItem("Tools/ActionFlow/Install UniTask Dependency")]
        private static void InstallFromMenu()
        {
            Install(true);
        }

        private static void Install(bool verbose)
        {
            string manifestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
            if (!File.Exists(manifestPath))
            {
                if (verbose) Debug.LogError($"ActionFlow: could not find the project manifest at '{manifestPath}'.");
                return;
            }

            string manifest;
            try
            {
                manifest = File.ReadAllText(manifestPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"ActionFlow: failed to read the project manifest. {e.Message}");
                return;
            }

            // Declared by any means — registry version or a git URL — counts as installed.
            if (manifest.Contains($"\"{PackageName}\""))
            {
                if (verbose) Debug.Log("<color=cyan>ActionFlow:</color> UniTask is already declared in the project dependencies.");
                return;
            }

            string patched = EnsureScopedRegistry(manifest);
            if (!ReferenceEquals(patched, manifest))
            {
                try
                {
                    File.WriteAllText(manifestPath, patched);
                    Debug.Log($"<color=cyan>ActionFlow:</color> added the OpenUPM scoped registry ('{ScopeName}') to Packages/manifest.json.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"ActionFlow: failed to write the project manifest. {e.Message}");
                    return;
                }
            }

            Debug.Log($"<color=cyan>ActionFlow:</color> installing {PackageName}@{PackageVersion} — ActionFlow requires UniTask.");

            _request = Client.Add($"{PackageName}@{PackageVersion}");
            EditorApplication.update += PollRequest;
        }

        private static void PollRequest()
        {
            if (_request == null || !_request.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= PollRequest;

            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"<color=cyan>ActionFlow:</color> ✓ UniTask installed ({_request.Result.packageId}).");
            }
            else
            {
                Debug.LogError(
                    $"ActionFlow: failed to install UniTask automatically ({_request.Error?.message}). " +
                    $"Install it manually, then use Tools > ActionFlow > Install UniTask Dependency to retry.");
            }

            _request = null;
        }

        #region Manifest patching

        /// <summary>
        /// Returns the manifest with the OpenUPM scoped registry (scoped to <see cref="ScopeName"/>) present.
        /// Returns the exact same string instance when nothing had to change.
        /// </summary>
        private static string EnsureScopedRegistry(string manifest)
        {
            int registriesKey = manifest.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);
            if (registriesKey < 0)
            {
                int rootBrace = manifest.IndexOf('{');
                if (rootBrace < 0) return manifest;

                string block =
                    "\n  \"scopedRegistries\": [\n" +
                    "    {\n" +
                    $"      \"name\": \"{RegistryName}\",\n" +
                    $"      \"url\": \"{RegistryUrl}\",\n" +
                    "      \"scopes\": [\n" +
                    $"        \"{ScopeName}\"\n" +
                    "      ]\n" +
                    "    }\n" +
                    "  ],";

                return manifest.Insert(rootBrace + 1, block);
            }

            int arrayStart = manifest.IndexOf('[', registriesKey);
            if (arrayStart < 0) return manifest;

            int arrayEnd = FindMatching(manifest, arrayStart, '[', ']');
            if (arrayEnd < 0) return manifest;

            int registryUrlIndex = manifest.IndexOf(RegistryUrl, arrayStart, arrayEnd - arrayStart, StringComparison.Ordinal);
            if (registryUrlIndex < 0)
            {
                string entry =
                    "\n    {\n" +
                    $"      \"name\": \"{RegistryName}\",\n" +
                    $"      \"url\": \"{RegistryUrl}\",\n" +
                    "      \"scopes\": [\n" +
                    $"        \"{ScopeName}\"\n" +
                    "      ]\n" +
                    "    }";

                // A trailing comma is only legal when something follows it — manifest.json rejects them otherwise.
                entry += IsBlank(manifest, arrayStart + 1, arrayEnd) ? "\n  " : ",";

                return manifest.Insert(arrayStart + 1, entry);
            }

            // The registry exists — make sure our scope is listed on it.
            int objectStart = manifest.LastIndexOf('{', registryUrlIndex);
            if (objectStart < 0) return manifest;

            int objectEnd = FindMatching(manifest, objectStart, '{', '}');
            if (objectEnd < 0) return manifest;

            int scopesKey = manifest.IndexOf("\"scopes\"", objectStart, objectEnd - objectStart, StringComparison.Ordinal);
            if (scopesKey < 0) return manifest;

            int scopesStart = manifest.IndexOf('[', scopesKey);
            if (scopesStart < 0 || scopesStart > objectEnd) return manifest;

            int scopesEnd = FindMatching(manifest, scopesStart, '[', ']');
            if (scopesEnd < 0) return manifest;

            if (manifest.IndexOf($"\"{ScopeName}\"", scopesStart, scopesEnd - scopesStart, StringComparison.Ordinal) >= 0)
            {
                return manifest;
            }

            string scope = $"\n        \"{ScopeName}\"";
            scope += IsBlank(manifest, scopesStart + 1, scopesEnd) ? "\n      " : ",";

            return manifest.Insert(scopesStart + 1, scope);
        }

        private static bool IsBlank(string text, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (!char.IsWhiteSpace(text[i])) return false;
            }

            return true;
        }

        private static int FindMatching(string text, int start, char open, char close)
        {
            int depth = 0;
            bool inString = false;

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];

                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') inString = true;
                else if (c == open) depth++;
                else if (c == close && --depth == 0) return i;
            }

            return -1;
        }

        #endregion
    }
}
