using UnityEngine;

public class SUIWidget : SUIProcess, IHasVisualAttributes
{
	protected GameObject mGameObject;

	protected Transform mGOTransformRef;

	private Vector2 mPosition;

	private float mPriority;

	private Vector2 mScale;

	public Vector2 position
	{
		get
		{
			return mPosition;
		}
		set
		{
			if (mPosition != value)
			{
				mPosition = value;
				updatePosition();
			}
		}
	}

	public float priority
	{
		get
		{
			return mPriority;
		}
		set
		{
			mPriority = value;
			updatePosition();
		}
	}

	public virtual float alpha
	{
		get
		{
			return 1f;
		}
		set
		{
		}
	}

	public virtual Rect area
	{
		get
		{
			return new Rect(position.x, position.y, 0f, 0f);
		}
		set
		{
			position = new Vector2(value.xMin + value.width / 2f, value.yMin + value.height / 2f);
		}
	}

	public virtual bool visible
	{
		get
		{
			return mGameObject.activeSelf;
		}
		set
		{
			if (mGameObject.activeSelf != value)
			{
				mGameObject.SetActive(value);
			}
		}
	}

	public virtual Vector2 scale
	{
		get
		{
			return new Vector2(1f, 1f);
		}
		set
		{
		}
	}

	public GameObject gameObject
	{
		get
		{
			return mGameObject;
		}
	}

	public SUIWidget()
	{
		mGameObject = new GameObject();
		mGameObject.layer = LayerMask.NameToLayer("SamuraiUI");
		mGOTransformRef = mGameObject.transform;
		mPosition = new Vector2(0.5f, 0.5f);
		updatePosition();
	}

	public virtual void Destroy()
	{
		mGOTransformRef = null;
		Object.Destroy(mGameObject);
		mGameObject = null;
	}

	public virtual void Update()
	{
	}

	public virtual void EditorRenderOnGUI()
	{
	}

	protected virtual void updatePosition()
	{
		Vector2 vector = SUIUtils.userToUnity(mPosition);
		mGOTransformRef.position = new Vector3(vector.x, vector.y, mPriority);
	}
}
