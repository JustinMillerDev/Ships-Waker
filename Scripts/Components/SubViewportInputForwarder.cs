using Godot;

public partial class SubViewportInputForwarder : SubViewportContainer
{
    private SubViewport _subViewport;
    private Camera2D _camera2D;

    /// <summary>True while a mouse button was pressed inside the container — keeps drag events flowing.</summary>
    private bool _dragging = false;

    public override void _Ready()
    {
        _subViewport = GetNode<SubViewport>("SubViewport");
        _camera2D = _subViewport.GetNode<Camera2D>("Camera2D");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouse mouseEvent)
            return;

        Vector2 localMousePos = GetGlobalTransform().AffineInverse() * mouseEvent.GlobalPosition;
        Rect2 containerRect = new Rect2(Vector2.Zero, Size);
        bool insideContainer = containerRect.HasPoint(localMousePos);

        // Track drag state: a press inside starts a drag, any release ends it.
        if (@event is InputEventMouseButton mbEvent)
        {
            if (mbEvent.Pressed && insideContainer)
                _dragging = true;
            else if (!mbEvent.Pressed)
                _dragging = false;
        }

        // Forward if inside the container OR we are mid-drag (mouse may have left the rect).
        if (!insideContainer && !_dragging)
            return;

        InputEventMouse forwardedEvent = (InputEventMouse)mouseEvent.Duplicate();

        Vector2 halfViewportSize = (Vector2)_subViewport.Size / 2f;
        Vector2 cameraWorldPos   = _camera2D.GlobalPosition;
        Vector2 adjustedPos      = ((localMousePos - halfViewportSize) / _camera2D.Zoom) + cameraWorldPos;

        forwardedEvent.Position       = adjustedPos;
        forwardedEvent.GlobalPosition = adjustedPos;

        _subViewport.PushInput(forwardedEvent);
        GetViewport().SetInputAsHandled();
    }
}