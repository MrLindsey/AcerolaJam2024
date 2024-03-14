//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class GameTool : EditorWindow
{
    Color _LightmapTintDefault = Color.white;

    [MenuItem("Tools/Game Tool")]
    public static void ShowTool()
    {
        EditorWindow wnd = GetWindow<GameTool>();
        wnd.titleContent = new GUIContent("Game Tool");
    }

    void OnGUI()
    {
        float space = 10;

        GUILayout.Space(space);
        GUILayout.Label("Lightmapping Tools", EditorStyles.boldLabel);
        GUILayout.Space(space);

        _LightmapTintDefault = EditorGUILayout.ColorField("Lightmap Tint", _LightmapTintDefault);

        if (GUILayout.Button("Set Global Shader Defaults"))
            SetGlobalShaders();

    }

    void SetGlobalShaders()
    {
        Shader.SetGlobalColor("_LightmapTint", _LightmapTintDefault);
    }
}

