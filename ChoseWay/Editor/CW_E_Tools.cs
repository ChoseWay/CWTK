using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ChoseWay.Editor
{
    public class CW_E_Tools : OdinEditorWindow
    {
        public static Texture texture;
        #region 标题
        [MenuItem("水熊工具箱/工具箱")]
        static void ShowWindow()
        {
            CW_E_Tools window = GetWindow<CW_E_Tools>();
            GUIContent content = new GUIContent();
            texture = (Texture)Resources.Load("CWTK_Recources/InspectorTexture_Logo");
            content.image = texture;
            window.maxSize = new Vector2(532, 1200);
            window.minSize = new Vector2(532, 650);
            window.titleContent = content;
            window.Show();
        }


        [PropertyOrder(0), HorizontalGroup("Title", MarginLeft = 10, MarginRight = 10)]
        [VerticalGroup("Title/Logo")]
        [OnInspectorGUI]
        private void ShowImage()
        {
            GUILayout.Label((Texture)Resources.Load("CWTK_Recources/CWTK_Logo"));
            GUILayout.Space(-25); // 向上偏移
            GUIStyle rightAlignStyle = new GUIStyle(GUI.skin.label);
            rightAlignStyle.alignment = TextAnchor.MiddleRight;
            rightAlignStyle.margin = new RectOffset(0, 10, 0, 0); // 右侧留出10像素（约2个字符）的安全边距
            GUILayout.Label(CW_E_WelcomScreen.VERSION, rightAlignStyle);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.2f, 0.6f, 1f);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Space(-25); // 向上偏移
            GUILayout.Label("工具箱", titleStyle);
        }
        #region 分割线
        [PropertyOrder(0)]
        [HorizontalGroup("分割线", MarginLeft = 10, MarginRight = 10)]
        [OnInspectorGUI]
        private void ShowDivider()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        #endregion
        #endregion 标题

        #region Transform工具
        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/规则排列")]
        [HorizontalGroup("Transform工具/规则排列/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("Transform工具/规则排列/Split/Left")]
        [LabelText("行数")]
        [Range(1, 20)]
        public int gridRows = 1;

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/规则排列")]
        [VerticalGroup("Transform工具/规则排列/Split/Left")]
        [LabelText("列数")]
        [Range(1, 20)]
        public int gridColumns = 1;

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/规则排列")]
        [VerticalGroup("Transform工具/规则排列/Split/Left")]
        [LabelText("间隔大小")]
        public Vector2 gridSpacing = new Vector2(2f, 2f);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/规则排列")]
        [VerticalGroup("Transform工具/规则排列/Split/Right")]
        [Button("规则排列选中物体", ButtonHeight = 60)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void ArrangeObjectsInGrid()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }

            Undo.RecordObjects(selectedTransforms, "规则排列物体");

            int index = 0;
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    if (index < selectedTransforms.Length)
                    {
                        Vector3 position = new Vector3(col * gridSpacing.x, 0, row * gridSpacing.y);
                        selectedTransforms[index].position = position;
                        index++;
                    }
                }
            }
        }

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机分布")]
        [HorizontalGroup("Transform工具/随机分布/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("Transform工具/随机分布/Split/Left")]
        [LabelText("分布范围")]
        public Vector3 distributionRange = new Vector3(10f, 0f, 10f);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机分布")]
        [VerticalGroup("Transform工具/随机分布/Split/Right")]
        [Button("随机分布选中物体", ButtonHeight = 20)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void RandomlyDistributeObjects()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }

            Undo.RecordObjects(selectedTransforms, "随机分布物体");

            foreach (Transform transform in selectedTransforms)
            {
                float x = Random.Range(-distributionRange.x / 2, distributionRange.x / 2);
                float y = Random.Range(-distributionRange.y / 2, distributionRange.y / 2);
                float z = Random.Range(-distributionRange.z / 2, distributionRange.z / 2);
                transform.position = new Vector3(x, y, z);
            }
        }

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机偏移")]
        [HorizontalGroup("Transform工具/随机偏移/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("Transform工具/随机偏移/Split/Left")]
        [LabelText("偏移范围")]
        public Vector3 offsetRange = new Vector3(1f, 0f, 1f);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机偏移")]
        [VerticalGroup("Transform工具/随机偏移/Split/Right")]
        [Button("随机偏移选中物体", ButtonHeight = 20)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [Tooltip("在当前位置基础上对选中物体进行随机偏移")]
        public void RandomlyOffsetObjects()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }

            Undo.RecordObjects(selectedTransforms, "随机偏移物体");

            foreach (Transform transform in selectedTransforms)
            {
                Vector3 currentPosition = transform.position;
                float offsetX = Random.Range(-offsetRange.x, offsetRange.x);
                float offsetY = Random.Range(-offsetRange.y, offsetRange.y);
                float offsetZ = Random.Range(-offsetRange.z, offsetRange.z);
                
                Vector3 newPosition = currentPosition + new Vector3(offsetX, offsetY, offsetZ);
                transform.position = newPosition;
            }
        }

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机旋转")]
        [HorizontalGroup("Transform工具/随机旋转/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("Transform工具/随机旋转/Split/Left")]
        [LabelText("旋转范围X")]
        [MinMaxSlider(-180, 180, true)]
        public Vector2 rotationRangeX = new Vector2(-180, 180);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机旋转")]
        [VerticalGroup("Transform工具/随机旋转/Split/Left")]
        [LabelText("旋转范围Y")]
        [MinMaxSlider(-180, 180, true)]
        public Vector2 rotationRangeY = new Vector2(-180, 180);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机旋转")]
        [VerticalGroup("Transform工具/随机旋转/Split/Left")]
        [LabelText("旋转范围Z")]
        [MinMaxSlider(-180, 180, true)]
        public Vector2 rotationRangeZ = new Vector2(-180, 180);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机旋转")]
        [VerticalGroup("Transform工具/随机旋转/Split/Right")]
        [Button("随机旋转选中物体", ButtonHeight = 60)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void RandomlyRotateObjects()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }

            Undo.RecordObjects(selectedTransforms, "随机旋转物体");

            foreach (Transform transform in selectedTransforms)
            {
                float x = Random.Range(rotationRangeX.x, rotationRangeX.y);
                float y = Random.Range(rotationRangeY.x, rotationRangeY.y);
                float z = Random.Range(rotationRangeZ.x, rotationRangeZ.y);
                transform.rotation = Quaternion.Euler(x, y, z);
            }
        }

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机缩放")]
        [HorizontalGroup("Transform工具/随机缩放/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("Transform工具/随机缩放/Split/Left")]
        [LabelText("缩放范围X")]
        [MinMaxSlider(0.1f, 5f, true)]
        public Vector2 scaleRangeX = new Vector2(0.5f, 2f);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机缩放")]
        [VerticalGroup("Transform工具/随机缩放/Split/Left")]
        [LabelText("缩放范围Y")]
        [MinMaxSlider(0.1f, 5f, true)]
        public Vector2 scaleRangeY = new Vector2(0.5f, 2f);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机缩放")]
        [VerticalGroup("Transform工具/随机缩放/Split/Left")]
        [LabelText("缩放范围Z")]
        [MinMaxSlider(0.1f, 5f, true)]
        public Vector2 scaleRangeZ = new Vector2(0.5f, 2f);

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机缩放")]
        [VerticalGroup("Transform工具/随机缩放/Split/Left")]
        [LabelText("统一缩放")]
        public bool uniformScale = true;

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/随机缩放")]
        [VerticalGroup("Transform工具/随机缩放/Split/Right")]
        [Button("随机缩放选中物体", ButtonHeight = 80)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void RandomlyScaleObjects()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }

            Undo.RecordObjects(selectedTransforms, "随机缩放物体");

            foreach (Transform transform in selectedTransforms)
            {
                if (uniformScale)
                {
                    float scale = Random.Range(scaleRangeX.x, scaleRangeX.y);
                    transform.localScale = new Vector3(scale, scale, scale);
                }
                else
                {
                    float x = Random.Range(scaleRangeX.x, scaleRangeX.y);
                    float y = Random.Range(scaleRangeY.x, scaleRangeY.y);
                    float z = Random.Range(scaleRangeZ.x, scaleRangeZ.y);
                    transform.localScale = new Vector3(x, y, z);
                }
            }
        }

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/物体落地")]
        [HorizontalGroup("Transform工具/物体落地/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("Transform工具/物体落地/Split/Left")]
        [LabelText("偏移值")]
        [Tooltip("在落地后额外添加的Y轴偏移值")]
        public float groundOffset = 0f;

        [PropertyOrder(1)]
        [FoldoutGroup("Transform工具")]
        [BoxGroup("Transform工具/物体落地")]
        [VerticalGroup("Transform工具/物体落地/Split/Right")]
        [Button("物体落地", ButtonHeight = 30)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [Tooltip("将选中物体放置在地面上，使网格的最低点位于Y=0位置")]
        public void PlaceObjectsOnGround()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }

            Undo.RecordObjects(selectedTransforms, "物体落地");

            foreach (Transform transform in selectedTransforms)
            {
                PlaceObjectOnGround(transform);
            }
        }

        private void PlaceObjectOnGround(Transform objectTransform)
        {
            float lowestPointY = float.MaxValue;
            bool foundValidRenderer = false;
            
            // 检查该物体及其所有子物体的MeshFilter组件
            MeshFilter[] meshFilters = objectTransform.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    foundValidRenderer = true;
                    
                    // 获取网格中所有顶点
                    Vector3[] vertices = meshFilter.sharedMesh.vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        // 将顶点从本地坐标转换为世界坐标
                        Vector3 worldVertex = meshFilter.transform.TransformPoint(vertices[i]);
                        
                        // 更新最低点
                        if (worldVertex.y < lowestPointY)
                        {
                            lowestPointY = worldVertex.y;
                        }
                    }
                }
            }
            
            // 检查SkinnedMeshRenderer
            SkinnedMeshRenderer[] skinnedMeshes = objectTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
            {
                if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
                {
                    foundValidRenderer = true;
                    
                    // 对蒙皮网格需要烘焙当前姿势的网格
                    Mesh bakedMesh = new Mesh();
                    skinnedMesh.BakeMesh(bakedMesh);
                    
                    // 获取烘焙网格中所有顶点
                    Vector3[] vertices = bakedMesh.vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        // 将顶点从本地坐标转换为世界坐标
                        Vector3 worldVertex = skinnedMesh.transform.TransformPoint(vertices[i]);
                        
                        // 更新最低点
                        if (worldVertex.y < lowestPointY)
                        {
                            lowestPointY = worldVertex.y;
                        }
                    }
                }
            }
            
            // 如果没有找到有效的渲染器，尝试使用碰撞器
            if (!foundValidRenderer)
            {
                bool foundCollider = false;
                Collider[] colliders = objectTransform.GetComponentsInChildren<Collider>();
                
                foreach (Collider collider in colliders)
                {
                    if (collider != null)
                    {
                        foundCollider = true;
                        
                        // 处理不同类型的碰撞器
                        if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
                        {
                            // 对于网格碰撞器，检查所有顶点
                            Vector3[] vertices = meshCollider.sharedMesh.vertices;
                            for (int i = 0; i < vertices.Length; i++)
                            {
                                Vector3 worldVertex = meshCollider.transform.TransformPoint(vertices[i]);
                                if (worldVertex.y < lowestPointY)
                                {
                                    lowestPointY = worldVertex.y;
                                }
                            }
                        }
                        else
                        {
                            // 对于其他类型的碰撞器，使用边界框的8个角点
                            Bounds bounds = collider.bounds;
                            Vector3[] cornerPoints = new Vector3[8];
                            
                            // 计算边界框的8个角点（世界坐标）
                            cornerPoints[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
                            cornerPoints[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
                            cornerPoints[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
                            cornerPoints[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
                            cornerPoints[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
                            cornerPoints[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
                            cornerPoints[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
                            cornerPoints[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
                            
                            // 找出最低点
                            foreach (Vector3 point in cornerPoints)
                            {
                                if (point.y < lowestPointY)
                                {
                                    lowestPointY = point.y;
                                }
                            }
                        }
                    }
                }
                
                // 如果既没有渲染器也没有碰撞器
                if (!foundCollider)
                {
                    // 直接使用物体的位置
                    lowestPointY = objectTransform.position.y;
                }
            }
            
            // 计算需要移动的距离，使最低点到达地面
            float targetY = groundOffset; // 目标高度（地面 + 偏移）
            float offsetY = targetY - lowestPointY; // 需要上移的距离
            
            // 应用偏移到物体的位置
            Vector3 newPosition = objectTransform.position;
            newPosition.y += offsetY;
            objectTransform.position = newPosition;
        }
        #endregion
        
        #region 层级工具
        [PropertyOrder(3)]
        [FoldoutGroup("层级工具")]
        [BoxGroup("层级工具/重置父物体")]
        [HorizontalGroup("层级工具/重置父物体/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("层级工具/重置父物体/Split/Left")]
        [LabelText("保持子物体世界位置")]
        public bool maintainChildPosition = true;
        
        [PropertyOrder(3)]
        [FoldoutGroup("层级工具")]
        [BoxGroup("层级工具/重置父物体")]
        [VerticalGroup("层级工具/重置父物体/Split/Right")]
        [Button("重置父物体变换", ButtonHeight = 30)]
        [GUIColor(0.4f, 0.8f, 1f)]
        [Tooltip("重置选中物体的父物体的位移、旋转和缩放，同时保持选中物体的世界坐标不变")]
        public void ResetParentTransform()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }
            
            foreach (Transform transform in selectedTransforms)
            {
                if (transform.parent == null)
                {
                    Debug.LogWarning($"物体 {transform.name} 没有父物体，已跳过");
                    continue;
                }
                
                Transform parent = transform.parent;
                
                // 记录Undo
                Undo.RecordObject(parent, "重置父物体变换");
                
                if (maintainChildPosition)
                {
                    // 记录所有子物体的世界坐标信息
                    Dictionary<Transform, TransformData> childrenWorldData = new Dictionary<Transform, TransformData>();
                    foreach (Transform child in parent)
                    {
                        childrenWorldData[child] = new TransformData(
                            child.position,
                            child.rotation,
                            child.lossyScale
                        );
                    }
                    
                    // 重置父物体变换
                    parent.localPosition = Vector3.zero;
                    parent.localRotation = Quaternion.identity;
                    parent.localScale = Vector3.one;
                    
                    // 恢复所有子物体的世界坐标
                    foreach (var pair in childrenWorldData)
                    {
                        Transform child = pair.Key;
                        TransformData data = pair.Value;
                        
                        Undo.RecordObject(child, "重置父物体变换");
                        
                        child.position = data.position;
                        child.rotation = data.rotation;
                        
                        // 因为缩放可能受父级影响，这里只能尽量接近原来的值
                        Vector3 currentLossyScale = child.lossyScale;
                        Vector3 scaleMultiplier = new Vector3(
                            Mathf.Approximately(currentLossyScale.x, 0) ? 1 : data.lossyScale.x / currentLossyScale.x,
                            Mathf.Approximately(currentLossyScale.y, 0) ? 1 : data.lossyScale.y / currentLossyScale.y,
                            Mathf.Approximately(currentLossyScale.z, 0) ? 1 : data.lossyScale.z / currentLossyScale.z
                        );
                        
                        child.localScale = Vector3.Scale(child.localScale, scaleMultiplier);
                    }
                }
                else
                {
                    // 直接重置父物体变换，不保持子物体世界坐标
                    parent.localPosition = Vector3.zero;
                    parent.localRotation = Quaternion.identity;
                    parent.localScale = Vector3.one;
                }
            }
        }
        
        // 用于存储物体变换信息的辅助类
        private class TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 lossyScale;
            
            public TransformData(Vector3 pos, Quaternion rot, Vector3 scale)
            {
                position = pos;
                rotation = rot;
                lossyScale = scale;
            }
        }
        #endregion
        
        #region 其他工具
        [PropertyOrder(9)]
        [FoldoutGroup("其他工具")]
        [BoxGroup("其他工具/批量重命名")]
        [HorizontalGroup("其他工具/批量重命名/Split", 0.7f, LabelWidth = 80)]
        [VerticalGroup("其他工具/批量重命名/Split/Left")]
        [LabelText("基准名称")]
        public string baseObjectName = "Object";
        
        [PropertyOrder(9)]
        [FoldoutGroup("其他工具")]
        [BoxGroup("其他工具/批量重命名")]
        [VerticalGroup("其他工具/批量重命名/Split/Right")]
        [Button("批量重命名选中物体", ButtonHeight = 30)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void BatchRenameObjects()
        {
            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择物体", "确定");
                return;
            }

            Undo.RecordObjects(selectedTransforms, "批量重命名物体");

            // 按照Hierarchy顺序排序
            System.Array.Sort(selectedTransforms, (a, b) => 
            {
                // 获取在Hierarchy中的排序索引
                int aIndex = GetHierarchyIndex(a);
                int bIndex = GetHierarchyIndex(b);
                return aIndex.CompareTo(bIndex);
            });

            for (int i = 0; i < selectedTransforms.Length; i++)
            {
                string newName = $"{baseObjectName}_{i}";
                selectedTransforms[i].name = newName;
            }
        }

        // 获取GameObject在Hierarchy中的索引
        private int GetHierarchyIndex(Transform transform)
        {
            // 如果是根对象
            if (transform.parent == null)
            {
                // 查找场景中所有根对象
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    for (int j = 0; j < rootObjects.Length; j++)
                    {
                        if (rootObjects[j].transform == transform)
                            return j;
                    }
                }
            }
            // 如果是子对象
            else
            {
                // 返回在父对象中的索引
                return transform.GetSiblingIndex();
            }
            return 0;
        }
        #endregion
    }
}