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

[System.Serializable]
public class TaskDef
{
    public enum Type
    {
        FromScript,
        GrabObject,
        ReachedTrigger,
        WaitForChatToFinish
    }

    public string _name;
    public string _taskDescription;
    public Type _type;
    public string _objectName;
}
