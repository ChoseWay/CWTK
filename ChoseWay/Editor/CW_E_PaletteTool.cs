using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Codice.Client.BaseCommands.TubeClient;

namespace ChoseWay.Editor
{
    /// <summary>
    /// Unity编辑器调色盘工具，提供颜色选择、编辑和管理功能
    /// </summary>
    public class CW_E_PaletteTool : EditorWindow
    {
        private Color currentColor = Color.white;
        private string hexColor = "#FFFFFF";
        private Vector2 scrollPosition;

        // 记录当前正在拖动的滑块ID
        private static int activeSliderID = -1;

        // 记录滑块拖动状态
        private static Vector2 lastMousePosition;
        private static Rect activeSliderRect;
        private static float activeSliderValue = 0;
        private static bool isDragging = false;

        // 圆角矩形纹理缓存
        private Texture2D roundRectTexture;

        private Texture2D colorWheel;        // 环形色轮
        private Texture2D colorSquare;       // 中间的方形色彩/饱和度选择区域
        private Texture2D colorWheelThumb;   // 色轮上的选择标记
        private Texture2D squareThumb;       // 方形区域的选择标记

        private Rect colorWheelRect;         // 色轮区域
        private Rect colorSquareRect;        // 方形选择区域

        // HSV颜色参数
        private float hue = 0f;              // 色相
        private float saturation = 0f;       // 饱和度
        private float brightness = 1f;       // 亮度
        private float alpha = 1f;            // 透明度

        // 拖动状态
        private bool isDraggingWheel = false;
        private bool isDraggingSquare = false;

        // 色板 - 限制为5个颜色的FIFO队列
        private Queue<Color> colorPresets = new Queue<Color>(5);
        private const int MAX_PRESETS = 5;
        private bool showPresets = true;

        // 存储颜色滑块纹理
        private Texture2D redSliderTex;
        private Texture2D greenSliderTex;
        private Texture2D blueSliderTex;
        private Texture2D alphaSliderTex;

        /// <summary>
        /// 在Unity菜单中添加调色盘工具选项并显示窗口
        /// </summary>
        [MenuItem("水熊工具箱/调色盘工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<CW_E_PaletteTool>("调色盘工具");
            window.minSize = new Vector2(250, 600);  // 设置稍微宽一些的最小宽度，确保所有元素都能正常显示
        }

        /// <summary>
        /// 窗口启用时初始化各种纹理和加载保存的颜色预设
        /// </summary>
        private void OnEnable()
        {
            // 注册接收鼠标移动事件
            wantsMouseMove = true;

            // 添加全局事件监听
            EditorApplication.update += OnEditorUpdate;

            GenerateColorWheel();
            GenerateColorSquare();
            GenerateColorWheelThumb();
            GenerateSquareThumb();

            // 初始化RGBA滑块纹理
            GenerateColorSliderTextures();

            // 加载保存的预设颜色
            LoadColorPresets();
        }

        private void OnDisable()
        {
            // 移除全局事件监听
            EditorApplication.update -= OnEditorUpdate;

            // 释放滑块锁定
            activeSliderID = -1;
            isDragging = false;
        }

        /// <summary>
        /// 编辑器全局更新事件，用于处理滑块拖动
        /// </summary>
        private void OnEditorUpdate()
        {
            // 确保拖动状态有效且活动滑块ID有效
            if (isDragging && activeSliderID != -1)
            {
                // 强制窗口重绘，让OnGUI能够接收到最新的鼠标事件
                Repaint();
            }
        }

        /// <summary>
        /// 检测全局鼠标事件，包括窗口外
        /// </summary>
        private void OnGUI()
        {
            // 处理全局MouseUp事件来解除滑块锁定
            UnityEngine.Event e = UnityEngine.Event.current;

            // 如果正在拖动并且鼠标位置变化或释放
            if (isDragging && activeSliderID != -1)
            {
                // 鼠标位置变化时更新滑块值
                if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
                {
                    Vector2 mousePos = e.mousePosition;

                    // 只在鼠标位置变化时更新
                    if (mousePos != lastMousePosition)
                    {
                        lastMousePosition = mousePos;
                        UpdateSliderValueFromMouse(mousePos);
                    }
                }

                // 鼠标释放时结束拖动
                if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    isDragging = false;
                    activeSliderID = -1;
                    Repaint();
                }
            }

            // 继续正常绘制
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 获取可用宽度
            float availableWidth = position.width - 200; // 预留边距

            GUILayout.Space(10);

            // 颜色预览区 - 圆角矩形，自适应宽度并确保居中
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // 左侧弹性空间

            // 计算预览矩形的宽度，不超过可用宽度
            float previewWidth = Mathf.Min(availableWidth, 200);
            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, 30);

            // 确保预览矩形水平居中
            float centerOffset = (EditorGUIUtility.currentViewWidth - previewWidth) / 2;
            previewRect.x = centerOffset;

            DrawRoundedRect(previewRect, currentColor, 8);
            GUILayout.FlexibleSpace(); // 右侧弹性空间
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            // 绘制色轮和色彩饱和度选择区域
            DrawColorControls();

            GUILayout.Space(15);

            // 显示RGB滑块
            DrawColorSliders();

            GUILayout.Space(15);

            // 颜色参数和复制功能
            DrawColorCopyButtons();

            GUILayout.Space(15);

            // 显示精简的色板
            DrawColorPresets();

            EditorGUILayout.EndScrollView();

            // 保证GUI更新
            if (GUI.changed)
            {
                Repaint();
            }
        }

        /// <summary>
        /// 绘制圆角矩形
        /// </summary>
        /// <param name="rect">要绘制的区域</param>
        /// <param name="color">矩形颜色</param>
        /// <param name="radius">圆角半径</param>
        private void DrawRoundedRect(Rect rect, Color color, float radius)
        {
            // 防止圆角半径过大
            radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) / 5);

            // 保存当前颜色
            Color oldColor = GUI.color;

            // 创建一个临时的纹理用于圆角矩形
            Texture2D roundRectTex = new Texture2D(1, 1);
            roundRectTex.SetPixel(0, 0, Color.white);
            roundRectTex.Apply();

            // 设置绘制颜色
            GUI.color = color;

            // 绘制矩形主体（带圆角）
            GUI.DrawTexture(rect, roundRectTex, ScaleMode.StretchToFill, true, 0, color, 0, radius);

            // 恢复颜色
            GUI.color = oldColor;

            // 用完后销毁临时纹理
            DestroyImmediate(roundRectTex);
        }

        /// <summary>
        /// 根据鼠标位置更新滑块值
        /// </summary>
        private void UpdateSliderValueFromMouse(Vector2 mousePos)
        {
            if (activeSliderID == -1 || !isDragging) return;

            // 计算相对于滑块的水平位置
            float xPos = Mathf.Clamp(mousePos.x, activeSliderRect.x, activeSliderRect.x + activeSliderRect.width);
            float normalizedValue = Mathf.Clamp01((xPos - activeSliderRect.x) / activeSliderRect.width);

            // 保存当前值
            activeSliderValue = normalizedValue;

            // 根据不同滑块更新颜色
            UpdateColorFromSliderValue();
        }

        /// <summary>
        /// 根据当前活动滑块值更新颜色
        /// </summary>
        private void UpdateColorFromSliderValue()
        {
            if (activeSliderID == -1) return;

            float normalizedValue = activeSliderValue;

            // 根据滑块类型更新不同的颜色通道
            switch (activeSliderID)
            {
                case 1: // R滑块
                    float redValue = normalizedValue;
                    currentColor = new Color(redValue, currentColor.g, currentColor.b, currentColor.a);
                    break;
                case 2: // G滑块
                    float greenValue = normalizedValue;
                    currentColor = new Color(currentColor.r, greenValue, currentColor.b, currentColor.a);
                    break;
                case 3: // B滑块
                    float blueValue = normalizedValue;
                    currentColor = new Color(currentColor.r, currentColor.g, blueValue, currentColor.a);
                    break;
                case 4: // A滑块
                    alpha = normalizedValue;
                    currentColor = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
                    break;
            }

            // 更新HSV和十六进制值
            Color.RGBToHSV(currentColor, out hue, out saturation, out brightness);
            hexColor = ColorUtility.ToHtmlStringRGB(currentColor);

            // 更新滑块纹理
            UpdateColorSliders();
        }

        /// <summary>
        /// 绘制色轮和色彩饱和度选择区域
        /// </summary>
        private void DrawColorControls()
        {
            // 计算可用宽度，并根据窗口大小调整色轮尺寸
            float availableWidth = position.width; // 使用完整窗口宽度
            float wheelSize = Mathf.Min(availableWidth, 210);
            float squareSize = wheelSize * 0.5f;  // 减小中心方形的尺寸，防止重叠

            // 使用完全居中的布局
            float centerX = (position.width - wheelSize) / 2; // 计算左侧偏移量使色轮居中

            // 色轮区域
            Rect wheelRect = new Rect(centerX, GUILayoutUtility.GetRect(1, wheelSize).y, wheelSize, wheelSize);
            colorWheelRect = wheelRect;
            GUI.DrawTexture(colorWheelRect, colorWheel);

            // 方形选择区域 (居中于色轮内)
            float squareX = colorWheelRect.x + (wheelSize - squareSize) / 2;
            float squareY = colorWheelRect.y + (wheelSize - squareSize) / 2;
            colorSquareRect = new Rect(squareX, squareY, squareSize, squareSize);
            GUI.DrawTexture(colorSquareRect, colorSquare);

            // 处理鼠标事件
            HandleColorWheelEvents();
            HandleColorSquareEvents();

            // 绘制当前选择的点（色轮）
            DrawWheelSelector();

            // 绘制当前选择的点（方形区域）
            DrawSquareSelector();
        }

        /// <summary>
        /// 在色轮上绘制当前选择的色相指示器
        /// </summary>
        private void DrawWheelSelector()
        {
            // 获取色轮参数
            float wheelRadius = colorWheelRect.width / 2;
            Vector2 center = new Vector2(colorWheelRect.x + wheelRadius, colorWheelRect.y + wheelRadius);

            // 计算环形内外半径 - 确保与GenerateColorWheel和HandleColorWheelEvents一致
            float outerRadius = wheelRadius;
            float innerRadius = wheelRadius * 0.8f;

            // 计算环形宽度和中心位置
            float ringWidth = outerRadius - innerRadius;
            float ringMiddleRadius = innerRadius + (ringWidth / 2);

            // 选择器大小应该适合环形宽度，但不能太大或太小
            float thumbSize = Math.Min(Math.Max(ringWidth, 10), 30);

            // 计算指示器在环上的位置，需要与UpdateHueFromMousePosition保持一致
            float angle = hue * 2 * Mathf.PI;
            // 使用反向的Y坐标计算，与色相计算逻辑保持一致
            Vector2 thumbPos = center + new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * ringMiddleRadius;

            // 绘制白色选择器
            Rect thumbRect = new Rect(thumbPos.x - thumbSize / 2, thumbPos.y - thumbSize / 2, thumbSize, thumbSize);

            // 绘制圆形选择器
            Color oldColor = GUI.color;

            // 绘制白色边框
            GUI.color = new Color(1, 1, 1, 1);
            GUI.DrawTexture(thumbRect, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, true, 0, Color.white, 0, thumbSize / 2);

            // 内部区域透明（而非黑色）
            float innerSize = thumbSize - 4;
            if (innerSize > 0)
            {
                Rect innerRect = new Rect(
                    thumbRect.x + (thumbSize - innerSize) / 2,
                    thumbRect.y + (thumbSize - innerSize) / 2,
                    innerSize,
                    innerSize
                );
                // 不绘制内部，保持透明
            }

            // 恢复GUI颜色
            GUI.color = oldColor;
        }

        /// <summary>
        /// 在色彩方块上绘制当前选择的饱和度和亮度指示器
        /// </summary>
        private void DrawSquareSelector()
        {
            // 调整选择器大小
            float thumbSize = 16f;
            float x = colorSquareRect.x + saturation * colorSquareRect.width;
            float y = colorSquareRect.y + (1 - brightness) * colorSquareRect.height;

            // 绘制白色圆形选择器
            Rect thumbRect = new Rect(x - thumbSize / 2, y - thumbSize / 2, thumbSize, thumbSize);

            // 使用GUI直接绘制圆形
            Color oldColor = GUI.color;

            // 白色边框
            GUI.color = new Color(1, 1, 1, 1);
            GUI.DrawTexture(thumbRect, EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, true, 0, Color.white, 0, thumbSize / 2);

            // 内部保持透明，不绘制黑色内圈

            // 恢复GUI颜色
            GUI.color = oldColor;
        }

        /// <summary>
        /// 处理色轮上的鼠标事件，实现色相选择功能
        /// </summary>
        private void HandleColorWheelEvents()
        {
            UnityEngine.Event e = UnityEngine.Event.current;
            Vector2 mousePos = e.mousePosition;

            // 获取色轮中心和半径
            float wheelRadius = colorWheelRect.width / 2;
            Vector2 center = new Vector2(colorWheelRect.x + wheelRadius, colorWheelRect.y + wheelRadius);

            // 计算内外圆半径 - 确保与GenerateColorWheel中相同
            float outerRadius = wheelRadius;
            float innerRadius = wheelRadius * 0.8f;

            // 计算鼠标到中心的距离
            Vector2 delta = mousePos - center;
            float distance = delta.magnitude;

            // 检查鼠标是否在环形区域内
            bool inRing = distance <= outerRadius && distance >= innerRadius;

            if (e.type == EventType.MouseDown && e.button == 0 && inRing)
            {
                isDraggingWheel = true;
                UpdateHueFromMousePosition(mousePos, center);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDraggingWheel)
            {
                UpdateHueFromMousePosition(mousePos, center);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && isDraggingWheel)
            {
                isDraggingWheel = false;
                e.Use();
            }
        }

        /// <summary>
        /// 处理色彩方块上的鼠标事件，实现饱和度和亮度选择功能
        /// </summary>
        private void HandleColorSquareEvents()
        {
            UnityEngine.Event e = UnityEngine.Event.current;
            Vector2 mousePos = e.mousePosition;

            if (e.type == EventType.MouseDown && e.button == 0 && colorSquareRect.Contains(mousePos))
            {
                isDraggingSquare = true;
                UpdateSaturationBrightnessFromMousePosition(mousePos);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDraggingSquare)
            {
                UpdateSaturationBrightnessFromMousePosition(mousePos);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && isDraggingSquare)
            {
                isDraggingSquare = false;
                e.Use();
            }
        }

        /// <summary>
        /// 根据鼠标在色轮上的位置更新色相值
        /// </summary>
        /// <param name="mousePos">鼠标当前位置</param>
        /// <param name="center">色轮中心位置</param>
        private void UpdateHueFromMousePosition(Vector2 mousePos, Vector2 center)
        {
            // 计算鼠标相对于中心的方向向量
            Vector2 direction = mousePos - center;

            // 翻转y轴方向以修正上下颠倒问题
            direction.y = -direction.y;

            // 计算角度（色相）- 使用修正后的方向
            hue = (Mathf.Atan2(direction.y, direction.x) / (2 * Mathf.PI));
            if (hue < 0) hue += 1;

            // 更新方形选择区域的颜色
            GenerateColorSquare();

            // 更新当前颜色
            UpdateCurrentColor();
        }

        /// <summary>
        /// 根据鼠标在色彩方块上的位置更新饱和度和亮度值
        /// </summary>
        /// <param name="mousePos">鼠标当前位置</param>
        private void UpdateSaturationBrightnessFromMousePosition(Vector2 mousePos)
        {
            // 计算相对位置（0-1范围）
            saturation = Mathf.Clamp01((mousePos.x - colorSquareRect.x) / colorSquareRect.width);
            brightness = 1 - Mathf.Clamp01((mousePos.y - colorSquareRect.y) / colorSquareRect.height);

            // 更新当前颜色
            UpdateCurrentColor();
        }

        /// <summary>
        /// 绘制RGB和Alpha调整滑块
        /// </summary>
        private void DrawColorSliders()
        {
            // EditorGUILayout.LabelField("颜色分量", EditorStyles.boldLabel);

            // 获取可用宽度
            float availableWidth = position.width - 10; // 预留边距和滚动条空间

            // 确保滑块纹理已初始化
            if (redSliderTex == null || greenSliderTex == null || blueSliderTex == null || alphaSliderTex == null)
            {
                GenerateColorSliderTextures();
            }

            // 保存当前颜色值（0-1范围）
            float r = currentColor.r;
            float g = currentColor.g;
            float b = currentColor.b;
            float a = alpha;

            // 将颜色值转换为0-255范围的整数，用于显示和输入
            int rInt = Mathf.RoundToInt(r * 255);
            int gInt = Mathf.RoundToInt(g * 255);
            int bInt = Mathf.RoundToInt(b * 255);
            int aInt = Mathf.RoundToInt(a * 255);

            // 创建统一的样式
            GUIStyle sliderBgStyle = new GUIStyle(GUI.skin.horizontalSlider);
            sliderBgStyle.fixedHeight = 16;

            GUIStyle thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumbStyle.fixedHeight = 16;

            // 计算输入框宽度
            float inputWidth = Mathf.Min(45, availableWidth * 0.15f);

            // 滑块外边距
            GUILayout.Space(5);

            // R通道滑块
            EditorGUILayout.BeginHorizontal(GUILayout.Width(availableWidth));
            // 使用固定宽度的Label替代PrefixLabel
            GUILayout.Label("R", GUILayout.Width(15));

            // 开始滑块区域，使用剩余宽度减去标签和输入框宽度
            GUILayout.BeginHorizontal(GUILayout.Width(availableWidth - 15 - inputWidth - 10));
            // 绘制自定义纹理滑块
            int oldRInt = rInt;
            rInt = DrawColorSlider(rInt, redSliderTex, sliderBgStyle, thumbStyle, 1); // 使用ID 1表示R滑块
            GUILayout.EndHorizontal();

            // 输入框
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1, 0.3f, 0.3f, 0.6f); // 红色背景
            string rString = EditorGUILayout.TextField(rInt.ToString(), GUILayout.Width(inputWidth));
            GUI.backgroundColor = oldBgColor; // 恢复原色

            if (int.TryParse(rString, out int newRInt))
            {
                rInt = Mathf.Clamp(newRInt, 0, 255);
            }

            EditorGUILayout.EndHorizontal();

            // 增加滑块间的行间距
            GUILayout.Space(4);

            // 检查R值是否变化
            if (rInt != oldRInt)
            {
                // 更新颜色和所有滑块
                float redValue = rInt / 255f;
                currentColor = new Color(redValue, currentColor.g, currentColor.b, currentColor.a);

                // 更新HSV值
                Color.RGBToHSV(currentColor, out hue, out saturation, out brightness);
                hexColor = ColorUtility.ToHtmlStringRGB(currentColor);

                // 立即更新其他所有滑块的背景
                GenerateColorSliderTextures();
                Repaint();
            }

            // G通道滑块
            EditorGUILayout.BeginHorizontal(GUILayout.Width(availableWidth));
            // 使用固定宽度的Label替代PrefixLabel
            GUILayout.Label("G", GUILayout.Width(15));

            // 开始滑块区域，使用剩余宽度减去标签和输入框宽度
            GUILayout.BeginHorizontal(GUILayout.Width(availableWidth - 15 - inputWidth - 10));
            // 绘制自定义纹理滑块
            int oldGInt = gInt;
            gInt = DrawColorSlider(gInt, greenSliderTex, sliderBgStyle, thumbStyle, 2); // 使用ID 2表示G滑块
            GUILayout.EndHorizontal();

            // 输入框
            GUI.backgroundColor = new Color(0.3f, 1, 0.3f, 0.6f); // 绿色背景
            string gString = EditorGUILayout.TextField(gInt.ToString(), GUILayout.Width(inputWidth));
            GUI.backgroundColor = oldBgColor; // 恢复原色

            if (int.TryParse(gString, out int newGInt))
            {
                gInt = Mathf.Clamp(newGInt, 0, 255);
            }

            EditorGUILayout.EndHorizontal();

            // 增加滑块间的行间距
            GUILayout.Space(4);

            // 检查G值是否变化
            if (gInt != oldGInt)
            {
                // 更新颜色和所有滑块
                float greenValue = gInt / 255f;
                currentColor = new Color(currentColor.r, greenValue, currentColor.b, currentColor.a);

                // 更新HSV值
                Color.RGBToHSV(currentColor, out hue, out saturation, out brightness);
                hexColor = ColorUtility.ToHtmlStringRGB(currentColor);

                // 立即更新其他所有滑块的背景
                GenerateColorSliderTextures();
                Repaint();
            }

            // B通道滑块
            EditorGUILayout.BeginHorizontal(GUILayout.Width(availableWidth));
            // 使用固定宽度的Label替代PrefixLabel
            GUILayout.Label("B", GUILayout.Width(15));

            // 开始滑块区域，使用剩余宽度减去标签和输入框宽度
            GUILayout.BeginHorizontal(GUILayout.Width(availableWidth - 15 - inputWidth - 10));
            // 绘制自定义纹理滑块
            int oldBInt = bInt;
            bInt = DrawColorSlider(bInt, blueSliderTex, sliderBgStyle, thumbStyle, 3); // 使用ID 3表示B滑块
            GUILayout.EndHorizontal();

            // 输入框
            GUI.backgroundColor = new Color(0.3f, 0.3f, 1, 0.6f); // 蓝色背景
            string bString = EditorGUILayout.TextField(bInt.ToString(), GUILayout.Width(inputWidth));
            GUI.backgroundColor = oldBgColor; // 恢复原色

            if (int.TryParse(bString, out int newBInt))
            {
                bInt = Mathf.Clamp(newBInt, 0, 255);
            }

            EditorGUILayout.EndHorizontal();

            // 增加滑块间的行间距
            GUILayout.Space(4);

            // 检查B值是否变化
            if (bInt != oldBInt)
            {
                // 更新颜色和所有滑块
                float blueValue = bInt / 255f;
                currentColor = new Color(currentColor.r, currentColor.g, blueValue, currentColor.a);

                // 更新HSV值
                Color.RGBToHSV(currentColor, out hue, out saturation, out brightness);
                hexColor = ColorUtility.ToHtmlStringRGB(currentColor);

                // 立即更新其他所有滑块的背景
                GenerateColorSliderTextures();
                Repaint();
            }

            // A通道滑块
            EditorGUILayout.BeginHorizontal(GUILayout.Width(availableWidth));
            // 使用固定宽度的Label替代PrefixLabel
            GUILayout.Label("A", GUILayout.Width(15));

            // 开始滑块区域，使用剩余宽度减去标签和输入框宽度
            GUILayout.BeginHorizontal(GUILayout.Width(availableWidth - 15 - inputWidth - 10));
            // 绘制自定义纹理滑块
            int oldAInt = aInt;
            aInt = DrawColorSlider(aInt, alphaSliderTex, sliderBgStyle, thumbStyle, 4); // 使用ID 4表示A滑块
            GUILayout.EndHorizontal();

            // 输入框
            string aString = EditorGUILayout.TextField(aInt.ToString(), GUILayout.Width(inputWidth));

            if (int.TryParse(aString, out int newAInt))
            {
                aInt = Mathf.Clamp(newAInt, 0, 255);
            }

            EditorGUILayout.EndHorizontal();

            // 检查A值是否变化
            if (aInt != oldAInt)
            {
                // 透明度只更新Alpha值，不影响RGB滑块背景
                alpha = aInt / 255f;
                currentColor = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);

                // 只更新Alpha滑块纹理，保持RGB滑块不变
                UpdateAlphaSliderTexture();
                Repaint();
            }

            // 不再需要这段代码，因为每个滑块变化时都会立即更新
            float newR = rInt / 255f;
            float newG = gInt / 255f;
            float newB = bInt / 255f;
            float newA = aInt / 255f;

            // 检查总体是否有变化（确保一次性输入后的更新）
            if (newR != r || newG != g || newB != b || newA != a)
            {
                currentColor = new Color(newR, newG, newB);
                alpha = newA;

                // 更新HSV值
                Color.RGBToHSV(currentColor, out hue, out saturation, out brightness);
                hexColor = ColorUtility.ToHtmlStringRGB(currentColor);

                // 更新色彩方块 - 只有RGB变化时才需要
                if (newR != r || newG != g || newB != b)
                {
                    GenerateColorSquare();
                }

                // 更新所有滑块纹理
                GenerateColorSliderTextures();
            }
        }

        /// <summary>
        /// 仅更新Alpha滑块纹理，其他滑块保持不变
        /// </summary>
        private void UpdateAlphaSliderTexture()
        {
            int width = 256;
            int height = 16;

            // 释放已有Alpha纹理
            if (alphaSliderTex != null) DestroyImmediate(alphaSliderTex);

            // 创建Alpha滑块纹理（带棋盘格背景）
            alphaSliderTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 创建棋盘格背景
                    bool isEvenX = (x / 4) % 2 == 0;
                    bool isEvenY = (y / 4) % 2 == 0;
                    Color bgColor = (isEvenX == isEvenY) ? new Color(0.8f, 0.8f, 0.8f, 1) : new Color(0.5f, 0.5f, 0.5f, 1);

                    // 添加当前颜色，Alpha从0到1渐变
                    Color colorWithAlpha = currentColor;
                    colorWithAlpha.a = (float)x / width;

                    // 混合棋盘格和带Alpha的颜色
                    Color finalColor = Color.Lerp(bgColor, currentColor, colorWithAlpha.a);
                    alphaSliderTex.SetPixel(x, y, finalColor);
                }
            }
            alphaSliderTex.Apply();
        }

        /// <summary>
        /// 生成颜色滑块使用的渐变纹理
        /// </summary>
        private void GenerateColorSliderTextures()
        {
            int width = 256;
            int height = 16;

            // 释放已有纹理
            if (redSliderTex != null) DestroyImmediate(redSliderTex);
            if (greenSliderTex != null) DestroyImmediate(greenSliderTex);
            if (blueSliderTex != null) DestroyImmediate(blueSliderTex);
            if (alphaSliderTex != null) DestroyImmediate(alphaSliderTex);

            // 创建红色滑块纹理 - 从当前颜色为基础，只变化R通道
            redSliderTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = new Color((float)x / width, currentColor.g, currentColor.b, 1);
                for (int y = 0; y < height; y++)
                {
                    redSliderTex.SetPixel(x, y, pixelColor);
                }
            }
            redSliderTex.Apply();

            // 创建绿色滑块纹理 - 从当前颜色为基础，只变化G通道
            greenSliderTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = new Color(currentColor.r, (float)x / width, currentColor.b, 1);
                for (int y = 0; y < height; y++)
                {
                    greenSliderTex.SetPixel(x, y, pixelColor);
                }
            }
            greenSliderTex.Apply();

            // 创建蓝色滑块纹理 - 从当前颜色为基础，只变化B通道
            blueSliderTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = new Color(currentColor.r, currentColor.g, (float)x / width, 1);
                for (int y = 0; y < height; y++)
                {
                    blueSliderTex.SetPixel(x, y, pixelColor);
                }
            }
            blueSliderTex.Apply();

            // 创建Alpha滑块纹理 - 特殊处理棋盘格背景
            UpdateAlphaSliderTexture();
        }

        /// <summary>
        /// 更新颜色滑块纹理，当颜色变化时调用
        /// </summary>
        private void UpdateColorSliders()
        {
            // 立即更新所有滑块纹理
            GenerateColorSliderTextures();
            Repaint();
        }

        /// <summary>
        /// 根据当前HSV值更新RGB颜色值和十六进制颜色码
        /// </summary>
        private void UpdateCurrentColor()
        {
            currentColor = Color.HSVToRGB(hue, saturation, brightness);
            currentColor.a = alpha;
            hexColor = ColorUtility.ToHtmlStringRGB(currentColor);

            // 更新滑块纹理
            UpdateColorSliders();
        }

        /// <summary>
        /// 绘制颜色参数复制按钮
        /// </summary>
        private void DrawColorCopyButtons()
        {
            // 准备参数值
            int r255 = Mathf.RoundToInt(currentColor.r * 255);
            int g255 = Mathf.RoundToInt(currentColor.g * 255);
            int b255 = Mathf.RoundToInt(currentColor.b * 255);
            int a255 = Mathf.RoundToInt(alpha * 255);

            float r1 = currentColor.r;
            float g1 = currentColor.g;
            float b1 = currentColor.b;
            float a1 = alpha;

            string hex = "#" + hexColor;

            // 获取可用宽度
            float availableWidth = position.width - 30; // 预留边距和滚动条空间
            float halfWidth = availableWidth / 2 - 5; // 减去两个按钮之间的间隙

            // 准备按钮样式
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.wordWrap = true;
            buttonStyle.fontSize = 10; // 缩小文字大小
            buttonStyle.richText = true; // 启用富文本支持

            // 创建标签样式
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.alignment = TextAnchor.LowerRight;
            labelStyle.normal.textColor = new Color(1.00f, 1.00f, 1.00f);  // 淡灰色
            labelStyle.fontSize = 12;
            labelStyle.richText = true;

            // 创建值文本样式
            GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel);
            valueStyle.alignment = TextAnchor.UpperLeft;
            valueStyle.normal.textColor = new Color(0.42f, 0.42f, 0.42f); // 白色文本
            valueStyle.fontSize = 12; // 调整值文本大小
            valueStyle.richText = true;
            valueStyle.fontStyle = FontStyle.Normal; // 非粗体

            // 十六进制输入和复制按钮 - 使用精确计算的宽度
            float labelWidth = 50; // HEX标签宽度
            float buttonWidth = Mathf.Min(70, availableWidth * 0.25f); // 限制复制按钮的最大宽度
            float textFieldWidth = availableWidth - labelWidth - buttonWidth + 10; // 文本框占据剩余宽度

            // 创建水平布局，不指定宽度让其填充可用空间
            EditorGUILayout.BeginHorizontal();

            // 使用固定宽度的标签
            EditorGUILayout.LabelField("HEX", GUILayout.Width(labelWidth));

            // 文本框使用计算后的宽度
            string newHex = EditorGUILayout.TextField(hexColor, GUILayout.Width(textFieldWidth));

            // HEX复制按钮放在文本框右边，使用计算后的宽度
            if (GUILayout.Button(hex, buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(18)))
            {
                EditorGUIUtility.systemCopyBuffer = hex;
                Debug.Log($"已复制十六进制值: {hex}");
            }
            EditorGUILayout.EndHorizontal();

            // 如果十六进制值发生变化并且有效，更新当前颜色
            if (newHex != hexColor && newHex.Length == 6)
            {
                try
                {
                    if (ColorUtility.TryParseHtmlString("#" + newHex, out Color newColor))
                    {
                        currentColor = newColor;
                        currentColor.a = alpha; // 保持之前的透明度
                        hexColor = newHex;

                        // 更新HSV值
                        Color.RGBToHSV(currentColor, out hue, out saturation, out brightness);

                        // 更新色彩方块和滑块
                        GenerateColorSquare();
                        GenerateColorSliderTextures();
                    }
                }
                catch
                {
                    // 无效的十六进制值，保持原值
                }
            }

            GUILayout.Space(10);

            // 使用表格布局使按钮均匀分布
            EditorGUILayout.BeginVertical();

            // 第一行：RGB(0-255)和RGBA(0-255)
            EditorGUILayout.BeginHorizontal();

            // 注意：互换了标签文本和按钮文本的位置，现在标签文本显示RGB值，按钮文本显示格式描述
            // RGB(0-255)按钮
            DrawButtonWithLabel(
                "<b><i>RGB(0-255)</i></b>",                       // 按钮主文本(显示格式描述)
                $"{r255}, {g255}, {b255}",         // 标签文本(显示实际RGB值)
                () =>
                {                             // 点击回调
                    EditorGUIUtility.systemCopyBuffer = $"{r255}, {g255}, {b255}";
                    Debug.Log($"已复制RGB(0-255): {r255}, {g255}, {b255}");
                },
                halfWidth - 50,                      // 按钮宽度
                25,                                // 按钮高度
                labelStyle,                        // 标签样式
                valueStyle,                        // 值文本样式
                new Vector2(0, 0)                  // 标签偏移(x, y)
            );

            // RGBA(0-255)按钮 
            DrawButtonWithLabel(
                "<b><i>RGBA(0-255)</i></b>",                                // 按钮主文本(显示格式描述)
                $"{r255}, {g255}, {b255}, {a255}",           // 标签文本(显示实际RGBA值)
                () =>
                {                                       // 点击回调
                    EditorGUIUtility.systemCopyBuffer = $"{r255}, {g255}, {b255}, {a255}";
                    Debug.Log($"已复制RGBA(0-255): {r255}, {g255}, {b255}, {a255}");
                },
                halfWidth - 20,                                // 按钮宽度
                25,                                         // 按钮高度
                labelStyle,                                 // 标签样式
                valueStyle,                                 // 值文本样式
                new Vector2(0, 0)                           // 标签偏移(x, y)
            );

            EditorGUILayout.EndHorizontal();

            // 第二行：RGB(0-1)和RGBA(0-1)
            EditorGUILayout.BeginHorizontal();

            // RGB(0-1)按钮
            DrawButtonWithLabel(
                "<b><i>RGB(0-1)</i></b>",                                  // 按钮主文本(显示格式描述)
                $"{r1:F2}f, {g1:F2}f, {b1:F2}f",            // 标签文本(显示实际RGB值)
                () =>
                {                                      // 点击回调
                    EditorGUIUtility.systemCopyBuffer = $"{r1:F2}f, {g1:F2}f, {b1:F2}f";
                    Debug.Log($"已复制RGB(0-1): {r1:F2}f, {g1:F2}f, {b1:F2}f");
                },
                halfWidth - 50,                               // 按钮宽度
                25,                                         // 按钮高度
                labelStyle,                                 // 标签样式
                valueStyle,                                 // 值文本样式
                new Vector2(0, 0)                           // 标签偏移(x, y)
            );

            // RGBA(0-1)按钮
            DrawButtonWithLabel(
                "<b><i>RGBA(0-1)</i></b>",                                              // 按钮主文本(显示格式描述)
                $"{r1:F2}f, {g1:F2}f, {b1:F2}f, {a1:F2}f",               // 标签文本(显示实际RGBA值)
                () =>
                {                                                   // 点击回调
                    EditorGUIUtility.systemCopyBuffer = $"{r1:F2}f, {g1:F2}f, {b1:F2}f, {a1:F2}f";
                    Debug.Log($"已复制RGBA(0-1): {r1:F2}f, {g1:F2}f, {b1:F2}f, {a1:F2}f");
                },
                halfWidth - 20,                                            // 按钮宽度
                25,                                                      // 按钮高度
                labelStyle,                                              // 标签样式
                valueStyle,                                              // 值文本样式
                new Vector2(0, 0)                                        // 标签偏移(x, y)
            );

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制带有自定义标签的按钮
        /// </summary>
        /// <param name="buttonText">按钮主文本</param>
        /// <param name="labelText">标签文本</param>
        /// <param name="onClick">点击回调</param>
        /// <param name="width">按钮宽度</param>
        /// <param name="height">按钮高度</param>
        /// <param name="labelStyle">标签样式</param>
        /// <param name="valueStyle">值文本样式</param>
        /// <param name="labelOffset">标签偏移</param>
        private void DrawButtonWithLabel(string buttonText, string labelText, System.Action onClick, float width, float height, GUIStyle labelStyle, GUIStyle valueStyle, Vector2 labelOffset)
        {
            // 保留按钮绘制的位置
            Rect buttonRect = GUILayoutUtility.GetRect(width, height);

            // 计算标签位置 - 可以根据需要进行调整
            Rect labelRect = new Rect(
                buttonRect.x + labelOffset.x,
                buttonRect.y + labelOffset.y,
                buttonRect.width,
                25  // 标签高度
            );

            // 创建按钮样式
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            // 绘制按钮 - 主要文本放在下半部分
            bool clicked = GUI.Button(buttonRect, "");

            // 计算值文本的位置 - 可根据需要调整
            Rect valueTextRect = new Rect(
                buttonRect.x,
                buttonRect.y, // 位置稍微调整
                buttonRect.width,
                buttonRect.height
            );

            // 使用传入的样式绘制值文本
            GUI.Label(valueTextRect, buttonText, valueStyle);

            // 绘制标签文本
            GUI.Label(labelRect, labelText, labelStyle);

            // 处理按钮点击事件
            if (clicked && onClick != null)
            {
                onClick();
            }
        }

        /// <summary>
        /// 绘制带有自定义标签的按钮 (兼容旧版本调用)
        /// </summary>
        private void DrawButtonWithLabel(string buttonText, string labelText, System.Action onClick, float width, float height, GUIStyle labelStyle, Vector2 labelOffset)
        {
            // 创建默认的值文本样式
            GUIStyle defaultValueStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);

            // 调用新版本方法
            DrawButtonWithLabel(buttonText, labelText, onClick, width, height, labelStyle, defaultValueStyle, labelOffset);
        }

        /// <summary>
        /// 绘制颜色预设区域，显示保存的颜色
        /// </summary>
        private void DrawColorPresets()
        {
            // 获取可用宽度
            float availableWidth = position.width - 30; // 预留边距和滚动条空间

            // 使用折叠面板显示预设颜色
            showPresets = EditorGUILayout.Foldout(showPresets, "颜色预设");

            if (showPresets)
            {
                EditorGUILayout.BeginVertical("box", GUILayout.Width(availableWidth));

                // 添加颜色按钮
                EditorGUILayout.BeginHorizontal(GUILayout.Width(availableWidth));
                if (GUILayout.Button("添加到预设", GUILayout.Width(Mathf.Min(80, availableWidth * 0.3f))))
                {
                    AddColorPreset(currentColor);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // 绘制预设颜色 - 一排5个，自适应宽度
                float swatchSize = Mathf.Min(20, (availableWidth - 40) / MAX_PRESETS); // 确保色块之间有间距
                float padding = Mathf.Min(4, availableWidth * 0.01f);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                Color[] presetArray = colorPresets.ToArray();
                for (int i = 0; i < presetArray.Length; i++)
                {
                    Color presetColor = presetArray[i];

                    // 创建方形色块
                    Rect swatchRect = GUILayoutUtility.GetRect(swatchSize, swatchSize, GUILayout.Width(swatchSize), GUILayout.Height(swatchSize));

                    // 绘制颜色样块
                    EditorGUI.DrawRect(swatchRect, presetColor);

                    // 处理点击事件
                    if (UnityEngine.Event.current.type == EventType.MouseDown && swatchRect.Contains(UnityEngine.Event.current.mousePosition))
                    {
                        if (UnityEngine.Event.current.button == 0)  // 左键点击选择颜色
                        {
                            currentColor = presetColor;
                            Color.RGBToHSV(currentColor, out hue, out saturation, out brightness);
                            alpha = currentColor.a;
                            hexColor = ColorUtility.ToHtmlStringRGB(currentColor);
                            GenerateColorSquare();
                            UnityEngine.Event.current.Use();
                        }
                    }

                    // 在每个色块后添加一点间隔
                    GUILayout.Space(padding);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 添加当前颜色到预设队列，如果队列已满则移除最旧的颜色
        /// </summary>
        /// <param name="color">要添加的颜色</param>
        private void AddColorPreset(Color color)
        {
            // 检查颜色是否已存在于预设中
            foreach (Color existingColor in colorPresets)
            {
                // 比较RGB值是否接近
                if (Mathf.Abs(existingColor.r - color.r) < 0.01f &&
                    Mathf.Abs(existingColor.g - color.g) < 0.01f &&
                    Mathf.Abs(existingColor.b - color.b) < 0.01f &&
                    Mathf.Abs(existingColor.a - color.a) < 0.01f)
                {
                    // 颜色已存在，不需要添加
                    return;
                }
            }

            // 如果队列已满，移除最旧的颜色
            if (colorPresets.Count >= MAX_PRESETS)
            {
                colorPresets.Dequeue();
            }

            // 添加新颜色
            colorPresets.Enqueue(color);
            SaveColorPresets();
        }

        /// <summary>
        /// 生成环形色轮纹理
        /// </summary>
        private void GenerateColorWheel()
        {
            // 增加纹理尺寸以获得更高的分辨率和更平滑的边缘
            int size = 512;

            // 释放旧纹理以防止内存泄漏
            if (colorWheel != null)
                DestroyImmediate(colorWheel);

            colorWheel = new Texture2D(size, size, TextureFormat.RGBA32, true); // 使用mipmap
            colorWheel.filterMode = FilterMode.Trilinear; // 使用三线性过滤
            colorWheel.anisoLevel = 9; // 增加各向异性过滤级别

            // 为确保完整圆形，稍微减小半径
            Vector2 center = new Vector2(size / 2, size / 2);
            float outerRadius = (size / 2) - 2; // 减小2像素确保完整绘制
            float innerRadius = outerRadius * 0.8f;
            float smoothingRange = 2.0f; // 边缘平滑过渡范围

            // 先将所有像素设置为透明
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0, 0, 0, 0);
            }
            colorWheel.SetPixels(pixels);

            // 逐像素生成色轮
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 注意：Unity纹理坐标系y轴是从底部向上的
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);

                    // 确保不超出边界
                    if (distance > outerRadius + smoothingRange)
                        continue;

                    // 计算色相（与距离无关，只与角度有关）
                    Vector2 direction = pos - center;
                    float angle = Mathf.Atan2(direction.y, direction.x);
                    float h = (angle / (2 * Mathf.PI));
                    if (h < 0) h += 1;

                    // 使用最大饱和度和亮度
                    Color pixelColor = Color.HSVToRGB(h, 1f, 1f);

                    // 应用抗锯齿边缘
                    if (distance <= outerRadius && distance >= innerRadius)
                    {
                        // 在环形区域内使用完全不透明
                        pixelColor.a = 1.0f;
                    }
                    else if (distance < innerRadius && distance >= innerRadius - smoothingRange)
                    {
                        // 内边缘平滑过渡
                        float t = (distance - (innerRadius - smoothingRange)) / smoothingRange;
                        pixelColor.a = Mathf.SmoothStep(0, 1, t);
                    }
                    else if (distance > outerRadius && distance <= outerRadius + smoothingRange)
                    {
                        // 外边缘平滑过渡
                        float t = 1.0f - ((distance - outerRadius) / smoothingRange);
                        pixelColor.a = Mathf.SmoothStep(0, 1, t);
                    }
                    else
                    {
                        // 环形外部完全透明
                        continue; // 跳过已经初始化为透明的像素
                    }

                    colorWheel.SetPixel(x, y, pixelColor);
                }
            }

            // 应用更改
            colorWheel.Apply(true); // 生成mipmap
        }

        /// <summary>
        /// 生成色彩饱和度方块纹理，根据当前色相值
        /// </summary>
        private void GenerateColorSquare()
        {
            int size = 256; // 增加尺寸以获得更平滑的效果

            if (colorSquare != null)
                DestroyImmediate(colorSquare);

            colorSquare = new Texture2D(size, size, TextureFormat.RGBA32, true);
            colorSquare.filterMode = FilterMode.Bilinear;

            // 使用当前色相生成色彩/饱和度矩形
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float s = (float)x / size;
                    float v = (float)y / size;  // 亮度从上到下增加，在Unity纹理坐标系中会显示为从上到下递减
                    Color pixelColor = Color.HSVToRGB(hue, s, v);
                    colorSquare.SetPixel(x, y, pixelColor);
                }
            }

            colorSquare.Apply(true);
        }

        /// <summary>
        /// 生成色轮上使用的选择器指示器纹理
        /// </summary>
        private void GenerateColorWheelThumb()
        {
            int size = 64; // 增加尺寸以获得更平滑的效果

            if (colorWheelThumb != null)
                DestroyImmediate(colorWheelThumb);

            colorWheelThumb = new Texture2D(size, size, TextureFormat.RGBA32, true);
            colorWheelThumb.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2, size / 2);
            float radius = size / 2;
            float outlineWidth = size / 16f; // 边框宽度

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);

                    if (distance <= radius)
                    {
                        // 平滑的白色边框，透明中心
                        if (distance > radius - outlineWidth)
                        {
                            // 外边缘羽化
                            float alpha = 1.0f - Mathf.SmoothStep(radius - outlineWidth, radius, distance);
                            colorWheelThumb.SetPixel(x, y, new Color(1, 1, 1, alpha));
                        }
                        else
                        {
                            colorWheelThumb.SetPixel(x, y, new Color(0, 0, 0, 0));
                        }
                    }
                    else
                    {
                        colorWheelThumb.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            colorWheelThumb.Apply(true);
        }

        /// <summary>
        /// 生成色彩方块上使用的选择器指示器纹理
        /// </summary>
        private void GenerateSquareThumb()
        {
            int size = 32; // 增加尺寸以获得更平滑的效果

            if (squareThumb != null)
                DestroyImmediate(squareThumb);

            squareThumb = new Texture2D(size, size, TextureFormat.RGBA32, true);
            squareThumb.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2, size / 2);
            float radius = size / 2;
            float outlineWidth = size / 10f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);

                    if (distance <= radius)
                    {
                        if (distance > radius - outlineWidth)
                        {
                            // 外边缘羽化
                            float alpha = 1.0f - Mathf.SmoothStep(radius - outlineWidth, radius, distance);
                            squareThumb.SetPixel(x, y, new Color(1, 1, 1, alpha));
                        }
                        else
                        {
                            // 内部区域半透明黑色
                            squareThumb.SetPixel(x, y, new Color(0, 0, 0, 0.3f));
                        }
                    }
                    else
                    {
                        squareThumb.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            squareThumb.Apply(true);
        }

        /// <summary>
        /// 从EditorPrefs加载保存的颜色预设
        /// </summary>
        private void LoadColorPresets()
        {
            colorPresets.Clear();

            // 从EditorPrefs加载预设颜色
            string presetData = EditorPrefs.GetString("CW_ColorPalette_Presets", "");
            if (!string.IsNullOrEmpty(presetData))
            {
                string[] colorStrings = presetData.Split(';');
                foreach (string colorStr in colorStrings)
                {
                    if (colorStr.Length > 0)
                    {
                        string[] components = colorStr.Split(',');
                        if (components.Length >= 4 &&
                            float.TryParse(components[0], out float r) &&
                            float.TryParse(components[1], out float g) &&
                            float.TryParse(components[2], out float b) &&
                            float.TryParse(components[3], out float a))
                        {
                            // 添加到队列，但限制最大数量
                            if (colorPresets.Count < MAX_PRESETS)
                                colorPresets.Enqueue(new Color(r, g, b, a));
                        }
                    }
                }
            }

            // 如果没有预设，添加一些默认颜色
            if (colorPresets.Count == 0)
            {
                AddColorPreset(Color.red);
                AddColorPreset(Color.green);
                AddColorPreset(Color.blue);
                AddColorPreset(Color.yellow);
                AddColorPreset(Color.white);
            }
        }

        /// <summary>
        /// 将当前颜色预设保存到EditorPrefs
        /// </summary>
        private void SaveColorPresets()
        {
            // 将预设颜色保存到EditorPrefs
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (Color color in colorPresets)
            {
                if (sb.Length > 0) sb.Append(";");
                sb.Append($"{color.r},{color.g},{color.b},{color.a}");
            }

            EditorPrefs.SetString("CW_ColorPalette_Presets", sb.ToString());
        }

        /// <summary>
        /// 绘制自定义纹理滑块控件
        /// </summary>
        /// <param name="value">当前值（0-255范围）</param>
        /// <param name="bgTexture">滑块背景纹理</param>
        /// <param name="sliderStyle">滑块样式</param>
        /// <param name="thumbStyle">滑块拇指样式</param>
        /// <param name="sliderID">唯一标识此滑块的ID</param>
        /// <returns>新的值（0-255范围）</returns>
        private int DrawColorSlider(int value, Texture2D bgTexture, GUIStyle sliderStyle, GUIStyle thumbStyle, int sliderID)
        {
            // 转换为0-1范围用于滑块
            float normalizedValue = value / 255f;

            // 使用背景纹理绘制滑块
            Rect sliderRect = GUILayoutUtility.GetRect(GUIContent.none, sliderStyle, GUILayout.Height(16));

            // 绘制边框
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            GUI.DrawTexture(new Rect(sliderRect.x - 1, sliderRect.y - 1, sliderRect.width + 2, sliderRect.height + 2), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            // 绘制背景
            GUI.DrawTexture(sliderRect, bgTexture);

            // 绘制高亮边框
            Color borderColor = EditorGUIUtility.isProSkin
                ? new Color(0.42f, 0.42f, 0.42f, 1.0f)
                : new Color(0.6f, 0.6f, 0.6f, 1.0f);

            // 左右边框
            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(sliderRect.x - 1, sliderRect.y, 1, sliderRect.height), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(sliderRect.x + sliderRect.width, sliderRect.y, 1, sliderRect.height), EditorGUIUtility.whiteTexture);

            // 顶部边框
            GUI.DrawTexture(new Rect(sliderRect.x, sliderRect.y - 1, sliderRect.width, 1), EditorGUIUtility.whiteTexture);

            // 底部边框
            GUI.DrawTexture(new Rect(sliderRect.x, sliderRect.y + sliderRect.height, sliderRect.width, 1), EditorGUIUtility.whiteTexture);

            GUI.color = Color.white;

            // 处理鼠标事件
            float newValue = normalizedValue;
            UnityEngine.Event current = UnityEngine.Event.current;

            // 如果当前正在拖动此滑块，使用存储的值
            if (isDragging && activeSliderID == sliderID)
            {
                newValue = activeSliderValue;
            }
            // 否则检查是否应开始拖动此滑块
            else if (!isDragging && activeSliderID == -1 && current.type == EventType.MouseDown && current.button == 0 && sliderRect.Contains(current.mousePosition))
            {
                // 激活此滑块并保存其信息
                activeSliderID = sliderID;
                activeSliderRect = sliderRect;
                lastMousePosition = current.mousePosition;
                isDragging = true;

                // 计算初始值并存储
                newValue = Mathf.Clamp01((current.mousePosition.x - sliderRect.x) / sliderRect.width);
                activeSliderValue = newValue;

                // 立即更新颜色以反映滑块变化
                UpdateColorFromSliderValue();

                // 标记GUI发生变化并使用事件
                GUI.changed = true;
                current.Use();
            }

            // 绘制自定义滑块thumb
            float thumbX = sliderRect.x + newValue * sliderRect.width - 3;
            Rect thumbRect = new Rect(thumbX, sliderRect.y - 1, 4, sliderRect.height + 2);

            // 绘制thumb阴影
            Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
            GUI.color = shadowColor;
            GUI.DrawTexture(new Rect(thumbRect.x + 1, thumbRect.y + 1, thumbRect.width, thumbRect.height), EditorGUIUtility.whiteTexture);

            // 绘制thumb主体
            Color thumbColor = EditorGUIUtility.isProSkin
                ? new Color(0.92f, 0.92f, 0.92f, 1.0f)
                : new Color(0.92f, 0.92f, 0.92f, 1.0f);

            GUI.color = thumbColor;
            GUI.DrawTexture(thumbRect, EditorGUIUtility.whiteTexture);

            // 绘制thumb边框
            Color thumbBorderColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            GUI.color = thumbBorderColor;

            // 绘制边框
            GUI.DrawTexture(new Rect(thumbRect.x, thumbRect.y, 1, thumbRect.height), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(thumbRect.x + thumbRect.width - 1, thumbRect.y, 1, thumbRect.height), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(thumbRect.x, thumbRect.y, thumbRect.width, 1), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(thumbRect.x, thumbRect.y + thumbRect.height - 1, thumbRect.width, 1), EditorGUIUtility.whiteTexture);

            // 恢复颜色
            GUI.color = Color.white;

            // 转换回0-255范围并返回
            return Mathf.RoundToInt(newValue * 255f);
        }
    }
}