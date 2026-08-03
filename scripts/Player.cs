using System;
using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float baseSpeed = 6.0f; // base movement speed

	[Export] public float groundAcceleration = 10.0f; // increase player speed by this amount each second
	//[Export] public float groundDecceleration = 10.0f;
	[Export] public float groundFriction = 10.0f; // decrease player speed by this amount each second
	[Export] public float jumpVelocity = 4.5f;

	// Player nodes
	private Node3D _head;
	private Vector3 _headStandingPosition;
	private Camera3D _camera;
	private CollisionShape3D _bodyStanding;
	private CollisionShape3D _bodyCrouching;
	private RayCast3D _raycast;
	private Node3D _wallrun;
	private Vector3 _wallrunDirection = Vector3.Zero;
	private RayCast3D _wallRunBoostDirRaycast;

	// Input vars
	public const float mouseSensitivity = 0.002f;

	private Label _debugLabel;
	private float _currentSpeed = 0.0f;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_bodyStanding = GetNode<CollisionShape3D>("standingCollisionShape");
		_bodyCrouching = GetNode<CollisionShape3D>("crouchingCollisionShape");
		_head = GetNode<Node3D>("head");
		_camera = GetNode<Camera3D>("head/Camera3D");
		_headStandingPosition = _head.Position;
		_raycast = GetNode<RayCast3D>("RayCast3D");
		_wallrun = GetNode<Node3D>("wallrun");
		_wallRunBoostDirRaycast = GetNode<RayCast3D>("wallRunBoostDirRay");

		SetupDebugView();
	}

	public override void _Input(InputEvent @event)
	{
		// Mouse looking logic
		if (@event is InputEventMouseMotion m)
		{
			_head.RotateY(-m.Relative.X * mouseSensitivity);
			_camera.RotateX(-m.Relative.Y * mouseSensitivity);

			Vector3 camRot = _camera.Rotation;
			camRot.X = Mathf.Clamp(camRot.X, Mathf.DegToRad(-80f), Mathf.DegToRad(80f));
			_camera.Rotation = camRot;
		}
		else if (@event is InputEventKey k && k.Keycode == Key.Escape)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("Jump") && IsOnFloor())
		{
			velocity.Y = jumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("Left", "Right", "Forward", "Back");
		Vector3 moveDirection = (_head.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (moveDirection != Vector3.Zero)
		{
			_currentSpeed = Mathf.MoveToward(_currentSpeed, baseSpeed, groundAcceleration * (float)delta);
			velocity.X = moveDirection.X * _currentSpeed;
			velocity.Z = moveDirection.Z * _currentSpeed;
		}
		else
		{
			_currentSpeed = Mathf.MoveToward(_currentSpeed, 0f, groundFriction * (float)delta);
			velocity.X = Mathf.MoveToward(Velocity.X, 0, baseSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, baseSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
		UpdateDebugLabel();
	}

	private void SetupDebugView()
	{
		_debugLabel = GetNodeOrNull<Label>("/root/world/DebugUI/MarginContainer/Label");
		if (_debugLabel != null)
		{
			return;
		}

		var debugCanvas = new CanvasLayer();
		debugCanvas.Name = "DebugUI";
		debugCanvas.Layer = 100;

		var margin = new MarginContainer();
		margin.Name = "MarginContainer";
		margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_top", 16);

		_debugLabel = new Label();
		_debugLabel.Name = "Label";
		_debugLabel.HorizontalAlignment = HorizontalAlignment.Left;
		_debugLabel.VerticalAlignment = VerticalAlignment.Top;
		_debugLabel.AddThemeColorOverride("font_color", Colors.White);
		_debugLabel.AddThemeFontSizeOverride("font_size", 24);

		margin.AddChild(_debugLabel);
		debugCanvas.AddChild(margin);
		GetTree().Root.AddChild(debugCanvas);
	}

	private void UpdateDebugLabel()
	{
		if (_debugLabel == null)
		{
			return;
		}

		_debugLabel.Text = $"Speed: {_currentSpeed:F1} / {"8.5":F0}\nVelocity: {Velocity.Length():F1}";
	}
}
