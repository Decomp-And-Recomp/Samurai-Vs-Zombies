using System.Collections.Generic;
using UnityEngine;

public class SUILabel : SUIWidget
{
	private const float kShadowPriorityOffset = -0.001f;

	private GUIText mTextSprite;

	private string mFontFile;

	private string mOriginalText;

	private int mMaxWidth = -1;

	private int mMaxLines = -1;

	private int mShownLines;

	private SUILabel mShadow;

	private Vector2 mShadowOffset = Vector2.zero;

	private float mFontScale = 1f;

	public string font
	{
		get
		{
			return mFontFile;
		}
	}

	public string text
	{
		get
		{
			return mOriginalText;
		}
		set
		{
			if (mOriginalText != value)
			{
				mOriginalText = value;
				refreshDisplay(true);
				if (mShadow != null)
				{
					mShadow.text = value;
				}
			}
		}
	}

	public override bool visible
	{
		get
		{
			return base.visible;
		}
		set
		{
			base.visible = value;
			if (mShadow != null)
			{
				mShadow.visible = value;
			}
		}
	}

	public Color fontColor
	{
		get
		{
			return mTextSprite.material.color;
		}
		set
		{
			mTextSprite.material.color = new Color(value.r, value.g, value.b, mTextSprite.material.color.a);
		}
	}

	public TextAnchor anchor
	{
		get
		{
			return mTextSprite.anchor;
		}
		set
		{
			mTextSprite.anchor = value;
			if (mShadow != null)
			{
				mShadow.anchor = value;
			}
		}
	}

	public TextAlignment alignment
	{
		get
		{
			return mTextSprite.alignment;
		}
		set
		{
			mTextSprite.alignment = value;
			if (mShadow != null)
			{
				mShadow.alignment = value;
			}
		}
	}

	public int maxWidth
	{
		get
		{
			return mMaxWidth;
		}
		set
		{
			if (mMaxWidth != value)
			{
				mMaxWidth = value;
				refreshDisplay(true);
				if (mShadow != null)
				{
					mShadow.maxWidth = value;
				}
			}
		}
	}

	public int maxLines
	{
		get
		{
			return mMaxLines;
		}
		set
		{
			if (mMaxLines != value)
			{
				mMaxLines = value;
				refreshDisplay(true);
				if (mShadow != null)
				{
					mShadow.maxLines = value;
				}
			}
		}
	}

	public int shownLines
	{
		get
		{
			return mShownLines;
		}
	}

	public string shownText
	{
		get
		{
			return mTextSprite.text;
		}
	}

	public override float alpha
	{
		get
		{
			return mTextSprite.material.color.a;
		}
		set
		{
			if (value != mTextSprite.material.color.a)
			{
				mTextSprite.material.color = new Color(mTextSprite.material.color.r, mTextSprite.material.color.g, mTextSprite.material.color.b, value);
				if (mShadow != null)
				{
					mShadow.alpha = value;
				}
			}
		}
	}

	public override Rect area
	{
		get
		{
			return SUIUtils.unityToUser(mTextSprite.GetScreenRect());
		}
	}

	public Color shadowColor
	{
		get
		{
			if (mShadow != null)
			{
				return mShadow.fontColor;
			}
			return Color.clear;
		}
		set
		{
			if (value == Color.clear)
			{
				if (mShadow != null)
				{
					mShadow.Destroy();
					mShadow = null;
				}
				return;
			}
			if (mShadow != null)
			{
				mShadow.fontColor = value;
				return;
			}
			mShadow = new SUILabel(mFontFile);
			mShadow.fontColor = value;
			mShadow.position = position + mShadowOffset;
			mShadow.priority = priority + -0.001f;
			mShadow.alpha = alpha;
			mShadow.maxWidth = maxWidth;
			mShadow.maxLines = maxLines;
			mShadow.anchor = anchor;
			mShadow.text = text;
		}
	}

	public Vector2 shadowOffset
	{
		get
		{
			return mShadowOffset;
		}
		set
		{
			mShadowOffset = value;
			if (mShadow != null)
			{
				mShadow.position = position + mShadowOffset;
			}
		}
	}

	private int adjustedMaxWidth
	{
		get
		{
			if (MultiLanguages.isAsian)
			{
				return Mathf.FloorToInt(WeakGlobalInstance<SUIScreen>.instance.autoScaler.toDeviceX(mMaxWidth));
			}
			return Mathf.FloorToInt(WeakGlobalInstance<SUIScreen>.instance.autoScaler.toDeviceX(mMaxWidth) / mFontScale);
		}
	}

	public SUILabel(string fontFile)
	{
		initSprite(fontFile);
	}

	public SUILabel(string fontFile, string txt)
	{
		initSprite(fontFile);
		text = txt;
	}

	public override void Destroy()
	{
		Object.Destroy(mTextSprite);
		if (mShadow != null)
		{
			mShadow.Destroy();
			mShadow = null;
		}
		base.Destroy();
	}

	public override void EditorRenderOnGUI()
	{
		GUIStyle gUIStyle = new GUIStyle();
		gUIStyle.font = mTextSprite.font;
		gUIStyle.normal.textColor = mTextSprite.material.color;
		Vector2 vector = gUIStyle.CalcSize(new GUIContent(mOriginalText));
		Rect rect = new Rect(position.x, position.y, vector.x, vector.y);
		switch (mTextSprite.anchor)
		{
		case TextAnchor.UpperCenter:
		case TextAnchor.MiddleCenter:
		case TextAnchor.LowerCenter:
			rect.x -= vector.x / 2f;
			break;
		case TextAnchor.UpperRight:
		case TextAnchor.MiddleRight:
		case TextAnchor.LowerRight:
			rect.x -= vector.x;
			break;
		}
		switch (mTextSprite.anchor)
		{
		case TextAnchor.MiddleLeft:
		case TextAnchor.MiddleCenter:
		case TextAnchor.MiddleRight:
			rect.y -= vector.y / 2f;
			break;
		case TextAnchor.LowerLeft:
		case TextAnchor.LowerCenter:
		case TextAnchor.LowerRight:
			rect.y -= vector.y;
			break;
		}
		Color color = GUI.color;
		GUI.color = new Color(1f, 1f, 1f, alpha);
		GUI.Label(rect, mOriginalText, gUIStyle);
		GUI.color = color;
	}

	private void initSprite(string fontFile)
	{
		mFontFile = fontFile;
		mFontScale = Singleton<FontSetFinder>.instance.GetScaleFactor(fontFile);
		string prefab = Singleton<FontSetFinder>.instance.GetPrefab(fontFile);
		if (prefab == string.Empty)
		{
			mTextSprite = (GUIText)mGameObject.AddComponent<GUIText>();
		}
		else
		{
			Object.Destroy(mGameObject);
			mGameObject = Object.Instantiate(Resources.Load(prefab)) as GameObject;
			mGOTransformRef = mGameObject.transform;
			mTextSprite = mGameObject.GetComponent<GUIText>();
		}
		string path = Singleton<FontSetFinder>.instance.basePath + fontFile;
		Font font = null;
		font = (Font)Resources.Load(path, typeof(Font));
		if (font == null)
		{
			font = (Font)Resources.Load("Fonts/default/" + fontFile, typeof(Font));
		}
		if (font == null)
		{
			Debug.Log("ERROR: Could not load font: " + fontFile);
		}
		mTextSprite.font = font;

		if (font != null)
		{
			Vector2 pixelOffset = mTextSprite.pixelOffset;
			pixelOffset.y += font.ascent;
			mTextSprite.pixelOffset = pixelOffset;
		}
	}

	private void refreshDisplay(bool forceRefresh)
	{
		if (mMaxWidth == -1 && !forceRefresh)
		{
			return;
		}
		mTextSprite.text = mOriginalText;
		mShownLines = 1;
		if (mMaxWidth > 0 && mTextSprite.GetScreenRect().width > (float)adjustedMaxWidth)
		{
			string text = mOriginalText;
			List<string> list = new List<string>();
			while (text.Length > 0)
			{
				string nextString = getNextString(text, adjustedMaxWidth);
				list.Add(nextString);
				text = ((text.Length > nextString.Length) ? text.Substring(nextString.Length).Trim() : string.Empty);
			}
			mShownLines = mMaxLines;
			if (mShownLines <= 0 || mShownLines > list.Count)
			{
				mShownLines = list.Count;
			}
			for (int i = 0; i < mShownLines; i++)
			{
				text = text + list[i] + "\n";
			}
			mTextSprite.text = text;
		}
		updateScale();
	}

	private string getNextString(string source, int maxWidth)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		if (MultiLanguages.isAsian)
		{
			mTextSprite.text = source;
			if (mTextSprite.GetScreenRect().width <= (float)adjustedMaxWidth)
			{
				return source;
			}
			for (int i = 1; i < source.Length; i++)
			{
				text2 = source.Substring(0, i);
				mTextSprite.text = text2;
				if (mTextSprite.GetScreenRect().width > (float)adjustedMaxWidth)
				{
					return source.Substring(0, i - 1);
				}
			}
		}
		else
		{
			List<string> list = new List<string>(source.Split(' '));
			for (int j = 0; j < list.Count; j++)
			{
				text2 = text2 + list[j] + " ";
				mTextSprite.text = text2;
				if (mTextSprite.GetScreenRect().width > (float)adjustedMaxWidth)
				{
					if (text.Length > 0)
					{
						text2 = text;
					}
					break;
				}
				text = text2;
			}
		}
		return text2.Trim();
	}

	protected override void updatePosition()
	{
		base.updatePosition();
		if (mShadow != null)
		{
			mShadow.position = position + mShadowOffset;
			mShadow.priority = priority + -0.001f;
		}
	}

	private void updateScale()
	{
		if (mFontScale != 1f)
		{
			mGameObject.transform.localScale = new Vector3(mFontScale * ((float)Screen.height / (float)Screen.width), mFontScale, 0f);
		}
	}
}
