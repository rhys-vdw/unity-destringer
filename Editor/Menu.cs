using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditorInternal;

namespace Destringer {
  static class Menu {
    [MenuItem("Tools/Destringer/Update animator wrappers for selection")]
    static void GenerateFromSelection() {
      var gameObjects = Selection.gameObjects;
      var animatorsByController = new Dictionary<RuntimeAnimatorController, List<Animator>>(gameObjects.Length);

      // Find controllers in selected game objects (scene objects and prefabs).
      for (int i = 0; i < gameObjects.Length; i++) {
        var gameObject = gameObjects[i];
        var animator = gameObject.GetComponent<Animator>();
        if (animator == null) {
          Debug.LogWarning($"No animator found on {gameObject}", gameObject);
        } else if (animator.runtimeAnimatorController == null) {
          Debug.LogWarning($"Animator has no {nameof(UnityEngine.RuntimeAnimatorController)}", animator);
        } else {
          var controller = animator.runtimeAnimatorController;
          if (animatorsByController.TryGetValue(controller, out var animators)) {
            animators.Add(animator);
          } else {
            animatorsByController[controller] = new List<Animator> { animator };
          }
        }
      }

      // Find any selected controller assets from the project.
      var assetGuids = Selection.assetGUIDs;
      for (int i = 0; i < assetGuids.Length; i++) {
        var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path) as RuntimeAnimatorController;
        if (controller != null) {
          if (!animatorsByController.TryGetValue(controller, out var animators)) {
            animatorsByController[controller] = new List<Animator>();
          }
        }
      }

      if (animatorsByController.Count == 0) {
        Debug.LogError($"Select {nameof(RuntimeAnimatorController)} assets or {nameof(GameObject)}s with animators before running this command.");
        return;
      }

      // Find or create wrappers as required.
      var allWrappers = AnimatorWrapper.LoadAll();
      var toGenerate = new List<AnimatorWrapper>();
      foreach (var kvp in animatorsByController) {
        var controller = kvp.Key;
        var animators = kvp.Value;
        var wrapper = allWrappers.Find(a => a.AnimatorController == controller);
        if (wrapper == null) {
          wrapper = AnimatorWrapper.Create(controller);
          wrapper.name = $"{controller.name}{nameof(AnimatorWrapper)}";
          var path = AnimatorWrapper.DefaultWrapperPath;
          var assetDirPath = $"Assets/{AnimatorWrapper.DefaultWrapperPath}";
          var absoluteDirPath = $"{Application.dataPath}/{path}";
          Directory.CreateDirectory(absoluteDirPath);
          var wrapperAssetPath = $"{assetDirPath}/{wrapper.name}.asset";
          AssetDatabase.CreateAsset(wrapper, wrapperAssetPath);
          Debug.Log($"Created {wrapperAssetPath}", wrapper);
        }
        toGenerate.Add(wrapper);
      }

      // Regenerate all assets.
      AnimatorWrapper.GenerateAndRefresh(toGenerate);

      // Attach the updated controllers to any animators that were selected.
      for (var i = 0; i < toGenerate.Count; i++) {
        var generated = toGenerate[i];
        var scriptName = generated.GeneratedScriptAsset.name;
        var animators = animatorsByController[generated.AnimatorController];
        for (var j = 0; j < animators.Count; j++) {
          var prevComponent = animators[j].gameObject.GetComponent(scriptName);
          if (prevComponent == null) {
            AddScriptComponent(animators[j].gameObject, generated.GeneratedScriptAsset);
          }
        }
      }
    }

    /// <remarks>
    /// https://github.com/Unity-Technologies/UnityCsReference/blob/11aeafbc7359dee968c6156b688b056d215dfd81/Editor/Mono/Inspector/AddComponent/NewScriptDropdownItem.cs#L72
    /// </remarks>
    static void AddScriptComponent(GameObject gameObject, MonoScript scriptComponent) {
      var AddScriptComponentUncheckedUndoableMethod = typeof(InternalEditorUtility).GetMethod(
        "AddScriptComponentUncheckedUndoable",
        BindingFlags.Static | BindingFlags.NonPublic
      );
      AddScriptComponentUncheckedUndoableMethod.Invoke(
        null,
        new object [] { gameObject, scriptComponent }
      );
    }
  }
}