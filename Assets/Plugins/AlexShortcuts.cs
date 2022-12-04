using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/*
Introduction:[By Alexander]
To create a hotkey you can use the 
following special characters: 
% (ctrl on Windows, cmd on macOS), # (shift), & (alt).
If no special modifier key combinations are required 
the key can be given after an underscore. For example
to create a menu with hotkey shift-alt-g use 
"MyMenu/Do Something #&g". To create a menu with
hotkey g and no key modifiers pressed use "MyMenu/Do Something _g".

Some special keyboard keys are supported as hotkeys,
for example "#LEFT" would map to shift-left. The keys 
supported like this are: LEFT, RIGHT, UP, DOWN, F1 .. F12, HOME, END, PGUP, PGDN.

A hotkey text must be preceded with a space character
("MyMenu/Do_g" won't be interpreted as hotkey, while "MyMenu/Do _g" will).
*/
public static class AlexShortcuts
{
#if UNITY_EDITOR
    [MenuItem("Tools/Clear Console %#x")] // Ctrl + SHIFT + X
    public static void ClearConsole()
    {
        var assembly = Assembly.GetAssembly(typeof(ActiveEditorTracker));
        var type = assembly.GetType("UnityEditorInternal.LogEntries");
        if (type == null)
        {
            type = assembly.GetType("UnityEditor.LogEntries");
        }
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null); //Reflect this method
    }

    /// /// /// ///

    [MenuItem("GameObject/UGUI/Image %#i")]// Ctrl + SHIFT + I
    static void CreateImage(MenuCommand menuCommand)
    {
        GameObject selectedObj = CheckSelection(menuCommand);
        if (selectedObj == null)
        {
            Debug.Log("UI object must be selected for creating image.");
            return;
        }
        GameObject go = new GameObject("Image");
        GameObjectUtility.SetParentAndAlign(go, selectedObj);
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
        go.AddComponent<Image>();
    }

    private static GameObject CheckSelection(MenuCommand menuCommand)
    {
        GameObject selectedObj = menuCommand.context as GameObject;
        //若当前不是右键点击物体的操作则看当前选中的物体的情况
        if (selectedObj == null)
            selectedObj = Selection.activeGameObject;
        //当前没有选中物体或者选中的物体不在Canvas之下则返回空，按键不响应。（当然也可以不要求存在Canvas，没有时则先创建一个新的Canvas）
        if (selectedObj == null || selectedObj != null && selectedObj.GetComponentInParent<Canvas>() == null)
        {
            return null;
        }
        Debug.Log("Created image for the selected object！");
        return selectedObj;
    }

    /// /// /// ///

    /*
        Command/Ctrl-E: Find source asset for one selected object // for prefabs!!!
        Command/Ctrl-H: Hide/show selected objects // for prefabs!!!
        Command/Ctrl-F1: Toggle between top/down views
        Command/Ctrl-F2: Toggle between left/right views
        Command/Ctrl-F3: Toggle between front/back views
        Command/Ctrl-`: Toggle between perspective/orthogonal views
        Command/Ctrl-I: Toggle lock of first inspector window
    */
    const string FindSourceAssetString = "GameObject/Find Source Asset %e"; // command / ctrl - e
    const string HideSelectionString = "GameObject/Hide Selection %h"; // command / ctrl - h
    const string ViewTopDownString = "View/Toggle View Top-Down %F1"; // command / ctrl - F1 
    const string ViewLeftRightString = "View/Toggle View Left-Right %F2"; // command / ctrl - F2 
    const string ViewFrontBackString = "View/Toggle View Front-Back %F3"; // command / ctrl - F3 
    const string ToggleOrthogonalString = "View/Toggle View Perspective-Orthogonal %`"; // command / ctrl - ` 
    const string LockInspectorString = "View/Toggle Inspector Lock %i"; // command / ctrl - i 

    // Hide/show selected objects
    #region Hide Selection
    [MenuItem(HideSelectionString, true)]
    public static bool ValidateHideSelection()
    {
        int hidden_objects = 0;
        int shown_objects = 0;

        foreach (var obj in Selection.gameObjects)
        {
            if (obj.activeSelf)
                shown_objects++;
            else
                hidden_objects++;
        }

        return (hidden_objects != 0 || shown_objects != 0);
    }

    [MenuItem(HideSelectionString)]
    static void HideSelection()
    {
        int hidden_objects = 0;
        int shown_objects = 0;

        foreach (var obj in Selection.gameObjects)
        {
            if (obj.activeSelf)
                shown_objects++;
            else
                hidden_objects++;
        }

        if (hidden_objects == 0 && shown_objects == 0)
            return;

        bool toggle = (hidden_objects != 0);
        foreach (var obj in Selection.gameObjects)
        {
            obj.SetActive(toggle);
        }
    }
    #endregion

    // Find the source asset in the project that is currently selected
    #region Find Source Asset
    [MenuItem(FindSourceAssetString, true)]
    public static bool ValidateFindSourceAsset()
    {
        var gameObject = Selection.activeGameObject;
        if (gameObject == null) return false;

        if (PrefabUtility.GetPrefabParent(gameObject) != null) return true;
        var meshFilter = gameObject.GetComponent<MeshFilter>();

        return (meshFilter != null && meshFilter.sharedMesh != null && AssetDatabase.Contains(meshFilter.sharedMesh));
    }

    [MenuItem(FindSourceAssetString)]
    public static void FindSourceAsset()
    {
        var gameObject = Selection.activeGameObject;
        if (gameObject == null)
            return;

        var parent = PrefabUtility.GetPrefabParent(gameObject);
        if (parent != null)
        {
            Selection.activeObject = parent;
            EditorGUIUtility.PingObject(gameObject);
            return;
        }

        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            return;

        var mesh = meshFilter.sharedMesh;
        if (mesh == null)
            return;

        Selection.activeObject = mesh;
    }
    #endregion

    // Toggle between top and down view 
    #region Set View Top-Down
    [MenuItem(ViewTopDownString, true)]
    public static bool ValidateSetViewTopDown() { return SceneView.lastActiveSceneView != null; }

    [MenuItem(ViewTopDownString)]
    public static void SetViewTopDown()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null)
            return;

        Quaternion topDirection = kDirectionRotations[1];
        Quaternion downDirection = kDirectionRotations[4];
        ToggleBetweenViewDirections(view, topDirection, downDirection);
    }
    #endregion

    // Toggle between left and right view 
    #region Set View Left-Right
    [MenuItem(ViewLeftRightString, true)]
    public static bool ValidateSetViewLeftRight() { return SceneView.lastActiveSceneView != null; }

    [MenuItem(ViewLeftRightString)]
    public static void SetViewLeftRight()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null)
            return;

        Quaternion leftDirection = kDirectionRotations[3];
        Quaternion rightDirection = kDirectionRotations[0];
        ToggleBetweenViewDirections(view, leftDirection, rightDirection);
    }
    #endregion

    // Toggle between front and back view 
    #region Set View Front-Back
    [MenuItem(ViewFrontBackString, true)]
    public static bool ValidateSetFrontBack() { return SceneView.lastActiveSceneView != null; }

    [MenuItem(ViewFrontBackString)]
    public static void SetViewFrontBack()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null)
            return;

        Quaternion frontDirection = kDirectionRotations[2];
        Quaternion backDirection = kDirectionRotations[5];
        ToggleBetweenViewDirections(view, frontDirection, backDirection);
    }
    #endregion

    // Toggle between perspective and orthogonal view 
    #region Toggle Orthogonal
    [MenuItem(ToggleOrthogonalString, true)]
    public static bool ValidateToggleOrthogonal() { return SceneView.lastActiveSceneView != null; }

    [MenuItem(ToggleOrthogonalString)]
    public static void ToggleOrthogonal()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null)
            return;

        view.LookAt(view.pivot, view.rotation, view.size, !view.orthographic);
    }
    #endregion

    // Toggle the inspector lock of the first inspector window
    #region Toggle Inspector Lock
    static void InitToggleInspectorLock()
    {
        inspectorIsLockedPropertyInfo = null;
        inspectorType = System.Reflection.Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.InspectorWindow");
        if (inspectorType != null)
        {
            inspectorIsLockedPropertyInfo = inspectorType.GetProperty("isLocked");
        }
        inspectorInitialized = true;
    }

    static bool inspectorInitialized = false;
    static System.Type inspectorType;
    static System.Reflection.PropertyInfo inspectorIsLockedPropertyInfo;
    [MenuItem(LockInspectorString, true)]
    static bool ValidateToggleInspectorLock()
    {
        if (!inspectorInitialized)
        {
            InitToggleInspectorLock();
        }
        if (inspectorIsLockedPropertyInfo == null)
            return false;
        var allInspectors = Resources.FindObjectsOfTypeAll(inspectorType);
        if (allInspectors.Length == 0)
            return false;
        var inspector = allInspectors[allInspectors.Length - 1] as EditorWindow;
        if (inspector == null)
            return false;
        return true;
    }

    [MenuItem(LockInspectorString)]
    static void ToggleInspectorLock()
    {
        if (!inspectorInitialized)
        {
            InitToggleInspectorLock();
        }
        if (inspectorIsLockedPropertyInfo == null)
            return;
        var allInspectors = Resources.FindObjectsOfTypeAll(inspectorType);
        if (allInspectors.Length == 0)
            return;
        var inspector = allInspectors[allInspectors.Length - 1] as EditorWindow;
        if (inspector == null)
            return;

        var value = (bool)inspectorIsLockedPropertyInfo.GetValue(inspector, null);
        inspectorIsLockedPropertyInfo.SetValue(inspector, !value, null);
        inspector.Repaint();
    }
    #endregion

    #region Toggle Between View Directions (helper function)
    static readonly Quaternion[] kDirectionRotations = {
        Quaternion.LookRotation (new Vector3 (-1, 0, 0)), // right
        Quaternion.LookRotation (new Vector3 (0, -1, 0)), // top
        Quaternion.LookRotation (new Vector3 (0, 0, -1)), // front
        Quaternion.LookRotation (new Vector3 (1, 0, 0)), // left
        Quaternion.LookRotation (new Vector3 (0, 1, 0)), // down
        Quaternion.LookRotation (new Vector3 (0, 0, 1)), // back
    };

    const float kCompareEpsilon = 0.0001f;
    static void ToggleBetweenViewDirections(SceneView view, Quaternion primaryDirection, Quaternion alternativeDirection)
    {
        Vector3 direction = primaryDirection * Vector3.forward;
        float dot = Vector3.Dot(view.camera.transform.forward, direction);
        if (dot < 1.0f - kCompareEpsilon) { view.LookAt(view.pivot, primaryDirection, view.size, view.orthographic); } else { view.LookAt(view.pivot, alternativeDirection, view.size, view.orthographic); }
    }
    #endregion

    /// /// /// ///

    /*
    Alt + ↑ :	        Move Hierarchy's order up.
    Alt + ↓ : 	        Move Hierarchy's order down.
    Alt + Shift + C :	Clear console.  ========= Same has been done above========
    Alt + C : 	        Copy Transform value.
    Alt + V :	        Paste Transform value.
    Alt + E :	        Deselect.
    Alt + D :	        Duplicate game object. (Suffix is omitted.)
    Alt + A :	        Toggle active. (Same as Shift + Alt + A)  ========= Same has been done above========
    Alt + R :	        Remove the suffix of the sequential number from the name of the object.
    Alt + L :	        Toggle Inspector lock.   ========= Same has been done above========
    Alt + K :	        Switch Inspector to debug mode.
    F5 :            	Play
    F6 :         	    Stop
    // Shift + F5 : 	    Stop  //Original one
*/
    public static class RunUnity
    {
        private const string ITEM_NAME_RUN = "Edit/Plus/Run _F5";
        private const string ITEM_NAME_STOP = "Edit/Plus/Stop _F6";
        // private const string ITEM_NAME_STOP = "Edit/Plus/Stop #_F5"; // Original one

        [MenuItem(ITEM_NAME_RUN)]
        private static void Run()
        {
            EditorApplication.isPlaying = true;
        }

        [MenuItem(ITEM_NAME_RUN, true)]
        private static bool CanRun()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem(ITEM_NAME_STOP)]
        private static void Stop()
        {
            EditorApplication.isPlaying = false;
        }

        [MenuItem(ITEM_NAME_STOP, true)]
        private static bool CanStop()
        {
            return EditorApplication.isPlaying;
        }
    }

    public static class ToggleDebugMode
    {
        private const string ITEM_NAME = "Edit/Plus/Toggle Inspector Debug &k";

        [MenuItem(ITEM_NAME)]
        private static void Toggle()
        {
            var window = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var inspectorWindow = ArrayUtility.Find(window, c => c.GetType().Name == "InspectorWindow");

            if (inspectorWindow == null) return;

            var inspectorType = inspectorWindow.GetType();
            var tracker = ActiveEditorTracker.sharedTracker;
            var isNormal = tracker.inspectorMode == InspectorMode.Normal;
            var methodName = isNormal ? "SetDebug" : "SetNormal";

            var attr = BindingFlags.NonPublic | BindingFlags.Instance;
            var methodInfo = inspectorType.GetMethod(methodName, attr);
            methodInfo.Invoke(inspectorWindow, null);
            tracker.ForceRebuild();
        }

        [MenuItem(ITEM_NAME, true)]
        private static bool CanToggle()
        {
            var window = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var inspectorWindow = ArrayUtility.Find(window, c => c.GetType().Name == "InspectorWindow");

            return inspectorWindow != null;
        }
    }

    public static class RemoveDuplicatedName
    {
        private const string ITEM_NAME = "Edit/Plus/Remove Duplicated Name &r";

        private static Regex m_regex = new Regex(@"(.*)(\([0-9]*\))");

        [MenuItem(ITEM_NAME)]
        public static void Remove()
        {
            var list = Selection.gameObjects
                .Where(c => m_regex.IsMatch(c.name))
                .ToArray();

            if (list == null || list.Length == 0) return;

            foreach (var n in list)
            {
                Undo.RecordObject(n, "Remove Duplicated Name");
                n.name = m_regex.Replace(n.name, @"$1");
            }
        }

        [MenuItem(ITEM_NAME, true)]
        public static bool CanRemove()
        {
            var gameObjects = Selection.gameObjects;
            return gameObjects != null && 0 < gameObjects.Length;
        }
    }

    public static class LockInspector
    {
        private const string ITEM_NAME = "Edit/Plus/Lock Inspector &l";

        [MenuItem(ITEM_NAME)]
        private static void Lock()
        {
            var tracker = ActiveEditorTracker.sharedTracker;
            tracker.isLocked = !tracker.isLocked;
            tracker.ForceRebuild();
        }

        [MenuItem(ITEM_NAME, true)]
        private static bool CanLock()
        {
            return ActiveEditorTracker.sharedTracker != null;
        }
    }

    public static class InvertActiveGameObject
    {
        private const string ITEM_NAME = "Edit/Plus/Invert Active &a";

        [MenuItem(ITEM_NAME)]
        public static void Invert()
        {
            EditorApplication.ExecuteMenuItem("GameObject/Toggle Active State");
        }

        [MenuItem(ITEM_NAME, true)]
        public static bool CanInvert()
        {
            var gameObjects = Selection.gameObjects;
            return gameObjects != null && 0 < gameObjects.Length;
        }
    }

    public static class DuplicateWithoutSerialNumber
    {
        private const string ITEM_NAME = "Edit/Plus/Duplicate Without Serial Number &d";

        [MenuItem(ITEM_NAME)]
        public static void Duplicate()
        {
            var list = new List<int>();

            foreach (var n in Selection.gameObjects)
            {
                var clone = GameObject.Instantiate(n, n.transform.parent);
                clone.name = n.name;
                list.Add(clone.GetInstanceID());
                Undo.RegisterCreatedObjectUndo(clone, "Duplicate Without Serial Number");
            }

            Selection.instanceIDs = list.ToArray();
            list.Clear();
        }

        [MenuItem(ITEM_NAME, true)]
        public static bool CanDuplicate()
        {
            var gameObjects = Selection.gameObjects;
            return gameObjects != null && 0 < gameObjects.Length;
        }
    }

    public static class DeselectAll
    {
        private const string ITEM_NAME = "Edit/Plus/Deselect All &e";

        [MenuItem(ITEM_NAME)]
        public static void Deselect()
        {
            Selection.objects = new Object[0];
        }

        [MenuItem(ITEM_NAME, true)]
        public static bool CanDeselect()
        {
            var objects = Selection.objects;
            return objects != null && 0 < objects.Length;
        }
    }

    public static class CopyPasteTransform
    {
        private class Data
        {
            public Vector3 m_localPosition;
            public Quaternion m_localRotation;
            public Vector3 m_localScale;

            public Data(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                m_localPosition = localPosition;
                m_localRotation = localRotation;
                m_localScale = localScale;
            }

            public Data(Transform t) : this(t.localPosition, t.localRotation, t.localScale) { }
        }

        private const string ITEM_NAME_COPY = "Edit/Plus/Copy Transform Values &c";
        private const string ITEM_NAME_PASTE = "Edit/Plus/Paste Transform Values &v";

        private static Data m_data;

        [MenuItem(ITEM_NAME_COPY)]
        public static void Copy()
        {
            m_data = new Data(Selection.activeTransform);
        }

        [MenuItem(ITEM_NAME_COPY, true)]
        public static bool CanCopy()
        {
            return Selection.activeTransform != null;
        }

        [MenuItem(ITEM_NAME_PASTE)]
        public static void Paste()
        {
            foreach (var n in Selection.gameObjects)
            {
                var t = n.transform;
                Undo.RecordObject(t, "Paste Transform Values");
                t.localPosition = m_data.m_localPosition;
                t.localRotation = m_data.m_localRotation;
                t.localScale = m_data.m_localScale;
            }
        }

        [MenuItem(ITEM_NAME_PASTE, true)]
        public static bool CanPaste()
        {
            var gameObjects = Selection.gameObjects;
            return m_data != null && gameObjects != null && 0 < gameObjects.Length;
        }
    }

    public static class ClearConsole2
    {
        private const string ITEM_NAME = "Edit/Plus/Clear Console &#c";

        [MenuItem(ITEM_NAME)]
        public static void Invert()
        {
            var type = Assembly
                .GetAssembly(typeof(SceneView))
#if UNITY_2017_1_OR_NEWER
                .GetType("UnityEditor.LogEntries")
#else
                .GetType ("UnityEditorInternal.LogEntries")
#endif
            ;

            var attr = BindingFlags.Static | BindingFlags.Public;
            var method = type.GetMethod("Clear", attr);
            method.Invoke(null, null);
        }
    }

    public static class ChangeSibiling
    {
        private const string ITEM_NAME_UP = "Edit/Plus/Sibiling Up &UP";
        private const string ITEM_NAME_DOWN = "Edit/Plus/Sibiling Down &DOWN";

        [MenuItem(ITEM_NAME_UP)]
        private static void Up()
        {
            var t = Selection.activeTransform;
            t.SetSiblingIndex(t.GetSiblingIndex() - 1);
        }

        [MenuItem(ITEM_NAME_UP, true)]
        private static bool CanUp()
        {
            return Selection.activeTransform != null;
        }

        [MenuItem(ITEM_NAME_DOWN)]
        private static void Down()
        {
            var t = Selection.activeTransform;
            t.SetSiblingIndex(t.GetSiblingIndex() + 1);
        }

        [MenuItem(ITEM_NAME_DOWN, true)]
        private static bool CanDown()
        {
            return Selection.activeTransform != null;
        }
    }





    /// /// /// ///


    [MenuItem("Tools/Reset Transform #t")]
    static void ResetTransform()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length < 1) return;
        Undo.RegisterCompleteObjectUndo(selection, "Zero Position");
        foreach (GameObject go in selection)
        {
            InternalZeroPosition(go);
            InternalZeroRotation(go);
            InternalZeroScale(go);
        }
    }

    [MenuItem("Tools/Reset Name &n")]
    static void ResetName()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length < 1) return;

        Undo.RegisterCompleteObjectUndo(selection, "Reset Name");
        foreach (GameObject go in selection)
        {
            Rename(go);
        }
    }

    private static void Rename(GameObject go)
    {
        int start = go.name.IndexOf("(");
        int end = go.name.IndexOf(")");
        if (start != -1 && end != -1 && start < end)
        {
            go.name = go.name.Substring(0, start);
        }
    }

    private static void InternalZeroPosition(GameObject go)
    {
        go.transform.localPosition = Vector3.zero;
    }

    private static void InternalZeroRotation(GameObject go)
    {
        go.transform.localRotation = Quaternion.Euler(Vector3.zero);
    }

    private static void InternalZeroScale(GameObject go)
    {
        go.transform.localScale = Vector3.one;
    }

#endif
}

/*
////Second type to create shortcuts in UnityEditor 
using System.Reflection;
using UnityEditor;

//LogEntries类是被Internal修饰符修饰的，
//我们当前脚本的程序集与LogEntries不在一个程序集，
//所以如果想要调用它的方法，就需要使用反射。

public static class ShortCuts
{
    [MenuItem("MyTools/ClearConsole _c", false, 1)] // C
    public static void ClearConsole()
    {
        //首先创建程序集实例，
        //SceneView类(Public)和LogEntries都是在UnityEditor程序集中的，
        //所以可以通过SceneView来获取程序集，也可以用以下方式代替
        Assembly assembly = Assembly.GetAssembly(typeof(SceneView));

        //根据程序集实例获取LogEntries类型实例，注意要给LogEntries加上UnityEditor命名空间
        System.Type type = assembly.GetType("UnityEditor.LogEntries");
        MethodInfo method = type.GetMethod("Clear");//获取类型后获取相应的方法
        method.Invoke(new object(), null);
    }
}
*/
