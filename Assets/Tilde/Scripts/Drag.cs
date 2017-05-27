using UnityEngine;
using UnityEngine.EventSystems;

namespace Tilde {
	public class Drag : MonoBehaviour, IPointerDownHandler, IDragHandler {
		[SerializeField] RectTransform panelRectTransform;

		//////////////////////////////////////////////////

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

		//////////////////////////////////////////////////

		Vector2 originalLocalPointerPosition;
		Vector3 originalPanelLocalPosition;
		RectTransform parentRectTransform;

		//////////////////////////////////////////////////

		#region MonoBehaviour
		void Awake() {
			parentRectTransform = panelRectTransform.parent as RectTransform;
		}
		#endregion
	}
}
