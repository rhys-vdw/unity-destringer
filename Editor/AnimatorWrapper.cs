using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Destringer {
  [CreateAssetMenu(fileName = "AnimatorWrapper", menuName = "Destringer/AnimatorWrapper")]
  public class AnimatorWrapper : ScriptableObject {
    public static readonly string DefaultScriptPath = "AnimatorWrapper/Generated/Scripts";
    public static readonly string DefaultWrapperPath = "AnimatorWrapper/Generated/AnimationWrappers";

    public MonoScript GeneratedScriptAsset;
    public RuntimeAnimatorController AnimatorController;
    public AccessModifier AccessModifier = AccessModifier.Public;
    public string ClassName = null;
    public string Namespace = null;
    public string GeneratedScriptPath = DefaultScriptPath;
    public bool IsPartial = false;

    public static AnimatorWrapper Create(RuntimeAnimatorController controller) {
      var wrapper = ScriptableObject.CreateInstance<AnimatorWrapper>();
      wrapper.AnimatorController = controller;
      return wrapper;
    }

    string Generate() {
      if (AnimatorController == null) {
        Debug.LogWarning(
          $"{name} has no {nameof(RuntimeAnimatorController)} assigned. Skipping.",
          this
        );
        return null;
      }

      // Use specified class name or generate one from the controller.
      var className = string.IsNullOrWhiteSpace(ClassName)
        ? PascalCase(AnimatorController.name)
        : ClassName.Trim();

      var generated = Generator.GenerateFromController(
        AnimatorController,
        Namespace,
        className,
        AccessModifier,
        IsPartial
      );

      // A MonoBehaviour's filename must match its filename. Partial classes
      // do not have this issue.
      var suffix = IsPartial ? ".Generated" : "";
      var assetName = $"{className}{suffix}";
      var filename = $"{assetName}.cs";

      // Check if we already have a script.
      string scriptAssetPath;
      if (GeneratedScriptAsset != null) {
        // Rename the asset if its previous name doesn't match.
        if (GeneratedScriptAsset.name != assetName) {
          var path = AssetDatabase.GetAssetPath(GeneratedScriptAsset);
          AssetDatabase.RenameAsset(path, assetName);
        }
        scriptAssetPath = AssetDatabase.GetAssetPath(GeneratedScriptAsset);
      } else {
        // If not, decide where it will go.
        scriptAssetPath = $"Assets/{GeneratedScriptPath}/{filename}";
      }

      // Directory and asset paths.
      var scriptPath = scriptAssetPath.Substring("Assets/".Length);
      var absoluteFilePath = $"{Application.dataPath}/{scriptPath}";
      var absoluteDirPath = Path.GetDirectoryName(absoluteFilePath);

      // Ensure that the directory exists.
      Directory.CreateDirectory(absoluteDirPath);

      // Write the file.
      using (var stream = File.Create(absoluteFilePath)) {
        var csData = new UTF8Encoding(true).GetBytes(generated);
        stream.Write(csData, 0, csData.Length);
      }

      return scriptAssetPath;
    }

    public static void GenerateAndRefresh(IEnumerable<AnimatorWrapper> wrappers) {
      var generated = new List<(AnimatorWrapper, string)>();
      foreach (var wrapper in wrappers) {
        var scriptAssetPath = wrapper.Generate();
        if (scriptAssetPath != null) {
          generated.Add((wrapper, scriptAssetPath));
        }
      }
      AssetDatabase.Refresh();
      foreach (var (wrapper, path) in generated) {
        var scriptAsset = (MonoScript) AssetDatabase.LoadAssetAtPath(path, typeof(MonoScript));
        if (scriptAsset == null) {
          Debug.LogError($"Could not find generated file {path}!");
        } else {
          Debug.Log(
            wrapper.GeneratedScriptAsset == null
              ? $"Created '{path}'"
              : $"Regenerated '{path}'"
          , scriptAsset);
        }
        wrapper.GeneratedScriptAsset = scriptAsset;
        EditorUtility.SetDirty(wrapper);
      }
    }

    public static List<AnimatorWrapper> LoadAll() {
      var result = new List<AnimatorWrapper>();
      var guids = AssetDatabase.FindAssets($"t:{nameof(AnimatorWrapper)}");
      foreach (var guid in guids) {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var wrapper = (AnimatorWrapper) AssetDatabase.LoadAssetAtPath(path, typeof(AnimatorWrapper));
        if (wrapper != null) {
          result.Add(wrapper);
        }
      }
      return result;
    }

    // see: https://stackoverflow.com/a/55615973
    static string PascalCase(string str) {
      TextInfo cultInfo = new CultureInfo("en-US", false).TextInfo;
      str = Regex.Replace(str, "([A-Z]+)", " $1");
      str = cultInfo.ToTitleCase(str);
      str = str.Replace(" ", "");
      return str;
    }
  }
}