using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[HideMonoScript]
[ExecuteInEditMode]
public class BezierLine : MonoBehaviour
{ 
    [Title("贝塞尔曲线控制器",TitleAlignment =TitleAlignments.Centered,Subtitle ="建议挂在和LineRenderer组件物体上")]
    [Title("场景中的LineRenderer对象",HorizontalLine =false)]
    [SerializeField] LineRenderer lineRenderer;
    [Title("取点坐标的数量", HorizontalLine = false)]
    [Range(3, 100)]
    [SerializeField] int bezierPointCount = 10;
    //[SerializeField] List<Vector3> pointList;
    [Title("取点的坐标列表", HorizontalLine = false)]
    public List<Transform> positionList;
     List<Vector3> pointList;
    [Title("贝塞尔曲线属性")]
    [InfoBox("如果勾选静态曲线,则不会每帧渲染,只会在开始游戏时渲染一遍")]
    [LabelText("是否显示曲线"),LabelWidth(250)]
    public bool isShowLine;
    [LabelText("是否开场就渲染显示曲线"), LabelWidth(250)]
    public bool isShowOnStart = false;
    [LabelText("是否是静态曲线"), LabelWidth(250)]
    public bool isStatic = false;
    bool isUpdateLine = true;
    void Awake()
    {
        ////显示曲线
        //lineRenderer.positionCount = bezierPointCount;
        //lineRenderer.SetPositions(DrawBezierLine(pointList, bezierPointCount));
        //positionList = new List<Object>();
        pointList = new List<Vector3>();
        if (isShowOnStart)
        {
            SetLine();
            isShowLine = true;
        }
    }

    public void SetLine()
    {
        pointList.Clear();
        for (int i = 0; i < positionList.Count; i++)
        {
            //Debug.Log(positionList[i].position);
            pointList.Add(positionList[i].position);
        }
    }


    private void LateUpdate()
    {
        if (isShowLine&& isUpdateLine)
        {
            for (int i = 0; i < positionList.Count; i++)
            {
                pointList[i] = positionList[i].position;
            }
            lineRenderer.positionCount = bezierPointCount;
            lineRenderer.SetPositions(DrawBezierLine(pointList, bezierPointCount));
            if (isStatic)
            {
                isUpdateLine = false;
            }
        }
    }

    public Vector3[] DrawBezierLine(List<Vector3> targetPointList, int bezierPointCount)
    {
        if (targetPointList == null || targetPointList.Count < 2 || bezierPointCount < 2)
            return null;
        Vector3[] bezierPoints = new Vector3[bezierPointCount];
        for (int i = 0; i < bezierPointCount; i++)
        {
            //通过递增的插值系数f(保证取值范围0~1), 获取连续的曲线折点
            bezierPoints[i] = BezierPoint(1.0f * i / (bezierPointCount - 1), targetPointList);
        }
        return bezierPoints;
    }
   


    //一阶直线(插值系数, 起始点, 终止点)
    Vector3 BezierPoint(float f, Vector3 p0, Vector3 p1)
    {
        ////插值计算原始公式, 插值系数: f >= 0 & f <= 1
        //return p0 + (p1 - p0) * f;
        //公式变形
        return (1 - f) * p0 + f * p1;
    }
    //二阶曲线(插值系数, 起始点, 控制点, 终止点)
    Vector3 BezierPoint(float f, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        ////降为一阶
        //Vector3 p0p1 = (1 - f) * p0 + f * p1;
        //Vector3 p1p2 = (1 - f) * p1 + f * p2;
        //return (1 - f) * p0p1 + f * p1p2;
        //合并化简公式
        return Mathf.Pow(1 - f, 2) * p0 + 2 * f * (1 - f) * p1 + Mathf.Pow(f, 2) * p2;
    }
    //三阶曲线(插值系数, 起始点, 控制点, 控制点, 终止点)
    Vector3 BezierPoint(float f, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        ////降为二阶
        //Vector3 p0p1 = (1 - f) * p0 + f * p1;
        //Vector3 p1p2 = (1 - f) * p1 + f * p2;
        //Vector3 p2p3 = (1 - f) * p2 + f * p3;
        ////降为一阶
        //Vector3 p0p1p2 = (1 - f) * p0p1 + f * p1p2;
        //Vector3 p1p2p3 = (1 - f) * p1p2 + f * p2p3;
        //return (1 - f) * p0p1p2 + f * p1p2p3;
        //合并化简公式
        return Mathf.Pow(1 - f, 3) * p0 + 3 * f * Mathf.Pow(1 - f, 2) * p1 + 3 * Mathf.Pow(f, 2) * (1 - f) * p2 + Mathf.Pow(f, 3) * p3;
    }
    //N阶曲线(插值系数, List(起始点, 控制点 ··· , 终止点))
    public Vector3 BezierPoint(float f, List<Vector3> pointList)
    {
        if (pointList.Count == 1)
            return pointList[0];
        //降阶
        List<Vector3> newPointList = new List<Vector3>();
        for (int i = 0; i < pointList.Count - 1; i++)
        {
            newPointList.Add((1 - f) * pointList[i] + f * pointList[i + 1]);
        }
        //递归计算
        return BezierPoint(f, newPointList);
    }
}