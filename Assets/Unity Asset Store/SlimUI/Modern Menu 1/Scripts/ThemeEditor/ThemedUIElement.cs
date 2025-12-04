using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlimUI.ModernMenu{
	[System.Serializable]
	public class ThemedUIElement : ThemedUI {
		[Header("Parameters")]
		Color outline;
		Image image;
		GameObject message;
		public enum OutlineStyle {solidThin, solidThick, dottedThin, dottedThick};
		public bool hasImage = false;
		public bool isText = false;

		protected override void OnSkinUI(){
			base.OnSkinUI();

			if(hasImage){
				image = GetComponent<Image>();
				if (image != null && themeController != null){
					image.color = themeController.currentColor;
				}
			}

			message = gameObject;

			if(isText && themeController != null){
				TMP_Text tmp = message.GetComponent<TMP_Text>();
				if (tmp != null){
					// 文字颜色
					tmp.color = themeController.textColor;

					// 阴影
					Material mat = tmp.fontMaterial;
					if (themeController.useShadow){
						mat.EnableKeyword("UNDERLAY_ON");
						mat.SetColor("_UnderlayColor", themeController.shadowColor);
						mat.SetFloat("_UnderlayOffsetX", themeController.shadowOffset.x);
						mat.SetFloat("_UnderlayOffsetY", themeController.shadowOffset.y);
					} else {
						mat.DisableKeyword("UNDERLAY_ON");
					}
				}
			}
		}
	}
}
