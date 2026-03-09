using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHD.Editor
{
    public static class EditorPath
    {
        #region Common

        public const string EDITOR_SheetImporter_PATH = "Assets/Scripts/ProjectHD/Editor/ToolWindow/ScriptableObject/SheetImporter.asset";
        public const string EDITOR_INTERGRATEDTOOLSETTING_PATH = "Assets/Scripts/ProjectHD/Editor/ToolWindow/ScriptableObject/IntegratedToolSetting.asset";
        public const string EDITOR_AUTOADDRESSABLE_DEFAULTSETTING_PATH = "Assets/Scripts/ProjectHD/Editor/Addressable/AutomationAddressable/ScriptableObject/[Origin]BaseAddressableSetting.asset";

        #endregion

        #region DS
        public const string EDITOR_DIALOG_DATA_PATH = "Assets/Core/Editor/UnityNode/Dialog/SCO/DialogEditorData.asset";
        public const string EDITOR_DIALOG_CREATE_NEW_EVENT = "Assets/Editor/UnityNode/Dialog/XML/CreateNewDialogEvent.uxml";

        //Style
        public const string EDITOR_DIALOG_GRAPHVIEW_STYLESHEET_PATH = "Assets/Core/Editor/UnityNode/Dialog/Styles/DSGraphViewStyles.uss";
        public const string EDITOR_DIALOG_TOOLBAR_PATH = "Assets/Core/Editor/UnityNode/Dialog/Styles/DSToolbarStyles.uss";
        public const string EDITOR_DIALOG_NODE_STYLESHEET_PATH = "Assets/Core/Editor/UnityNode/Dialog/Styles/DSNodeStyles.uss";

        #endregion

        #region Dialog
        public const string EDITOR_DIALOG_EDITOR_SETTINGS_PATH = "Assets/Core/Editor/UnityNode/Dialog/SCO/DialogEditorSettings.asset";
        #endregion

    }

    public static class EditorEnum
    {
        public enum EditorPlayerPrefsType
        {
            None = 0,
            EditorAccountId = 1,
        }
    }

    public static class EditorConst
    {
        #region DialogSystem
        public const string EDITOR_DIALOG_HIDDEN = "숨기기";
        public const string EDITOR_DIALOG_End_STRING = "End";
        public const string EDITOR_DIALOG_PREVNODE_STRING = "Prev_Node";
        public const string EDITOR_DIALOG_NULL_STRING = "NULL";

        public const int EDITOR_DIALOG_DEFAULT_CONST = -1;

        public const string EDITOR_DIALOG_MENUITEM = "CustomTools/DialogEditorWindow";
        public const string EDITOR_DIALOG_WINDOW_NAME = "DialogEditorWindow";

        //pref Save variable name
        public const string EDITOR_DIALOG_SHOW_EVENT_LIST_KEY = "DialogEventListKey";
        public const string EDITOR_DIALOG_SHOW_EVENT_DATA_KEY = "DialogEventDataKey";
        public const string EDITOR_DIALOG_LEFT_PANEL_WIDTH_KEY = "DialogLeftPanelWidthKey";
        public const string EDITOR_DIALOG_EVENT_LIST_WIDTH_KEY = "DialogEventListWidthKey";
        public const string EDITOR_DIALOG_EVENT_DATA_WIDTH_KEY = "DialogEventDataWidthKey";

        public static Color defaultBackgroundColor = new Color(29f / 255f, 29f / 255f, 30f / 255f);
        public static Color defaultErrorBackgroundColor = new Color(0.8f, 0.3f, 0.3f);
        public static Color defaultButtonBackgroundColor = new Color(60f / 255f, 60f / 255f, 60f / 255f);
        #endregion

        #region IntegratedTool
        public const string EDITOR_INTERGRATED_WINDOW_NAME = "종합툴";


        #endregion
    }
}