using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Tilde { 
public class Resize : MonoBehaviour, IPointerDownHandler, IDragHandler {
	public Vector2 minSize = new Vector2(100, 100);
	public RectTransform panelRectTransform;
	private Vector2 originalLocalPointerPosition;
	private Vector2 originalSizeDelta;
	
	public void OnPointerDown(PointerEventData data) {
		originalSizeDelta = panelRectTransform.sizeDelta;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out originalLocalPointerPosition);
	}
	
	public void OnDrag(PointerEventData data) {
		if (panelRectTransform == null) {
			return;
		}
		
		Vector2 localPointerPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out localPointerPosition);
		
		Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
		
		Vector2 sizeDelta = originalSizeDelta + new Vector2(offsetToOriginal.x, -offsetToOriginal.y);
		sizeDelta = new Vector2(Mathf.Clamp(sizeDelta.x, minSize.x, sizeDelta.x), Mathf.Clamp(sizeDelta.y, minSize.y, sizeDelta.y));
		
		panelRectTransform.sizeDelta = sizeDelta;
	}
}
}
