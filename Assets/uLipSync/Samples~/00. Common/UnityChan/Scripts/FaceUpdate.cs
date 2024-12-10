﻿using UnityEngine;
using System.Collections;

namespace UnityChan
{
	public class FaceUpdate : MonoBehaviour
	{
		public AnimationClip[] animations;
		Animator anim;
		public float delayWeight;
		public bool isKeepFace = false;

		void Start ()
		{
			anim = GetComponent<Animator> ();
		}

		void OnGUI ()
		{
			GUILayout.Box ("Face Update", GUILayout.Width (170), GUILayout.Height (25 * (animations.Length + 2)));
			Rect screenRect = new Rect (10, 25, 150, 25 * (animations.Length + 1));
			GUILayout.BeginArea (screenRect);
			foreach (var animation in animations) {
				if (GUILayout.RepeatButton (animation.name)) {
					anim.CrossFade (animation.name, 0);
				}
			}
			isKeepFace = GUILayout.Toggle (isKeepFace, " Keep Face");
			GUILayout.EndArea ();
		}

		float current = 0;

		void Update ()
		{

			if (Input.GetMouseButton (0)) {
				current = 1;
			} else if (!isKeepFace) {
				current = Mathf.Lerp (current, 0, delayWeight);
			}
			anim.SetLayerWeight (1, current);
		}
	 

		//アニメーションEvents側につける表情切り替え用イベントコール
		public void OnCallChangeFace (string str)
		{   
			int ichecked = 0;
			foreach (var animation in animations) {
				if (str == animation.name) {
					ChangeFace (str);
					break;
				} else if (ichecked <= animations.Length) {
					ichecked++;
				} else {
					//str指定が間違っている時にはデフォルトで
					str = "default@unitychan";
					ChangeFace (str);
				}
			} 
		}

		void ChangeFace (string str)
		{
			isKeepFace = true;
			current = 1;
			anim.CrossFade (str, 0);
		}
	}
}
