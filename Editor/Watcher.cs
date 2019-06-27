using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ModificationProcessor = UnityEditor.AssetModificationProcessor;

namespace Destringer {
  class Watcher : ModificationProcessor {
    static void OnWillSaveAssets(string[] paths) {
      HashSet<AnimatorWrapper> dirty = new HashSet<AnimatorWrapper>();

      // Search for wrappers to update.
      List<AnimatorWrapper> wrappers = null;
      foreach (string path in paths) {
        var controller = (RuntimeAnimatorController) AssetDatabase.LoadAssetAtPath(path, typeof(RuntimeAnimatorController));

        // Has a controller been reimporter?
        if (controller != null) {

          // Lazily initialize.
          if (wrappers == null) {
            wrappers = AnimatorWrapper.LoadAll();
          }

          // Record all the wrappers that need to be reimported.
          foreach (var wrapper in wrappers) {
            if (wrapper.AnimatorController == controller) dirty.Add(wrapper);
          }

          continue;
        }

        var dirtyWrapper = (AnimatorWrapper) AssetDatabase.LoadAssetAtPath(path, typeof(AnimatorWrapper));
        if (dirtyWrapper != null) {
          // Record all the wrappers that need to be reimported.
          dirty.Add(dirtyWrapper);
          continue;
        }
      }

      if (dirty.Count > 0) {
        AnimatorWrapper.GenerateAndRefresh(dirty);
      }
    }
  }
}

