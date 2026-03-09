using DG.Tweening;
using ProjectHD.Data;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Utilities;

namespace UITool
{
    public class TableViewDelegate
    {
        public delegate void ReuseDelegate(GameObject itemNode, IDataKey<int> item);
        public delegate void UnuseDelegate(GameObject itemNode);

        public List<IDataKey<int>> Items = null;
        public ReuseDelegate Reuse = null;
        public UnuseDelegate Unuse = null;

        public TableViewDelegate(List<IDataKey<int>> items, ReuseDelegate reuse = null, UnuseDelegate unuse = null)
        {
            Items = items;
            Reuse = reuse;
            Unuse = unuse;
        }

        public void SetDelegate(List<IDataKey<int>> items, ReuseDelegate reuse = null, UnuseDelegate unuse = null)
        {
            Items = items;
            Reuse = reuse;
            Unuse = unuse;
        }
    }
    public enum eTableViewType
    {
        Vertical,
        Horizontal
    }
    public class TableView : MonoBehaviour
    {

        [Header("Default")]
        [SerializeField]
        protected eTableViewType tableType = eTableViewType.Vertical;
        [SerializeField]
        protected GameObject itemTemplate = null;
        public GameObject ItemTemplate { get { return itemTemplate; } }
        [Space(10f)]
        [Header("Padding")]
        [SerializeField]
        protected float paddingX = 0f;
        [SerializeField]
        protected float paddingY = 0f;
        [Space(10f)]
        [Header("Spaceing")]
        [SerializeField]
        protected float spaceingX = 0f;
        [SerializeField]
        protected float spaceingY = 0f;
        protected ScrollRect scrollView = null;
        protected float itemWidth = 0f;
        protected float itemHeight = 0f;
        protected CustomListPool<GameObject> itemPool = null;
        protected TableViewDelegate viewDelegate = null;
        protected List<IDataKey<int>> dataSource = null;
        protected float visibleWidth = 0f;
        protected float visibleHeight = 0f;
        protected int spawnCount = 0;
        protected Dictionary<int, GameObject> visibleNodes = null;
        protected float lastX = float.MinValue;
        protected float lastY = float.MinValue;
        protected HashSet<int> itemIndex = null;
        protected Vector3 vec3 = Vector3.zero;
        protected RectTransform contentTransform = null;
        protected float contentScaleX = 1f;
        protected float contentScaleY = 1f;

        protected bool isInitialized = false;

        public delegate void DeInitDelegate(GameObject itemNode);
        protected virtual void OnDestroy()
        {
            isInitialized = false;

            if (itemPool != null)
            {
                while (itemPool.Get() != null)
                {
                    continue;
                }
            }

            Utilities.InternalDebug.Log($"테이블 뷰 파괴됨 isInitialized:({isInitialized})");
        }
        /// <summary>
        /// 최초 1회 무조건 호출할 것
        /// At first, must call this 
        /// </summary>
        public virtual void OnStart()
        {
            if (isInitialized)
                return;
            isInitialized = true;

            if (itemPool == null)
            {
                itemPool = new CustomListPool<GameObject>((GameObject item) =>
                {
                    if (item == null)
                        return;
                    if (item.transform.parent != contentTransform)
                    {
                        item.transform.SetParent(contentTransform, false);
                        item.transform.localScale = Vector3.one;
                    }
                    item.SetActive(true);
                }, (GameObject item) =>
                {
                    if (item == null)
                        return;

                    item.SetActive(false);
                    if (item.transform.parent != contentTransform)
                    {
                        item.transform.SetParent(contentTransform, false);
                        item.transform.localScale = Vector3.one;
                    }
                    //item.transform.SetParent(null, false);
                });
            }

            if (dataSource == null)
                dataSource = new List<IDataKey<int>>();
            if (visibleNodes == null)
                visibleNodes = new Dictionary<int, GameObject>();
            if (itemIndex == null)
                itemIndex = new HashSet<int>();

            scrollView = GetComponent<ScrollRect>();
            if (scrollView != null)
            {
                var scrollViewRect = scrollView.viewport.GetComponent<RectTransform>();
                if (scrollViewRect != null)
                {
                    visibleWidth = scrollViewRect.rect.width;
                    visibleHeight = scrollViewRect.rect.height;
                }

                contentScaleX = scrollView.content.localScale.x;
                contentScaleY = scrollView.content.localScale.y;

                 contentTransform = scrollView.content;
            }

            if (itemTemplate != null)
            {
                var itemSize = itemTemplate.GetComponent<RectTransform>();
                if (itemSize != null)
                {
                    itemWidth = itemSize.rect.width;
                    itemHeight = itemSize.rect.height;
                }
            }
            itemPool.Put(itemTemplate);
            lastX = float.MinValue;
            lastY = float.MinValue;

            switch (tableType)
            {
                case eTableViewType.Vertical:
                    {
                        scrollView.vertical = true;
                        scrollView.horizontal = false;
                        contentTransform.anchorMin = new Vector2(0.5f, 1f);
                        contentTransform.anchorMax = new Vector2(0.5f, 1f);
                        contentTransform.pivot = new Vector2(0.5f, 1f);
                        spawnCount = Mathf.RoundToInt(visibleHeight / itemHeight / contentScaleY) + 2;
                    }
                    break;
                case eTableViewType.Horizontal:
                    {
                        scrollView.vertical = false;
                        scrollView.horizontal = true;
                        contentTransform.anchorMin = new Vector2(0f, 0.5f);
                        contentTransform.anchorMax = new Vector2(0f, 0.5f);
                        contentTransform.pivot = new Vector2(0f, 0.5f);
                        spawnCount = Mathf.RoundToInt(visibleWidth / itemWidth / contentScaleX) + 2;
                    }
                    break;
            }

            PoolSpawn(spawnCount);
        }

        protected virtual void LateUpdate()
        {
            if (contentTransform != null)
            {
                switch (tableType)
                {
                    case eTableViewType.Vertical:
                        {
                            var y = contentTransform.anchoredPosition.y / contentScaleY;
                            if (lastY != y)
                            {
                                lastY = y;
                                GetVisibleItemIndexVertical(y);

                                var keys = new List<int>(visibleNodes.Keys);
                                var count = keys.Count;
                                for (var i = 0; i < count; ++i)
                                {
                                    var idx = keys[i];
                                    if (!itemIndex.Contains(idx))
                                    {
                                        viewDelegate.Unuse?.Invoke(visibleNodes[idx]);
                                        itemPool.Put(visibleNodes[idx]);
                                        visibleNodes.Remove(idx);
                                    }
                                }

                                var it = itemIndex.GetEnumerator();
                                while (it.MoveNext())
                                {
                                    IndexChange(it.Current);
                                }
                            }
                        }
                        break;
                    case eTableViewType.Horizontal:
                        {
                            var x = contentTransform.anchoredPosition.x / contentScaleX;
                            if (lastX != x)
                            {
                                lastX = x;
                                GetVisibleItemIndexHorizental(x);

                                var keys = new List<int>(visibleNodes.Keys);
                                var count = keys.Count;
                                for (var i = 0; i < count; ++i)
                                {
                                    var idx = keys[i];
                                    if (!itemIndex.Contains(idx))
                                    {
                                        viewDelegate.Unuse?.Invoke(visibleNodes[idx]);
                                        itemPool.Put(visibleNodes[idx]);
                                        visibleNodes.Remove(idx);
                                    }
                                }

                                var it = itemIndex.GetEnumerator();
                                while (it.MoveNext())
                                {
                                    IndexChange(it.Current);
                                }
                            }
                        }
                        break;
                }
            }
        }

        public virtual void ReSetScrollRectAndRectTransform()
        {
            scrollView = GetComponent<ScrollRect>();
            if (scrollView != null)
            {
                var scrollViewRect = scrollView.viewport.GetComponent<RectTransform>();
                if (scrollViewRect != null)
                {
                    visibleWidth = scrollViewRect.rect.width;
                    visibleHeight = scrollViewRect.rect.height;
                }

                contentScaleX = scrollView.content.localScale.x;
                contentScaleY = scrollView.content.localScale.y;


                contentTransform = scrollView.content;
           
            }

            if (itemTemplate != null)
            {
                var itemSize = itemTemplate.GetComponent<RectTransform>();
                if (itemSize != null)
                {
                    itemWidth = itemSize.rect.width;
                    itemHeight = itemSize.rect.height;
                }
            }
            itemPool.Put(itemTemplate);
            lastX = float.MinValue;
            lastY = float.MinValue;

            switch (tableType)
            {
                case eTableViewType.Vertical:
                    {
                        scrollView.vertical = true;
                        scrollView.horizontal = false;
                        contentTransform.anchorMin = new Vector2(0.5f, 1f);
                        contentTransform.anchorMax = new Vector2(0.5f, 1f);
                        contentTransform.pivot = new Vector2(0.5f, 1f);
                        spawnCount = Mathf.RoundToInt(visibleHeight / itemHeight / contentScaleY) + 2;
                    }
                    break;
                case eTableViewType.Horizontal:
                    {
                        scrollView.vertical = false;
                        scrollView.horizontal = true;
                        contentTransform.anchorMin = new Vector2(0f, 0.5f);
                        contentTransform.anchorMax = new Vector2(0f, 0.5f);
                        contentTransform.pivot = new Vector2(0f, 0.5f);
                        spawnCount = Mathf.RoundToInt(visibleWidth / itemWidth / contentScaleX) + 2;
                    }
                    break;
            }
        }

        protected virtual void PoolSpawn(int count)
        {
            while (itemPool.Count < count)
            {
                itemPool.Put(Instantiate(itemTemplate));
            }
        }
        public virtual void SetDelegate(TableViewDelegate viewDelegate)
        {
            this.viewDelegate = viewDelegate;
        }
        public virtual void RemoveDelegate()
        {
            this.viewDelegate = null;
        }
        public virtual void ReLoad(bool initPos = true)
        {
            dataSource = viewDelegate.Items;
            var size = contentTransform.sizeDelta;
            size.x = GetTotalWidth();
            size.y = GetTotalHeight();
            contentTransform.sizeDelta = size;
            switch (tableType)
            {
                case eTableViewType.Vertical:
                    {
                        var parentTr = scrollView.content.transform;
                        var cCount = parentTr.childCount;
                        for (int i = 0; i < cCount; i++)
                        {
                            itemPool.Put(parentTr.GetChild(i).gameObject);
                        }

                        visibleNodes.Clear();

                        scrollView.StopMovement();

                        if (initPos)
                        {
                            scrollView.normalizedPosition = new Vector2(0.5f, 1f);
                        }

                        lastY = int.MinValue;
                    }
                    break;
                case eTableViewType.Horizontal:
                    {
                        var parentTr = scrollView.content.transform;
                        var cCount = parentTr.childCount;
                        for (int i = 0; i < cCount; i++)
                        {
                            itemPool.Put(parentTr.GetChild(i).gameObject);
                        }

                        visibleNodes.Clear();

                        scrollView.StopMovement();

                        if (initPos)
                        {
                            scrollView.normalizedPosition = new Vector2(0f, 0.5f);
                        }

                        lastX = int.MinValue;
                    }
                    break;
            }
        }

        [Button("Scroll to Item Test")]
        public void ScrollToItem(int index, float time, float padding = 0f)
        {
            Utilities.InternalDebug.Log("Scroll to Index : "+index.ToString());
            switch (tableType)
            {
                case eTableViewType.Vertical:
                    var height = paddingY * 2 + itemHeight * index + (spaceingY * Mathf.Max(0, index - 1)) + padding;
                    scrollView.content.DOLocalMoveY(height, time);
                    break;
                case eTableViewType.Horizontal:
                    var width = paddingX * 2 + itemWidth * index + (spaceingX * Mathf.Max(0, index-1)) + padding;
                    scrollView.content.DOLocalMoveX(-width, time);
                    break;
            }
        }

        protected virtual void GetVisibleItemIndexVertical(float y)
        {
            var itemSpancing = itemHeight + spaceingY;
            var minY = Mathf.Max(0, Mathf.FloorToInt(y / itemSpancing));
            var maxY = minY == 0 ? spawnCount : Mathf.CeilToInt((y + visibleHeight / contentScaleY) / itemSpancing);

            var totalCount = dataSource.Count;
            if ((minY + spawnCount) > totalCount)
            {
                minY = Mathf.Max(0, totalCount - spawnCount);
            }
            maxY = Mathf.Min(maxY, totalCount);

            itemIndex.Clear();
            for (var i = minY; i < maxY; ++i)
            {
                itemIndex.Add(i);
            }
        }

        protected virtual void GetVisibleItemIndexHorizental(float x)
        {
            var itemSpancing = itemWidth + spaceingX;
            var minX = Mathf.Max(0, Mathf.FloorToInt(-x / itemSpancing));
            var maxX = minX == 0 ? spawnCount : Mathf.CeilToInt((visibleWidth / contentScaleX - x) / itemSpancing);

            var totalCount = dataSource.Count;
            if ((minX + spawnCount) > totalCount)
            {
                minX = Mathf.Max(0, totalCount - spawnCount);
            }
            maxX = Mathf.Min(maxX, totalCount);

            itemIndex.Clear();
            for (var i = minX; i < maxX; i++)
            {
                itemIndex.Add(i);
            }
        }

        public virtual void IndexChange(int idx)
        {
            if (!visibleNodes.ContainsKey(idx))
            {
                vec3 = GetIndexPosition(idx);
                switch (tableType)
                {
                    case eTableViewType.Vertical:
                        {
                            PoolSpawn(1);

                            var node = itemPool.Get();
                            vec3.z = node.transform.localPosition.z;
                            node.transform.localPosition = vec3;

                            viewDelegate.Reuse?.Invoke(node, dataSource[idx]);
                            visibleNodes.Add(idx, node);
                            node.transform.SetAsLastSibling();
                        }
                        break;
                    case eTableViewType.Horizontal:
                        {
                            PoolSpawn(1);

                            var node = itemPool.Get();
                            vec3.z = node.transform.localPosition.z;
                            node.transform.localPosition = vec3;

                            viewDelegate.Reuse?.Invoke(node, dataSource[idx]);
                            visibleNodes.Add(idx, node);
                            node.transform.SetAsLastSibling();
                        }
                        break;
                }
            }
        }
        protected virtual float GetTotalWidth()
        {
            if (tableType != eTableViewType.Horizontal)
            {
                return 0f;
            }
            return paddingX * 2 + itemWidth * dataSource.Count + (spaceingX * Mathf.Max(0, dataSource.Count - 1));
        }
        protected virtual float GetTotalHeight()
        {
            if (tableType != eTableViewType.Vertical)
            {
                return 0f;
            }
            return paddingY * 2 + itemHeight * dataSource.Count + (spaceingY * Mathf.Max(0, dataSource.Count - 1));
        }

        public void SetPaddingMulti(float value)
        {
            paddingX *= value;
            paddingY *= value;
        }
        public void SetSpaceingMulti(float value)
        {
            spaceingX *= value;
            spaceingY *= value;
        }

        public int GetDataIndex(IDataKey<int> data)
        {
            if (dataSource == null)
                return -1;

            return dataSource.FindIndex((e) => { return e == data; });
        }
        protected virtual Vector3 GetIndexPosition(int idx)
        {
            return tableType switch
            {
                eTableViewType.Vertical => new Vector3(paddingX, -(paddingY + (idx + 0.5f) * itemHeight + spaceingY * idx)),
                eTableViewType.Horizontal => new Vector3(paddingX + (idx + 0.5f) * itemWidth + spaceingX * idx, paddingY),
                _ => Vector3.zero
            };
        }

        public bool IsVisibleNode(int _index)
        {
            return visibleNodes.ContainsKey(_index);
        }

        public Dictionary<int, GameObject> GetVisibleNodes()
        {
            return visibleNodes;
        }

        public void DeInitialize(DeInitDelegate deInitDelegate)
        {
            foreach(var node in visibleNodes)
            {
                deInitDelegate?.Invoke(node.Value);
            }
            viewDelegate = null;
        }
    }

}
