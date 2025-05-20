using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ChoseWay.Event;


public class StepControllerEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        StepController step = (StepController)target;
        if(GUILayout.Button("执行步骤")) {
            step.DoStep(step.index);
        }
        if (GUILayout.Button("执行所有"))
        {
            step.DoStep(0);
        }
    }
    
}
