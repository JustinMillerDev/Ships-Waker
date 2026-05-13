using Godot;

/// <summary>
/// A HyperSleep casket slot. Only Crew members can be slotted into a Casket.
/// When a Crew member is accepted, their state is set to HyperSleep.
/// When a Crew member is vacated, their state is set to Idle.
/// </summary>
public partial class Casket : Node2D, ISlottable, IOrientation
{
/// <summary>Half-extents of the slot's drop area. Adjust in the Inspector.</summary>
[Export] public Vector2 HalfSize { get; set; } = new Vector2(32, 32);

// IOrientation --------------------------------------------------------------

/// <summary>The direction this casket faces.</summary>
[Export] public CardinalDirection Facing { get; set; } = CardinalDirection.Left;

private Tween _flickerTween;

public override void _Ready()
{
UpdateSprite();
}

private void UpdateSprite()
{
if (GetNodeOrNull("Sprites/Sprite2D") is Sprite2D sprite)
sprite.FlipH = Facing == CardinalDirection.Left;

if (Facing == CardinalDirection.Left)
Position = new Vector2(Position.X - 8, Position.Y);

if (GetNodeOrNull("ReadyLabel") is Label readyLabel)
{
if (Facing == CardinalDirection.Left)
readyLabel.Position = new Vector2(readyLabel.Position.X + 42, readyLabel.Position.Y);
}
}

// ISlottable ----------------------------------------------------------------

public Vector2 SlotPosition => GlobalPosition;

/// <summary>The Crew member currently inside this casket, or <c>null</c> if empty.</summary>
public IDraggable OccupiedBy { get; set; }

public bool IsOccupied => OccupiedBy != null;

/// <summary>Accepts only Crew draggables into an empty casket.</summary>
public bool CanAccept(IDraggable draggable)
{
return !IsOccupied && draggable is Crew;
}

public void Accept(IDraggable draggable)
{
if (!CanAccept(draggable))
return;

OccupiedBy = draggable;
draggable.CurrentSlot = this;

// Snap the crew member into the casket
if (draggable is Node2D node2D)
node2D.GlobalPosition = SlotPosition;

// Put the crew member into HyperSleep
if (draggable is Crew crew)
crew.State = CrewState.HyperSleep;
}

public void Vacate()
{
if (OccupiedBy is Crew crew)
crew.State = CrewState.Idle;

if (OccupiedBy != null)
OccupiedBy.CurrentSlot = null;

OccupiedBy = null;

StopFlicker();
if (GetNodeOrNull("ReadyLabel") is Label readyLabel)
readyLabel.Text = "--";
}

/// <summary>Directly sets the ReadyLabel text and stops any flicker.</summary>
public void SetReadyLabel(string text)
{
StopFlicker();
if (GetNodeOrNull("ReadyLabel") is Label readyLabel)
readyLabel.Text = text;
}

/// <summary>Turns the label green and starts a looping flicker to signal crew is ready.</summary>
public void StartReadyFlicker()
{
if (GetNodeOrNull("ReadyLabel") is not Label readyLabel)
return;

readyLabel.Text = "00";
readyLabel.AddThemeColorOverride("font_color", new Color(0f, 1f, 0f));

_flickerTween?.Kill();
_flickerTween = CreateTween().SetLoops();
const float halfPeriod = 0.4f;
_flickerTween.TweenProperty(readyLabel, "modulate:a", 0f, halfPeriod);
_flickerTween.TweenProperty(readyLabel, "modulate:a", 1f, halfPeriod);
}

/// <summary>Stops the flicker, restores full opacity and default label colour.</summary>
private void StopFlicker()
{
_flickerTween?.Kill();
_flickerTween = null;
if (GetNodeOrNull("ReadyLabel") is Label readyLabel)
{
readyLabel.Modulate = new Color(readyLabel.Modulate, 1f);
readyLabel.RemoveThemeColorOverride("font_color");
}
}

public bool ContainsPoint(Vector2 point)
{
Rect2 bounds = new Rect2(GlobalPosition - HalfSize, HalfSize * 2);
return bounds.HasPoint(point);
}
}
