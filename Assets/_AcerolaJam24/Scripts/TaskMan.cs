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
using UnityEngine.Events;

public class TaskMan : MonoBehaviour
{
    public class ActiveTask
    {
        public TaskDef _def;
        public int _index;
        public bool _isComplete;
    }

    [SerializeField] private CharacterMan _characterMan;
    [SerializeField] private TriggerLogic[] _triggers;

    public UnityEvent _OnTaskStarted;
    public UnityEvent _OnTaskComplete;

    // For Debugging
#if UNITY_EDITOR
    public List<string> _activeTasksDebug = new List<string>();
#endif

    private TaskListDef _curTaskList;
    private List<ActiveTask> _activeTasks = new List<ActiveTask>();
    private int _taskCompleteCount = 0;
    private bool _allTasksCompleted = false;

    private TaskDef _lastCompletedTask;
    private TaskDef _lastStartedTask;

    public TaskListDef GetCurrentTaskList()
    {
        return _curTaskList;
    }

    public TaskDef GetLastCompletedTask()
    {
        return _lastCompletedTask;
    }

    public TaskDef GetLastStartedTask()
    {
        return _lastStartedTask;
    }

    public bool HasCompletedTaskList()
    {
        return _allTasksCompleted;
    }

    public bool PlayerEnteredTrigger(Transform triggerObj)
    {
        return CheckObjectTask(triggerObj, TaskDef.Type.ReachedTrigger);
    }

    public bool PlayerGrabbedObject(Transform triggerObj)
    {
        return CheckObjectTask(triggerObj, TaskDef.Type.GrabObject);
    }

    public bool CompleteWaitForChat()
    {
        ActiveTask task = GetActiveTask(TaskDef.Type.WaitForChatToFinish);
        if (task != null)
        {
            TaskComplete(task._index);
            return true;
        }
        return false;
    }

    public bool CompleteFromScript(string taskObjectName)
    {
        ActiveTask task = GetActiveTask(TaskDef.Type.FromScript);
        if (task != null)
        {
            if (taskObjectName == task._def._objectName)
            {
                TaskComplete(task._index);
                return true;
            }
        }
        return false;
    }

    bool CheckObjectTask(Transform checkObj, TaskDef.Type type)
    {
        ActiveTask task = GetActiveTask(type);
        if (task != null)
        {
            if (checkObj.name == task._def._objectName)
            {
                TaskComplete(task._index);
                return true;
            }
        }
        return false;
    }

    public void StartTaskList(TaskListDef taskList)
    {
        _curTaskList = taskList;
        _activeTasks.Clear();

#if UNITY_EDITOR
        _activeTasksDebug.Clear();
#endif

        for (int i = 0; i < taskList._tasks.Length; ++i)
        {
            ActiveTask newTask = new ActiveTask();
            newTask._def = taskList._tasks[i];
            newTask._index = i;
            newTask._isComplete = false;

            _activeTasks.Add(newTask);

#if UNITY_EDITOR
            _activeTasksDebug.Add(newTask._def._name + " [ACTIVE]");
#endif
        }
        _taskCompleteCount = 0;
        _allTasksCompleted = false;

        // If we don't have any tasks then set it to completed at the start
        if (_curTaskList._tasks.Length == 0)
            _allTasksCompleted = true;

        // Start all the tasks in the list (they can be done in any order)
        foreach (TaskDef task in _curTaskList._tasks)
            StartTask(task);
    }

    public void CompletedTaskListAndChat()
    {
       
    }

    public void StartTask(TaskDef task)
    {
        // Do we need to activate a trigger?
        if (task._type == TaskDef.Type.ReachedTrigger)
        {
            foreach (TriggerLogic trigger in _triggers)
                if (trigger.name == task._objectName)
                    trigger.gameObject.SetActive(true);
        }

        _lastStartedTask = task;
        Debug.Log("Start Task: " + task._name);

        if (_OnTaskStarted != null)
            _OnTaskStarted.Invoke();
    }

    public bool IsTaskComplete(int index)
    {
        return _activeTasks[index]._isComplete;
    }

    public void TaskComplete(int index)
    {
        if (_activeTasks[index]._isComplete == false)
        {
            _activeTasks[index]._isComplete = true;

#if UNITY_EDITOR
            _activeTasksDebug[index] = _activeTasksDebug[index].Replace("[ACTIVE]", "[COMPLETE]");
#endif

            _lastCompletedTask = _activeTasks[index]._def;

            if (_OnTaskComplete != null)
                _OnTaskComplete.Invoke();

            _taskCompleteCount++;

            if (_taskCompleteCount >= _activeTasks.Count)
            {
                if (_characterMan != null)
                    _characterMan.CompleteTaskList();

                _allTasksCompleted = true;
            }
        }
    }

    ActiveTask GetActiveTask(TaskDef.Type type)
    {
        if (_curTaskList != null)
        {
            for (int i = 0; i < _activeTasks.Count; ++i)
            {
                ActiveTask task = _activeTasks[i];
                if (task._isComplete == false)
                {
                    if (task._def._type == type)
                        return _activeTasks[i];
                }
            }
        }

        return null;
    }

    private void Start()
    {
        // Disable all the task related triggers to start with 
        foreach (TriggerLogic trigger in _triggers)
            trigger.gameObject.SetActive(false);
    }
}