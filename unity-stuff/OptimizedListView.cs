using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(UIDraggablePanel))]
public class OptimizedListView : MonoBehaviour {
	public enum Arrangement
	{
		Horizontal,
		Vertical,
	}
	public Arrangement arrangement;
	public Transform itemPrefab;
	
	//x left, y right, z top, w bottom
	public Vector4 cullBox;
	
	public Vector2 size = new Vector2(150,190);
	public int maxPerLine = 3;
	
	public int rowCount = 2;
	
	public int count = 0;
	public int Count
	{
		set {
			count = value;
		}
		get {
			return count;
		}
	}
	
	int index = 0;
	
	public System.Action<int,GameObject> onInitItemView;
	
	public UIDraggablePanel draggablePanel;
	
	void Awake()
	{
		if(!draggablePanel) draggablePanel = GetComponent<UIDraggablePanel>();
		var dragScale = draggablePanel.scale;
		if(arrangement == Arrangement.Horizontal) {
			dragScale.y = 0;
			dragScale.z = 0;
		}
		else {
			dragScale.x = 0;
			dragScale.z = 0;
		}
		draggablePanel.scale = dragScale;
		
		var panel = draggablePanel.panel;
	}
	
	void Start()
	{
		Reposition();
	}
	
	void FixedUpdate () {
		for (int i = 0; i < transform.childCount; i++) {
			var child = transform.GetChild(i);
			if(!child.gameObject.activeSelf) continue;//skip inactive item
			Vector3 pos = child.localPosition + transform.localPosition;
			
			int repositionIdx = -1;
			float curr = arrangement == Arrangement.Horizontal ? pos.x : pos.y;
			
			bool cull0 = arrangement == Arrangement.Horizontal ? curr < cullBox.x : curr > cullBox.z;
			bool cull1 = arrangement == Arrangement.Horizontal ? curr > cullBox.y : curr < cullBox.w;
			
			if(cull0 && index + transform.childCount < Count)
			{
				index++;
				repositionIdx = index + transform.childCount-1;
			}
			else if(cull1 && index > 0)
			{
				index--;
				repositionIdx = index;
			}
			
			if(repositionIdx != -1) Reposition(repositionIdx, child);
		}
	}
	
	void Reposition()
	{
		index = 0;
		int totalCount = (maxPerLine+2) * rowCount;
		
		for (int i = 0; i < transform.childCount; i++) {
			transform.GetChild(i).gameObject.SetActive(false);
		}
		
		for (int i = 0; i < totalCount && i < Count; i++) {
			Transform child = null;
			if(i < transform.childCount) {
				child = transform.GetChild(i);
			}
			else {
				child = Instantiate(itemPrefab) as Transform;
			}
			child.gameObject.SetActive(true);
			child.parent = transform;
			Reposition(i, child);
		}
		
		draggablePanel.ResetPosition();
	}
	
	void Reposition(int idx, Transform view)
	{
		view.localPosition = (arrangement == Arrangement.Horizontal) ? 
			new Vector3(size.x * (idx/rowCount), -size.y * (idx%rowCount), 0):
			new Vector3(size.x * (idx%rowCount), -size.y * (idx/rowCount), 0);
		view.localScale = Vector3.one;
		
		draggablePanel.restrictWithinPanel = index+transform.childCount == Count || index == 0;
		
		if(onInitItemView != null) onInitItemView(idx, view.gameObject);
	}

#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		var relPos = transform.localPosition;
		var topLeft =  transform.TransformPoint(new Vector3(cullBox.x - relPos.x, cullBox.z - relPos.y, 0));
		var topRight = transform.TransformPoint(new Vector3(cullBox.y - relPos.x, cullBox.z - relPos.y, 0));
		var bottomLeft = transform.TransformPoint(new Vector3(cullBox.x - relPos.x, cullBox.w - relPos.y, 0));
		var bottomRight = transform.TransformPoint(new Vector3(cullBox.y - relPos.x, cullBox.w - relPos.y, 0));
		Gizmos.DrawLine(topLeft, topRight);
		Gizmos.DrawLine(topRight, bottomRight);
		Gizmos.DrawLine(bottomRight, bottomLeft);
		Gizmos.DrawLine(topLeft, bottomLeft);
	}
#endif
	
	public void ClearView()
	{
		for (int i = 0; i < transform.childCount; i++) {
			Destroy(transform.GetChild(i));
		}
		transform.DetachChildren();
	}
	
	public void ResetView(int count, System.Action<int,GameObject> itemInitCallback)
	{
		Count = count;
		onInitItemView = itemInitCallback;
		Reposition();
	}
}
