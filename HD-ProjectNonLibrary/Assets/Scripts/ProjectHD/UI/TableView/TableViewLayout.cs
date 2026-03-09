using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UITool;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UITool
{
    public class TableViewLayout : TableView
    {
        [SerializeField]
        protected TextAnchor defaultAnchor = TextAnchor.MiddleCenter;


        public override void ReLoad(bool initPos = true)
        {
            base.ReLoad(initPos);
            switch (tableType)
            {
                case eTableViewType.Vertical:
                    {
                        if (visibleHeight < GetTotalHeight())
                        {
                            scrollView.enabled = true;
                            contentTransform.sizeDelta = new Vector2(0, GetTotalHeight());
                            var layoutGroup = contentTransform.GetComponent<LayoutGroup>();
                            if (layoutGroup != null)
                                layoutGroup.enabled = false;
                        }
                        else
                        {
                            scrollView.enabled = false;
                            ItemTemplate.SetActive(false);
                            var layout = contentTransform.GetComponent<VerticalLayoutGroup>();
                            if (layout == null)
                                layout = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
                            contentTransform.sizeDelta = new Vector2(0, visibleHeight);
                            layout.padding.top = (int)paddingY;
                            layout.padding.bottom = (int)paddingY;
                            layout.padding.right = (int)paddingX;
                            layout.padding.left = (int)paddingX;
                            layout.spacing = spaceingY;
                            layout.childAlignment = defaultAnchor;
                            layout.enabled = true;
                        }
                    }
                    break;
                case eTableViewType.Horizontal:
                default:
                    {
                        if (visibleWidth < GetTotalWidth())
                        {
                            scrollView.enabled = true;
                            contentTransform.sizeDelta = new Vector2(GetTotalWidth(), 0);
                            var layoutGroup = contentTransform.GetComponent<LayoutGroup>();
                            if (layoutGroup != null)
                                layoutGroup.enabled = false;
                        }
                        else
                        {
                            scrollView.enabled = false;
                            ItemTemplate.SetActive(false);
                            var layout = contentTransform.GetComponent<HorizontalLayoutGroup>();
                            if (layout == null)
                                layout = contentTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
                            contentTransform.sizeDelta = new Vector2(visibleWidth, 0);
                            layout.padding.top = (int)paddingY;
                            layout.padding.bottom = (int)paddingY;
                            layout.padding.right = (int)paddingX;
                            layout.padding.left = (int)paddingX;
                            layout.spacing = spaceingX;
                            layout.childAlignment = defaultAnchor;
                            layout.enabled = true;
                        }
                    }
                    break;
            }
        }
    }
}
