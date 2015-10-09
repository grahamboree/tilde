using UnityEngine;
using UnityEngine.EventSystems;

namespace Tilde {
public class Drag : MonoBehaviour, IPointerDownHandler, IDragHandler {
	public RectTransform panelRectTransform;
	private Vector2 originalLocalPointerPosition;
	private Vector3 originalPanelLocalPosition;
	private RectTransform parentRectTransform;
	
	void Awake() {
		parentRectTransform = panelRectTransform.parent as RectTransform;
	}
	
	public void OnPointerDown(PointerEventData data) {
		originalPanelLocalPosition = panelRectTransform.localPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, data.position, data.pressEventCamera, out originalLocalPointerPosition);
	}
	
	public void OnDrag(PointerEventData data) {
		if (panelRectTransform == null || parentRectTransform == null) {
			return;
		}
		
		Vector2 localPointerPosition;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, data.position, data.pressEventCamera, out localPointerPosition)) {
			Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
			panelRectTransform.localPosition = originalPanelLocalPosition + offsetToOriginal;
		}
	}
}
}