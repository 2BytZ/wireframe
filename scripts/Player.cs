using System;
using Godot;

public partial class Player : CharacterBody3D
{

	// Player nodes

	private Node3D _head;
	private Vector3 _headStandingPosition;
	private Camera3D _camera;
	private CollisionShape3D _bodyStanding;
	private CollisionShape3D _bodyCrouching;
	private RayCast3D _raycast;
	private Node3D _wallrun;
	private Vector3 _wallrunDirection = Vector3.Zero;
	private Label _speedDebugLabel;

	private RayCast3D _wallRunBoostDirRaycast;

	//  Speed vars

	[Export] public float speedCap = 36f;

	public float speedCurrent = 0.0f;

	public const float runSpeed = 5.0f;
	public const float crouchSpeed = 3.0f;
	

	// States
	
	private enum MovementState
	{
		Idle,
		Running,
		Crouching,
		Sliding
	}

	public bool running = true;
	public bool crouching = false;
	public bool sliding = false;
	public bool isWallrunning = false;
	public bool isDashing = false;

	// Slide vars

	public double slideTimer = 0.0;
	public double slideTimerMax = 1.0;
	public Vector2 slideVector = Vector2.Zero;
	public double slideSpeed = 10.0;

	// Wallrun vars

	public bool canWallrun = false;
	public double wallrunDelay = 0.2;
	public const double wallrunDelayDefault = 0.2;

	[Export]
	public float wallrunAngle = 15;
	public float wallrunCurrentAngle = 0;
	public string side = "";

	// Wallrun jump vars

	public bool isWallrunJumping = false;
	public Vector3 wallJumpDirection = Vector3.Zero;
	private float forwardBoost = 4f;

	// Dash vars

	public double dashTimer = 0.0;
	public double dashTimerMax = 0.2;
	public float dashStartSpeed = 18.0f;
	public float dashEndSpeed = 7.0f;
	public float dashVerticalBoost = 0.6f;
	public Vector3 dashDirection = Vector3.Zero;
	

	// Movement Vars

	public double crouchDepth = -0.5;

	public const float jumpVelocity = 4.5f;

	public float groundAcceleration = 14.0f;
	public float groundFriction = 0.2f;


	public float airCap = 0.85f;
	public float airAcceleration = 800.0f;
	public float airMoveSpeed = 500.0f;

	public float accelerationCap = 80f;
	public float acceleration = 0.0f;
	public float deceleration = 0.0f;
	public float airDrag = 0.8f;

	// Input vars

	Vector3 direction = Vector3.Zero;
	public const float mouseSensitivity = 0.002f;


	

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
		_speedDebugLabel = GetNode<Label>("/root/world/HUD/SpeedDebugLabel");

	}
	// Handle mouse movement.
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

	


		// Getting movement input

		Vector2 inputDir = Input.GetVector("Left", "Right", "Forward", "Back");
		direction = (_head.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		deceleration = direction != Vector3.Zero ? IsOnFloor() ? MathF.Max(0, groundFriction) : MathF.Max(0, airDrag) : 0;


		// Getting the velocity

		Vector3 velocity = Velocity;
		
		// Getting look direction
		
		var playerViewDirection = -_camera.GlobalTransform.Basis.Z;

		// Handle movement state


		// Crouching

		if (Input.IsActionPressed("Crouch") || sliding)
		{
			speedCurrent = Mathf.Lerp(speedCurrent, crouchSpeed, (float)delta);
			Vector3 headPosition = _head.Position;
			headPosition.Y = _headStandingPosition.Y + (float)crouchDepth;
			_head.Position = headPosition;

			_bodyStanding.Disabled = true;
			_bodyCrouching.Disabled = false;

			running = false;
			crouching = true;

			// Slide begin logic

			if (Input.IsActionJustPressed("Slide") && inputDir != Vector2.Zero)
			{
				sliding = true;
				slideTimer = slideTimerMax;
				slideVector = inputDir;
			}
		}
		else if (!_raycast.IsColliding())
		{

			// Standing

			_bodyStanding.Disabled = false;
			_bodyCrouching.Disabled = true;
			
			Vector3 headPosition = _head.Position;
			headPosition.Y = _headStandingPosition.Y;
			_head.Position = headPosition;

			running = true;
			crouching = false;
		}

		// Handle sliding

		if (sliding)
		{
			slideTimer -= delta;
				if (slideTimer <= 0)
				{
					sliding = false;
				}
		}

		// Handle wallrun

		if (canWallrun)
		{
			if (IsOnWall())
			{
				if (!isWallrunning)
				{
					var collision = GetSlideCollision(0);
					if (collision != null)
					{
						var normal = collision.GetNormal();

						Vector3 wallrunDirection = Vector3.Up.Cross(normal);

						var dot = wallrunDirection.Dot(playerViewDirection);
						if (dot < 0)
						{
							wallrunDirection = -wallrunDirection;
						}

						wallrunDirection += -normal * 0.01f;
						_wallrunDirection = wallrunDirection.Normalized();

						// Boost!
						Vector3 boostDir = GlobalTransform.Basis.Z - _wallRunBoostDirRaycast.Transform.Basis.Z;
						velocity += boostDir * forwardBoost;

						isWallrunning = true;
					}
					side = getSide(collision.GetPosition());
				}
				else // Wallrunning
				{
					
				}
			}
			else
			{
				isWallrunning = false;
			}
		}

		// Handle wallrun rotation

		if (isWallrunning)
		{
			if (side == "Right")
			{
				wallrunCurrentAngle += (float)delta * 60;
				wallrunCurrentAngle = Mathf.Clamp(wallrunCurrentAngle, -wallrunAngle, wallrunAngle);
			}
			else if (side == "Left")
			{
				wallrunCurrentAngle -= (float)delta * 60;
				wallrunCurrentAngle = Mathf.Clamp(wallrunCurrentAngle, -wallrunAngle, wallrunAngle);
			}
		}
		else
		{
			if (wallrunCurrentAngle > 0)
			{
				wallrunCurrentAngle -= (float)delta * 40;
				wallrunCurrentAngle = Mathf.Max(0, wallrunCurrentAngle);
			}
			else if (wallrunCurrentAngle < 0)
			{
				wallrunCurrentAngle += (float)delta * 40;
				wallrunCurrentAngle = Mathf.Min(wallrunCurrentAngle, 0);
			}
		}
		_wallrun.RotationDegrees = new Vector3(0, 0, 1) * wallrunCurrentAngle;

		// Getting the side wall is on

		string getSide(Vector3 point)
		{
			point = ToLocal(point);

			if (point.X > 0)
			{
				return "Right";
			}
			else if (point.X < 0)
			{
				return "Left";
			}
			else
			{
				return "Center";
			}
		}

		// Handle dashing
		
		bool isDashing = dashTimer > 0.0;	

		if (Input.IsActionJustPressed("Dash") && !isDashing)
		{
			dashTimer = dashTimerMax;
			dashDirection = new Vector3(playerViewDirection.X, playerViewDirection.Y, playerViewDirection.Z).Normalized();
		}


		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;

			wallrunDelay = Mathf.Clamp(wallrunDelay - delta, 0, wallrunDelayDefault);
			if (wallrunDelay == 0.0)
			{
				canWallrun = true;
			}
		}
		else if (IsOnFloor())
		{
			canWallrun = false;
			isWallrunning = false;
			wallrunDelay = wallrunDelayDefault;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("Jump"))
		{
			if (IsOnFloor())
			{
				velocity.Y = jumpVelocity;
				sliding = false;	
			}
			
			if(isWallrunning)
			{
				velocity.Y = jumpVelocity;
				velocity += _wallrunDirection * 5.0f;
				isWallrunning = false;
				canWallrun = false;
				wallrunDelay = wallrunDelayDefault;
			}
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.

		bool isStillMoving = new Vector2(velocity.X, velocity.Z).Length() > 0.1f;

		if (isWallrunning && isStillMoving)
		{
			direction = _wallrunDirection;
			speedCurrent = runSpeed;
		}
		else if (sliding)
		{
			speedCurrent += (float)(slideTimer + 0.1) * (float)slideSpeed;
		}
		else if (dashTimer > 0.0)
		{
			dashTimer -= delta;
			float dashT = Mathf.Clamp(1.0f - (float)(dashTimer / dashTimerMax), 0.0f, 1.0f);
			float currentDashSpeed = Mathf.Lerp((float)dashStartSpeed, (float)dashEndSpeed, dashT);
			speedCurrent += currentDashSpeed;
			velocity.Y += dashVerticalBoost * (1.0f - dashT);
			if (dashTimer <= 0.0)
			{
				dashTimer = 0.0;
			}
		}
		else if (direction != Vector3.Zero)
		{
			speedCurrent = Mathf.Lerp(speedCurrent, runSpeed, (float)delta);
		}
		

		
		acceleration = Mathf.Max(0, MathF.Min(accelerationCap, speedCurrent - deceleration));

		speedCurrent = Mathf.MoveToward(speedCurrent, runSpeed, acceleration * (float)delta); // no touchy >:(
		speedCurrent = Mathf.Min(speedCap, speedCurrent); // no touchy >:(
		velocity.X = direction.X * speedCurrent;
		velocity.Z = direction.Z * speedCurrent;
		Velocity = velocity;
		MoveAndSlide();

		if (_speedDebugLabel != null)
		{
			_speedDebugLabel.Text = $"Speed: {speedCurrent:F1}\nAccel: {acceleration:F1}\nDecel: {deceleration:F1}";
		}

	}
}