using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditorInternal;

namespace Destringer {
  static class Menu {
    [MenuItem("Tools/Destringer/Attach animator wrappers")]
    static void GenerateFromSelection() {
      var gameObjects = Selection.gameObjects;
      var animators = new List<Animator>(gameObjects.Length);
      for (int i = 0; i < gameObjects.Length; i++) {
        var gameObject = gameObjects[i];
        var animator = gameObject.GetComponent<Animator>();
        if (animator == null) {
          Debug.LogWarning($"No animator found on {gameObject}", gameObject);
        } else if (animator.runtimeAnimatorController == null) {
          Debug.LogWarning($"Animator has no {nameof(UnityEngine.RuntimeAnimatorController)}", animator);
        } {
          animators.Add(animator);
        }
      }
      if (animators.Count == 0) {
        Debug.LogError($"No {typeof(RuntimeAnimatorController)}s selected");
        return;
      }
      var allWrappers = AnimatorWrapper.LoadAll();
      var toGenerate = new List<AnimatorWrapper>();
      foreach (var animator in animators) {
        var wrapper = allWrappers.Find(a => a.AnimatorController == animator);
        if (wrapper == null) {
          var controller = animator.runtimeAnimatorController;
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
      AnimatorWrapper.GenerateAndRefresh(toGenerate);
      for (int i = 0; i < animators.Count; i++) {
        var scriptName = toGenerate[i].GeneratedScriptAsset.name;
        var prevComponent = animators[i].gameObject.GetComponent(scriptName);
        if (prevComponent == null) {
          AddScriptComponent(animators[i].gameObject, toGenerate[i].GeneratedScriptAsset);
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