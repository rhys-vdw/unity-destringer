using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Destringer {
  [CustomEditor(typeof(AnimatorWrapper), true), CanEditMultipleObjects]
  public class AnimatorWrapperEditor : Editor {
    static List<AnimatorWrapper> _wrapperCache = new List<AnimatorWrapper>();
    public override void OnInspectorGUI() {
      base.DrawDefaultInspector();

      _wrapperCache.Clear();
      var missingCount = 0;
      foreach (var selected in Selection.objects) {
        var wrapper = selected as AnimatorWrapper;
        if (wrapper != null) {
          _wrapperCache.Add(wrapper);
          if (wrapper.AnimatorController == null) {
            missingCount++;
          }
        }
      }

      if (missingCount > 0) {
        EditorGUILayout.HelpBox(
          _wrapperCache.Count == 1
            ? $"Assign a {nameof(RuntimeAnimatorController)}"
            : $"Missing {missingCount} {nameof(RuntimeAnimatorController)}(s)",
          MessageType.Warning
        );
      }

      EditorGUI.BeginDisabledGroup(missingCount == _wrapperCache.Count);

      if (GUILayout.Button("Generate")) {
        AnimatorWrapper.GenerateAndRefresh(_wrapperCache);
      }

      EditorGUI.EndDisabledGroup();
    }
  }
}
