using UnityEngine;

namespace UITool
{
    public class TableViewGrid : TableView
    {
        [Space(10f)]
        [Header("Grid")]
        [SerializeField]
        protected int grid = 1;

        /// <summary>
        /// 최초 1회 무조건 호출할 것
        /// At first, must call this 
        /// </summary>
        public override void OnStart()
        {
            if (isInitialized)
                return;

            base.OnStart();

            if (grid < 1)
                grid = 1;

            switch (tableType)
            {
                case eTableViewType.Vertical:
                    {
                        spawnCount = (Mathf.RoundToInt(visibleHeight / contentScaleY / itemHeight) + 2) * grid;
                    }
                    break;
                case eTableViewType.Horizontal:
                    {
                        spawnCount = (Mathf.RoundToInt(visibleWidth / contentScaleX / itemWidth) + 2) * grid;
                    }
                    break;
            }

            PoolSpawn(spawnCount);
        }
        public override void IndexChange(int idx)
        {
            if (!visibleNodes.ContainsKey(idx))
            {
                var gridRow = Mathf.FloorToInt(idx / grid);
                var gridCol = idx % grid;
                switch (tableType)
                {
                    case eTableViewType.Vertical:
                        {
                            PoolSpawn(1);

                            var node = itemPool.Get();
                            vec3 = node.transform.localPosition;
                            vec3.x = -contentTransform.sizeDelta.x * 0.5f + paddingX + (gridCol + 0.5f) * itemWidth + spaceingX * gridCol;
                            vec3.y = -(paddingY + (gridRow + 0.5f) * itemHeight + spaceingY * gridRow);
                            node.transform.localPosition = vec3;

                            viewDelegate.Reuse?.Invoke(node, dataSource[idx]);
                            visibleNodes.Add(idx, node);
                        }
                        break;
                    case eTableViewType.Horizontal:
                        {
                            PoolSpawn(1);

                            var node = itemPool.Get();
                            vec3 = node.transform.localPosition;
                            vec3.x = paddingX + (gridRow + 0.5f) * itemWidth + spaceingX * gridRow;
                            vec3.y = contentTransform.sizeDelta.y * 0.5f - (paddingY + (gridCol + 0.5f) * itemHeight + spaceingY * gridCol);
                            node.transform.localPosition = vec3;

                            viewDelegate.Reuse?.Invoke(node, dataSource[idx]);
                            visibleNodes.Add(idx, node);
                        }
                        break;
                }
            }
        }
        protected override void GetVisibleItemIndexVertical(float y)
        {
            var itemSpancing = itemHeight + spaceingY;
            var minY = Mathf.Max(0, Mathf.FloorToInt(y / itemSpancing) * grid);
            var maxY = minY == 0 ? spawnCount : Mathf.CeilToInt((y + visibleHeight / contentScaleY + itemSpancing * 0.5f) / itemSpancing) * grid;

            var totalCount = dataSource.Count;
            if ((minY + spawnCount) > totalCount)
            {
                minY = Mathf.Max(0, totalCount - spawnCount);
            }
            maxY = Mathf.Min(maxY, totalCount);

            itemIndex.Clear();
            for (var i = minY; i < maxY; i++)
            {
                itemIndex.Add(i);
            }
        }
        protected override void GetVisibleItemIndexHorizental(float x)
        {
            var itemSpancing = itemWidth + spaceingX;
            var minX = Mathf.Max(0, Mathf.FloorToInt(-x / itemSpancing) * grid);
            var maxX = minX == 0 ? spawnCount : Mathf.CeilToInt((visibleWidth / contentScaleX - x + itemSpancing * 0.5f) / itemSpancing) * grid;

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
        protected override float GetTotalWidth()
        {
            if (tableType != eTableViewType.Horizontal)
                return paddingX * 2 + itemWidth * grid + (spaceingX * Mathf.Max(0, grid - 1));

            var gridLength = dataSource.Count / grid + ((dataSource.Count % grid) != 0 ? 1 : 0);
            return paddingX * 2 + itemWidth * gridLength + (spaceingX * Mathf.Max(0, gridLength - 1));
        }
        protected override float GetTotalHeight()
        {
            if (tableType != eTableViewType.Vertical)
            {
                return paddingY * 2 + itemHeight * grid + (spaceingY * Mathf.Max(0, grid - 1));
            }
            var gridLength = dataSource.Count / grid + ((dataSource.Count % grid) != 0 ? 1 : 0);
            return paddingY * 2 + itemHeight * gridLength + (spaceingY * Mathf.Max(0, gridLength - 1));
        }
        public void SetScrollViewToBottom()
        {
            this.scrollView.verticalNormalizedPosition = 0;
        }
        protected override Vector3 GetIndexPosition(int idx)
        {
            var gridRow = Mathf.FloorToInt(idx / grid);
            var gridCol = idx % grid;
            return tableType switch
            {
                eTableViewType.Vertical => new Vector3(-contentTransform.sizeDelta.x * 0.5f + paddingX + (gridCol + 0.5f) * itemWidth + spaceingX * gridCol, -(paddingY + (gridRow + 0.5f) * itemHeight + spaceingY * gridRow)),
                eTableViewType.Horizontal => new Vector3(paddingX + (gridRow + 0.5f) * itemWidth + spaceingX * gridRow, contentTransform.sizeDelta.y * 0.5f - (paddingY + (gridCol + 0.5f) * itemHeight + spaceingY * gridCol)),
                _ => Vector3.zero
            };
        }
    }
}