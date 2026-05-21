using Godot;

/// <summary>
/// A draggable crew member node. Can only be slotted into a CrewSlot.
/// </summary>
public partial class Crew : Node2D, IDraggable, IOrientation, IHealth
{
	public Vector2 OriginalPosition { get; set; }
	public bool IsDragging { get; set; }
	public bool IsHovered { get; private set; }
	public ISlottable CurrentSlot { get; set; }

	// IHealth ------------------------------------------------------------------
	public int MaxHealth { get; private set; } = 10;
	public int CurrentHealth { get; set; } = 10;

	private Node _originalParent;
	private Vector2 _originalLocalPosition;
	private bool _wasInCrewSlot;

	/// <summary>Local position offset applied whenever this crew is slotted into a casket.</summary>
	public Vector2 SlotOffset { get; set; } = Vector2.Zero;

	/// <summary>How many turns remain before this crew member is ready to act.</summary>
	public int TurnsUntilReady { get; set; } = 0;

	/// <summary>Whether this crew member is ready to act.</summary>
	public bool IsReady { get; set; } = false;

	/// <summary>True once this crew has been deployed via a deploy charge. Further moves are free.</summary>
	public bool IsDeployed { get; private set; } = false;

	// IOrientation --------------------------------------------------------------

	/// <summary>The direction this crew member is facing.</summary>
	[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Right;

	private CrewState _state = CrewState.HyperSleep;
	/// <summary>Current activity state of this crew member.</summary>
	public CrewState State
	{
		get => _state;
		set
		{
			if (_state == value) return;
			_state = value;
			EmitSignal(SignalName.StateChanged, (int)value);
		}
	}

	[Signal] public delegate void StateChangedEventHandler(int newState);

	/// <summary>The data record assigned to this crew member on spawn.</summary>
	private CrewData _data;
	public CrewData Data
	{
		get => _data;
		set
		{
			_data = value;
			if (_data != null)
			{
				MaxHealth = _data.Health;
				CurrentHealth = _data.Health;
			}
		}
	}

	private Node _tooltip;

	public override void _Ready()
	{
		OriginalPosition = GlobalPosition;
		_tooltip = GetNodeOrNull("Tooltip");
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;
	}

	// ---------------------------------------------------------------------------
	// Focus / Hover
	// ---------------------------------------------------------------------------

	public void _on_focus_pressed()
	{
		if (!IsReady)
		{
			PlayWiggle();
			return;
		}
		if (!IsDeployed && !HasCrewDeploysRemaining())
		{
			PlayWiggle();
			return;
		}
		OnDragStart();
	}

	public void _on_focus_mouse_entered()
	{
		IsHovered = true;
		if (_tooltip is CanvasItem tooltipItem)
		{
			tooltipItem.Visible = true;
			if (Data != null && _tooltip.GetNodeOrNull("PanelContainer/Label") is Label label)
				label.Text = $"{Data.Name}\n{Data.Role.ToDisplayString()}";
		}

		if (Data != null)
			ShowFocusStats(Data);
	}

	public void _on_focus_mouse_exited()
	{
		IsHovered = false;
		if (_tooltip is CanvasItem tooltipItem)
			tooltipItem.Visible = false;

		HideFocusStats();
	}

	private bool HasCrewDeploysRemaining()
	{
		PlaySpace ps = GetTree().Root.GetNodeOrNull("PlaySpace") as PlaySpace;
		return ps == null || ps.CrewDeploysRemaining > 0;
	}

	private Tween _wiggleTween;

	/// <summary>The local position the crew should rest at inside its current parent.</summary>
	private Vector2 RestingLocalPosition =>
		CurrentSlot is CrewSlot ? new Vector2(8, 8) :
		CurrentSlot != null     ? SlotOffset :
								  Position;

	private void PlayWiggle()
	{
		_wiggleTween?.Kill();
		Position = RestingLocalPosition;
		Vector2 origin = Position;
		const float dist = 4f;
		const float step = 0.05f;
		_wiggleTween = CreateTween();
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(0, -dist), step);
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(0, dist), step);
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(0, -dist), step);
		_wiggleTween.TweenProperty(this, "position", origin + new Vector2(0, dist), step);
		_wiggleTween.TweenProperty(this, "position", origin, step);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (mouseButton.Pressed && IsHovered)
				{
					if (!IsReady)
					{
						PlayWiggle();
						return;
					}
					if (!IsDeployed && !HasCrewDeploysRemaining())
					{
						PlayWiggle();
						return;
					}
					OnDragStart();
				}
				else if (!mouseButton.Pressed && IsDragging)
				{
					ISlottable targetSlot = FindSlotUnderMouse();
					OnDragEnd(targetSlot);
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		if (IsDragging)
		{
			OnDragUpdate(GetGlobalMousePosition());
		}
	}

	private static CompressedTexture2D _frontViewTexture;
	private static CompressedTexture2D _topDownTexture;

	public void OnDragStart()
	{
		_wasInCrewSlot = CurrentSlot is CrewSlot;
		// Vacate the current slot before lifting the piece
		if (CurrentSlot != null)
		{
			CurrentSlot.Vacate();
			CurrentSlot = null;
		}

		// Reparent to CanvasLayer so position is not relative to the casket
		Node root = GetTree().Root;
		Node dragParent = root.GetNodeOrNull("PlaySpace/CanvasLayer") ?? root;
		if (GetParent() != dragParent)
		{
			_originalParent = GetParent();
			_originalLocalPosition = Position;
			Vector2 globalPos = GlobalPosition;
			GetParent().RemoveChild(this);
			dragParent.AddChild(this);
			GlobalPosition = globalPos;
		}

		// Switch to front-view texture and reset rotation while dragging
		if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
		{
			_frontViewTexture ??= GD.Load<CompressedTexture2D>("res://Assets/Characters/CrewFrontView.png");
			sprite.Texture = _frontViewTexture;
			sprite.RotationDegrees = 0f;
		}

		IsDragging = true;
		CrewSlot.HighlightAll();
	}

	public void OnDragEnd(ISlottable targetSlot)
	{
		IsDragging = false;
		CrewSlot.ResetAll();

		// Deploying into a CrewSlot costs a crew deploy, unless already deployed this turn.
		bool isCrewSlotDeploy = targetSlot is CrewSlot && !IsDeployed;
		if (isCrewSlotDeploy)
		{
			PlaySpace ps = GetTree().Root.GetNodeOrNull("PlaySpace") as PlaySpace;
			if (ps == null || !ps.TryUseCrewDeploy())
				targetSlot = null; // treat as a failed drop
			else
				IsDeployed = true;
		}

		if (targetSlot != null && targetSlot.CanAccept(this))
		{
			// If moving out of a casket into a CrewSlot, mark casket label as empty
			if (targetSlot is CrewSlot && _originalParent is Casket sourceCasket)
				sourceCasket.SetReadyLabel("--");

			// Crew always lives under CanvasLayer/PlayerShip/Portrait/Pivot//Crew; other draggables are parented to their slot
			if (targetSlot is CrewSlot)
			{
				Node crewContainer = GetTree().Root.GetNodeOrNull("PlaySpace/CanvasLayer/PlayerShip/Portrait/Pivot/Crew");
				if (crewContainer != null && GetParent() != crewContainer)
				{
					GetParent().RemoveChild(this);
					crewContainer.AddChild(this);
				}
			}
			else if (targetSlot is Node slotNode && GetParent() != slotNode)
			{
				GetParent().RemoveChild(this);
				slotNode.AddChild(this);
			}
			targetSlot.Accept(this);
			if (targetSlot is CrewSlot)
				Position += new Vector2(8, 8);
			else
				Position = SlotOffset;

			// CrewSlot.Accept already sets the correct rotation; restore top-down texture too
			if (targetSlot is CrewSlot)
			{
				if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D crewSlotSprite)
				{
					_topDownTexture ??= GD.Load<CompressedTexture2D>("res://Assets/Characters/CrewTopDown.png");
					crewSlotSprite.Texture = _topDownTexture;
				}
			}
			if (targetSlot is not CrewSlot)
			{
				if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
					sprite.RotationDegrees = Facing == CardinalDirection.Left ? 90f : -90f;
			}
		}
		else
		{
			// If returning to a CrewSlot, restore top-down texture and correct rotation
			if (_wasInCrewSlot)
			{
				if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
				{
					_topDownTexture ??= GD.Load<CompressedTexture2D>("res://Assets/Characters/CrewTopDown.png");
					sprite.Texture = _topDownTexture;
					sprite.RotationDegrees = Facing switch
					{
						CardinalDirection.Left  => -90f,
						CardinalDirection.Right => 90f,
						CardinalDirection.Up    => 0f,
						_                       => 180f,
					};
				}
			}
			else
			{
				// Restore orientation rotation for casket return
				if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
					sprite.RotationDegrees = Facing == CardinalDirection.Left ? 90f : -90f;
			}

			// If returning to the original casket, restore its label (resume flicker if crew was ready)
			if (_originalParent is Casket originalCasket)
			{
				if (IsReady)
					originalCasket.StartReadyFlicker();
				else
					originalCasket.SetReadyLabel("00");
			}

			// Tween back to the original parent and position over 0.25 seconds
			if (_originalParent != null)
			{
				Node originalParent = _originalParent;
				Vector2 originalLocalPos = _originalLocalPosition;
				Vector2 currentGlobal = GlobalPosition;

				GetParent().RemoveChild(this);
				originalParent.AddChild(this);
				// Start at the global position we were at before reparenting
				GlobalPosition = currentGlobal;

				Tween tween = CreateTween();
				tween.TweenProperty(this, "position", originalLocalPos, 0.125)
					 .SetTrans(Tween.TransitionType.Sine)
					 .SetEase(Tween.EaseType.Out);
			}
			else
			{
				Tween tween = CreateTween();
				tween.TweenProperty(this, "global_position", OriginalPosition, 0.125)
					 .SetTrans(Tween.TransitionType.Sine)
					 .SetEase(Tween.EaseType.Out);
			}
		}
	}

	public void OnDragUpdate(Vector2 mousePosition)
	{
		GlobalPosition = mousePosition;
	}

	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private static string Stars(int value) => new string('*', Mathf.Max(value, 0));

	private void ShowFocusStats(CrewData data)
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is not CanvasItem panel)
			return;

		panel.Visible = true;

		if (panel.GetNodeOrNull("Tooltip/Label") is Label label)
		{
			label.Text =
				$"{data.Name}\n" +
				$"{data.Role.ToDisplayString()}\n" +
				$"HP:       {CurrentHealth}/{MaxHealth}\n" +
				$"Melee:    {Stars(data.Melee)}\n" +
				$"Repair:   {Stars(data.Repair)}\n" +
				$"Medical:  {Stars(data.Medical)}\n" +
				$"Weapons:  {Stars(data.Weapons)}\n" +
				$"Piloting: {Stars(data.Piloting)}\n" +
				$"Science:  {Stars(data.Science)}\n" +
				$"Arcane:   {Stars(data.Arcane)}";
		}
	}

	private void HideFocusStats()
	{
		Node root = GetTree().Root;
		if (root.GetNodeOrNull("PlaySpace/CanvasLayer/GUI/Gameplay/Stats/CurrentFocusStats") is CanvasItem panel)
			panel.Visible = false;
	}

	private bool IsMouseOver()
	{
		// Assumes the node has a CollisionShape2D child via an Area2D or similar.
		// For a simple rect-based check we use the node's global position and a
		// configurable size exported below.
		Rect2 bounds = new Rect2(GlobalPosition - HalfSize, HalfSize * 2);
		return bounds.HasPoint(GetGlobalMousePosition());
	}

	/// <summary>Half-extents of the clickable area. Adjust in the Inspector.</summary>
	[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

	private ISlottable FindSlotUnderMouse()
	{
		Vector2 mouse = GetGlobalMousePosition();
		ISlottable best = null;
		float bestDist = float.MaxValue;

		CollectClosestSlot(GetTree().Root, mouse, ref best, ref bestDist);
		return best;
	}

	private void CollectClosestSlot(Node root, Vector2 point, ref ISlottable best, ref float bestDist)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is ISlottable slottable && child != this && slottable.ContainsPoint(point))
			{
				float dist = child is Node2D n2d ? n2d.GlobalPosition.DistanceTo(point) : 0f;
				if (dist < bestDist)
				{
					bestDist = dist;
					best = slottable;
				}
			}

			CollectClosestSlot(child, point, ref best, ref bestDist);
		}
	}
}
