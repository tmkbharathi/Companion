using Godot;
using System;
using System.Runtime.InteropServices;

public partial class Main : Node3D
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private MeshInstance3D _leftPaw = null!;
    private MeshInstance3D _rightPaw = null!;
    private MeshInstance3D _catBody = null!;

    private bool _isDragging = false;
    private Vector2I _dragOffset;

    private float _leftPawTargetY = 0.1f;
    private float _rightPawTargetY = 0.1f;
    private bool _lastPressedWasLeft = false;

    private float _time = 0.0f;

    public override void _Ready()
    {
        _leftPaw = GetNode<MeshInstance3D>("LeftPaw");
        _rightPaw = GetNode<MeshInstance3D>("RightPaw");
        _catBody = GetNode<MeshInstance3D>("CatBody");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (mouseEvent.Pressed)
                {
                    _isDragging = true;
                    _dragOffset = DisplayServer.WindowGetPosition() - DisplayServer.MouseGetPosition();
                }
                else
                {
                    _isDragging = false;
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        // Handle window dragging
        if (_isDragging)
        {
            DisplayServer.WindowSetPosition(DisplayServer.MouseGetPosition() + _dragOffset);
        }

        // Idle breathing animation
        _time += (float)delta * 2.0f;
        float bodyScaleY = 1.0f + Mathf.Sin(_time) * 0.015f;
        _catBody.Scale = new Vector3(1.0f, bodyScaleY, 1.0f);

        // Check if any key is pressed globally on the system (vKeys 8 to 190)
        bool keyWasPressed = false;
        for (int i = 8; i < 190; i++)
        {
            if ((GetAsyncKeyState(i) & 0x8000) != 0)
            {
                keyWasPressed = true;
                break;
            }
        }

        if (keyWasPressed)
        {
            TriggerPawTap();
        }

        // Smoothly interpolate paw positions back to resting height
        Vector3 leftPos = _leftPaw.Position;
        leftPos.Y = Mathf.Lerp(leftPos.Y, _leftPawTargetY, (float)delta * 15.0f);
        _leftPaw.Position = leftPos;

        Vector3 rightPos = _rightPaw.Position;
        rightPos.Y = Mathf.Lerp(rightPos.Y, _rightPawTargetY, (float)delta * 15.0f);
        _rightPaw.Position = rightPos;

        // Reset paw targets if they have gone down
        if (_leftPawTargetY < 0.1f && leftPos.Y < 0.02f)
        {
            _leftPawTargetY = 0.1f;
        }
        if (_rightPawTargetY < 0.1f && rightPos.Y < 0.02f)
        {
            _rightPawTargetY = 0.1f;
        }
    }

    private void TriggerPawTap()
    {
        if (_lastPressedWasLeft)
        {
            _rightPawTargetY = 0.01f;
            _leftPawTargetY = 0.1f;
            _lastPressedWasLeft = false;
        }
        else
        {
            _leftPawTargetY = 0.01f;
            _rightPawTargetY = 0.1f;
            _lastPressedWasLeft = true;
        }
    }
}
